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
    public class WhenLoggingViaUDP : IDisposable
    {
        private ILogger _logger;
        private UDPListener _listener;

        private void ConfigureTestLogger(ITextFormatter formatter = null)
        {
            var port = new Random().Next(50123) + 10235;
            _logger = new LoggerConfiguration()
                .WriteTo.UDPSink(IPAddress.Loopback, port, formatter)
                .CreateLogger();

            _listener = new UDPListener(port);
            _listener.Start();
        }

        [Fact]
        public void CanLogHelloWorld_WithLogstashJsonFormatter()
        {
            ConfigureTestLogger(new LogstashJsonFormatter());
            var arbitraryMessage = nameof(WhenLoggingViaUDP) + "CanLogHelloWorld_WithLogstashJsonFormatter" + Guid.NewGuid();
            _logger.Information(arbitraryMessage);
            var receivedData = ServerPoller.PollForReceivedData(_listener);
            receivedData.Should().Contain($"\"message\":\"{arbitraryMessage}\"");
        }

        [Fact]
        public void CanLogHelloWorld_WithDefaultFormatter()
        {
            ConfigureTestLogger();
            var arbitraryMessage = nameof(WhenLoggingViaUDP) + "CanLogHelloWorld_WithDefaultFormatter" + Guid.NewGuid();
            _logger.Information(arbitraryMessage);
            var receivedData = ServerPoller.PollForReceivedData(_listener);
            receivedData.Should().Contain($"\"message\":\"{arbitraryMessage}\"");
        }

        [Fact]
        public void CanLogHelloWorld_WithRawFormatter()
        {
#pragma warning disable 618
            // specifically testing the deprecated RawFormatter
            ConfigureTestLogger(new CompactJsonFormatter());
#pragma warning restore 618

            var arbitraryMessage = nameof(WhenLoggingViaUDP) + "CanLogHelloWorld_WithCompactJsonFormatter" + Guid.NewGuid();
            _logger.Information(arbitraryMessage);
            var receivedData = ServerPoller.PollForReceivedData(_listener);
            
            
            receivedData.Should().Contain($"\"{arbitraryMessage}\"");
        }

        [Fact]
        public void CanLogWithProperties()
        {
            ConfigureTestLogger();

            _logger.Information("UDP Hello {location}", "world");
            var receivedData = ServerPoller.PollForReceivedData(_listener);
            var stringPayload = receivedData;
            dynamic payload = JsonConvert.DeserializeObject<ExpandoObject>(stringPayload);
            Assert.Equal("Information", payload.level);
            Assert.Equal("UDP Hello \"world\"", payload.message);
            Assert.Equal("world", payload.location);
        }

        public void Dispose()
        {
            _listener.Stop();
        }
    }
}
