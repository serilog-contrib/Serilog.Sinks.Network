using System;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
using FluentAssertions;
using Serilog.Core;
using Serilog.Core.Enrichers;
using Serilog.Events;
using Serilog.Formatting;
using Serilog.Sinks.Network.Formatters;
using Xunit;

namespace Serilog.Sinks.Network.Test
{
    public class JsonFormatter
    {
        private static LoggerAndSocket ConfigureTestLogger(
            ITextFormatter? formatter = null,
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

            receivedData.Should().Contain($"\"traceId\":\"{activity.TraceId}\"");
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

        // The following test documents and validates the current behavior, but this could change
        // depending on how https://github.com/serilog-contrib/Serilog.Sinks.Network/issues/39 is
        // resolved.
        [Fact]
        public async Task WritesTraceAndSpanIdsBeforeDuplicatePropertiesFromEnrichers()
        {
            using var activitySource = new ActivitySource("TestSource");
            using var activityListener = CreateAndAddActivityListener(activitySource.Name);
            using var activity = activitySource.StartActivity();
            Assert.NotNull(activity);

            using var fixture = ConfigureTestLogger(
                new LogstashJsonFormatter(), 
                [
                    new PropertyEnricher("traceId", "traceId-from-enricher"),
                    new PropertyEnricher("spanId", "spanId-from-enricher")
                ]
            );
            
            fixture.Logger.Information("arbitraryMessage");

            var receivedData = await ServerPoller.PollForReceivedData(fixture.Socket);

            var indexOfTraceId = receivedData.IndexOf($"\"traceId\":\"{activity.TraceId}\"");
            var indexOfTraceIdFromEnricher = receivedData.IndexOf("\"traceId\":\"traceId-from-enricher\"");
            indexOfTraceId.Should().BePositive().And.BeLessThan(indexOfTraceIdFromEnricher);

            var indexOfSpanId = receivedData.IndexOf($"\"spanId\":\"{activity.SpanId}\"");
            var indexOfSpanIdFromEnricher = receivedData.IndexOf("\"spanId\":\"spanId-from-enricher\"");
            indexOfSpanId.Should().BePositive().And.BeLessThan(indexOfSpanIdFromEnricher);
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