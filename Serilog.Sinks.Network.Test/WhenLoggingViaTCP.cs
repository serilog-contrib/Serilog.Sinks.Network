using System;
using System.Dynamic;
using System.Net;
using FluentAssertions;
using Newtonsoft.Json;
using Serilog.Formatting;
using Serilog.Formatting.Compact;
using Serilog.Sinks.Network.Formatters;
using Xunit;

namespace Serilog.Sinks.Network.Test
{
    public class WhenLoggingViaTCP : IDisposable
    {
        private ILogger _logger;
        private TCPServer _server;

        private void ConfigureTestLogger(ITextFormatter formatter = null)
        {
            var port = new Random().Next(50020) + 10000;
            _server = new TCPServer(IPAddress.Loopback, port);
            _server.Start();

            _logger = new LoggerConfiguration()
                .WriteTo.TCPSink(IPAddress.Loopback, port, null, null, formatter)
                .CreateLogger();
        }

        [Fact]
        public void CanLogHelloWorld_WithLogstashJsonFormatter()
        {
            ConfigureTestLogger(new LogstashJsonFormatter());
            var arbitraryMessage = nameof(WhenLoggingViaTCP) + "CanLogHelloWorld_WithLogstashJsonFormatter" + Guid.NewGuid();
            _logger.Information(arbitraryMessage);
            var receivedData = ServerPoller.PollForReceivedData(_server);
            receivedData.Should().Contain($"\"message\":\"{arbitraryMessage}\"");
        }

        [Fact]
        public void CanLogHelloWorld_WithDefaultFormatter()
        {
            ConfigureTestLogger();
            var arbitraryMessage = nameof(WhenLoggingViaTCP) + "CanLogHelloWorld_WithDefaultFormatter" + Guid.NewGuid();
            _logger.Information(arbitraryMessage);

            var receivedData = ServerPoller.PollForReceivedData(_server);

            receivedData.Should().Contain($"\"message\":\"{arbitraryMessage}\"");
        }

        [Fact]
        public void CanLogHelloWorld_WithRawFormatter()
        {
            ConfigureTestLogger(new CompactJsonFormatter());
            var arbitraryMessage = nameof(WhenLoggingViaTCP) + "CanLogHelloWorld_WithCompactJsonFormatter" + Guid.NewGuid();
            _logger.Information(arbitraryMessage);
            var receivedData = ServerPoller.PollForReceivedData(_server);
            receivedData.Should().Contain($"\"{arbitraryMessage}\"");
        }

        [Fact]
        public void CanLogWithProperties()
        {
            ConfigureTestLogger();
            _logger.Information("TCP Hello {location}", "world");
            var receivedData = ServerPoller.PollForReceivedData(_server);
            dynamic payload = JsonConvert.DeserializeObject<ExpandoObject>(receivedData);
            Assert.Equal("Information", payload.level);
            Assert.Equal("TCP Hello \"world\"", payload.message);
            Assert.Equal("world", payload.location);
        }

        public void Dispose()
        {
            _server.Stop();
        }
    }
}
