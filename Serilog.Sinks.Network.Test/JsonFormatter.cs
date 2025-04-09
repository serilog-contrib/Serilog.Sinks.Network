using System;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
using FluentAssertions;
using Serilog.Formatting;
using Serilog.Sinks.Network.Formatters;
using Xunit;

namespace Serilog.Sinks.Network.Test
{
    public class JsonFormatter
    {
        private static LoggerAndSocket ConfigureTestLogger(ITextFormatter? formatter = null)
        {
            var socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            socket.Bind(new IPEndPoint(IPAddress.Loopback, 0));
            socket.Listen();

            var logger = new LoggerConfiguration()
                .WriteTo.TCPSink(IPAddress.Loopback, ((IPEndPoint)socket.LocalEndPoint!).Port, null, null, formatter)
                .CreateLogger();
            
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
            // Create an ActivitySource and start an activity.
            // StartActivity() would return null if there were no listeners.
            using var activityListener = new ActivityListener
            {
                ShouldListenTo = _ => true,
                Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllData,
            };
            ActivitySource.AddActivityListener(activityListener);
            using var activitySource = new ActivitySource("TestSource");
            using Activity? activity = activitySource.StartActivity();

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
    }
}