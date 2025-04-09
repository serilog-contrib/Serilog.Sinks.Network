using System;
using System.Dynamic;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
using FluentAssertions;
using FluentAssertions.Execution;
using Newtonsoft.Json;
using Serilog.Formatting;
using Serilog.Formatting.Compact;
using Serilog.Sinks.Network.Formatters;
using Xunit;

namespace Serilog.Sinks.Network.Test
{
    public class WhenLoggingViaTcp
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
        public async Task CanLogHelloWorld_WithLogstashJsonFormatter()
        {
            using var fixture = ConfigureTestLogger(new LogstashJsonFormatter());
            var arbitraryMessage = nameof(WhenLoggingViaTcp) + "CanLogHelloWorld_WithLogstashJsonFormatter" + Guid.NewGuid();
            fixture.Logger.Information(arbitraryMessage);
            var receivedData = await ServerPoller.PollForReceivedData(fixture.Socket);
            receivedData.Should().Contain($"\"message\":\"{arbitraryMessage}\"");
        }

        [Fact]
        public async Task CanLogHelloWorld_WithDefaultFormatter()
        {
            using var fixture = ConfigureTestLogger();
            var arbitraryMessage = nameof(WhenLoggingViaTcp) + "CanLogHelloWorld_WithDefaultFormatter" + Guid.NewGuid();
            fixture.Logger.Information(arbitraryMessage);

            var receivedData = await ServerPoller.PollForReceivedData(fixture.Socket);

            receivedData.Should().Contain($"\"message\":\"{arbitraryMessage}\"");
        }

        [Fact]
        public async Task CanLogHelloWorld_WithRawFormatter()
        {
            using var fixture =ConfigureTestLogger(new CompactJsonFormatter());
            var arbitraryMessage = nameof(WhenLoggingViaTcp) + "CanLogHelloWorld_WithCompactJsonFormatter" + Guid.NewGuid();
            fixture.Logger.Information(arbitraryMessage);
            var receivedData = await ServerPoller.PollForReceivedData(fixture.Socket);
            receivedData.Should().Contain($"\"{arbitraryMessage}\"");
        }

        [Fact]
        public async Task CanLogWithProperties()
        {
            using var fixture = ConfigureTestLogger();
            fixture.Logger.Information("TCP Hello {location}", "world");
            var receivedData = await ServerPoller.PollForReceivedData(fixture.Socket);
            dynamic? payload = JsonConvert.DeserializeObject<ExpandoObject>(receivedData);
            if (payload == null)
            {
                throw new AssertionFailedException("expected payload not null");
            }
            Assert.Equal("Information", payload.level);
            Assert.Equal("TCP Hello \"world\"", payload.message);
            Assert.Equal("world", payload.location);
        }
    }
}
