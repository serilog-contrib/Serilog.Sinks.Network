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
    public class WhenLoggingViaUdp 
    {
        private static LoggerAndSocket ConfigureTestLogger(ITextFormatter? formatter = null)
        {
            var socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            socket.Bind(new IPEndPoint(IPAddress.Loopback, 0));
            
            var logger = new LoggerConfiguration()
                .WriteTo.UDPSink(IPAddress.Loopback, ((IPEndPoint)socket.LocalEndPoint!).Port, formatter)
                .CreateLogger();

            return new LoggerAndSocket { Logger = logger, Socket = socket };
        }

        [Fact]
        public async Task CanLogHelloWorld_WithLogstashJsonFormatter()
        {
            using var fixture = ConfigureTestLogger(new LogstashJsonFormatter());
            var arbitraryMessage = nameof(WhenLoggingViaUdp) + "CanLogHelloWorld_WithLogstashJsonFormatter" + Guid.NewGuid();
            fixture.Logger.Information(arbitraryMessage);
            var receivedData = await ServerPoller.PollForReceivedData(fixture.Socket, udp: true);
            receivedData.Should().Contain($"\"message\":\"{arbitraryMessage}\"");
        }

        [Fact]
        public async Task CanLogHelloWorld_WithDefaultFormatter()
        {
            using var fixture = ConfigureTestLogger();
            var arbitraryMessage = nameof(WhenLoggingViaUdp) + "CanLogHelloWorld_WithDefaultFormatter" + Guid.NewGuid();
            fixture.Logger.Information(arbitraryMessage);
            var receivedData = await ServerPoller.PollForReceivedData(fixture.Socket, udp: true);
            receivedData.Should().Contain($"\"message\":\"{arbitraryMessage}\"");
        }

        [Fact]
        public async Task CanLogHelloWorld_WithCompactJsonFormatter()
        {
            using var fixture = ConfigureTestLogger(new CompactJsonFormatter());
            var arbitraryMessage = nameof(WhenLoggingViaUdp) + "CanLogHelloWorld_WithCompactJsonFormatter" + Guid.NewGuid();
            fixture.Logger.Information(arbitraryMessage);
            var receivedData = await ServerPoller.PollForReceivedData(fixture.Socket, udp: true);
            receivedData.Should().Contain($"\"{arbitraryMessage}\"");
        }

        [Fact]
        public async Task CanLogWithProperties()
        {
            using var fixture = ConfigureTestLogger();

            fixture.Logger.Information("UDP Hello {location}", "world");
            var receivedData = await ServerPoller.PollForReceivedData(fixture.Socket, udp: true);
            var stringPayload = receivedData;
            dynamic payload = JsonConvert.DeserializeObject<ExpandoObject>(stringPayload) ?? throw new AssertionFailedException("expected deserialization to work");
            Assert.Equal("Information", payload.level);
            Assert.Equal("UDP Hello \"world\"", payload.message);
            Assert.Equal("world", payload.location);
        }
    }
}
