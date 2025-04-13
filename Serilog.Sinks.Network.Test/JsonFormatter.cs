using System;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
using FluentAssertions;
using Serilog.Core;
using Serilog.Events;
using Serilog.Formatting;
using Serilog.Sinks.Network.Formatters;
using Xunit;

namespace Serilog.Sinks.Network.Test
{
    public class JsonFormatter
    {
        private static LoggerAndSocket ConfigureTestLogger(
            ITextFormatter formatter = null,
            ILogEventEnricher[] enrichers = null
        )
        {
            var socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            socket.Bind(new IPEndPoint(IPAddress.Loopback, 0));
            socket.Listen();

            var loggerConfiguration = new LoggerConfiguration()
                .WriteTo.TCPSink(IPAddress.Loopback, ((IPEndPoint)socket.LocalEndPoint!).Port, null, null, formatter);
            if (enrichers != null)
            {
                loggerConfiguration.Enrich.With(enrichers);
            }
            var logger = loggerConfiguration.CreateLogger();
            
            return new LoggerAndSocket { Logger = logger, Socket = socket };
        }
        
        [Fact]
        public async Task MustNotLogATrailingCommaWhenThereAreNoProperties()
        {
            using var fixture = ConfigureTestLogger(new LogstashJsonFormatter());
            var arbitraryMessage = nameof(JsonFormatter) + "MustNotLogATrailingCommaWhenThereAreNoProperties" + Guid.NewGuid();
            
            fixture.Logger.Information(arbitraryMessage);

            var receivedData = await ServerPoller.PollForReceivedData(fixture.Socket);
            var loggedData = receivedData?.TrimEnd('\n');

            var logMessageWithTrailingComma = $"\"message\":\"{arbitraryMessage}\",}}";
            loggedData.Should().NotEndWith(logMessageWithTrailingComma);
        }
        
        [Fact]
        public async Task CanStillLogMessagesWithExceptions()
        {
            using var fixture = ConfigureTestLogger(new LogstashJsonFormatter());
            var arbitraryMessage = nameof(JsonFormatter) + "CanStillLogMessagesWithExceptions" + Guid.NewGuid();
            
            fixture.Logger.Information(new Exception("exploding"), arbitraryMessage);

            var receivedData = await ServerPoller.PollForReceivedData(fixture.Socket);

            receivedData.Should().Contain("\"exception\":\"System.Exception: exploding\"}");
        }

        [Fact]
        public async Task IncludesCurrentActivityTraceAndSpanIds()
        {
            // Create an ActivitySource, add a listener, and start an activity.
            // StartActivity() would return null if there were no listeners.
            using var activitySource = new ActivitySource("TestSource");
            using var activityListener = CreateAndAddActivityListener(activitySource.Name);
            using var activity = activitySource.StartActivity();
            Assert.NotNull(activity);

            using var fixture = ConfigureTestLogger(new LogstashJsonFormatter());

            fixture.Logger.Information("arbitraryMessage");

            var receivedData = await ServerPoller.PollForReceivedData(fixture.Socket);

            receivedData.Should().Contain($"\"traceId\":\"{activity!.TraceId}\"");
            receivedData.Should().Contain($"\"spanId\":\"{activity.SpanId}\"");
        }

        [Fact]
        public async Task OmitsTraceAndSpanIdsWhenThereIsNoActivity()
        {
            using var fixture = ConfigureTestLogger(new LogstashJsonFormatter());
            
            fixture.Logger.Information("arbitraryMessage");

            var receivedData = await ServerPoller.PollForReceivedData(fixture.Socket);

            receivedData.Should().NotContain("\"traceId\"");
            receivedData.Should().NotContain("\"spanId\"");
        }

        [Fact]
        public async Task DoesNotAddDuplicateTraceAndSpanIds()
        {
            using var activitySource = new ActivitySource("TestSource");
            using var activityListener = CreateAndAddActivityListener(activitySource.Name);
            using var activity = activitySource.StartActivity();
            Assert.NotNull(activity);

            using var fixture = ConfigureTestLogger(
                new LogstashJsonFormatter(),
                // This enricher will add traceId and spanId properties to the log event: 
                [ new TraceAndSpanEnricher() ]
            );
            
            fixture.Logger.Information("arbitraryMessage");

            var receivedData = await ServerPoller.PollForReceivedData(fixture.Socket);

            // Count the occurrences of traceId and spanId in the received data:
            var traceIdCount = receivedData.Split("\"traceId\"").Length - 1;
            traceIdCount.Should().Be(1, "traceId should only appear once in the log message.");
            var spanIdCount = receivedData.Split("\"spanId\"").Length - 1;
            spanIdCount.Should().Be(1, "spanId should only appear once in the log message.");
        }

        private class TraceAndSpanEnricher : ILogEventEnricher
        {
            public void Enrich(LogEvent logEvent, ILogEventPropertyFactory propertyFactory)
            {
                var currentActivity = Activity.Current;
                if (currentActivity != null)
                {
                    logEvent.AddOrUpdateProperty(propertyFactory.CreateProperty("traceId", currentActivity.TraceId.ToString()));
                    logEvent.AddOrUpdateProperty(propertyFactory.CreateProperty("spanId", currentActivity.SpanId.ToString()));
                }
            }
        }

        private static ActivityListener CreateAndAddActivityListener(string sourceName)
        {
            var activityListener = new ActivityListener
            {
                ShouldListenTo = source => source.Name == sourceName,
                Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllData,
            };
            ActivitySource.AddActivityListener(activityListener);
            return activityListener;
        }
    }
}