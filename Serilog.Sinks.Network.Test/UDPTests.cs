using System;
using System.Collections.Generic;
using System.Dynamic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Newtonsoft.Json;
using Serilog.Events;
using Serilog.Formatting;
using Serilog.Formatting.Raw;
using Serilog.Sinks.Network.Formatters;
using Xunit;

namespace Serilog.Sinks.Network.Test
{
    public class UDPTests : IDisposable
    {
        private ILogger _logger;
        private UDPListener _listener;

        public void ConfigureTestLogger(ITextFormatter formatter = null)
        {
            _logger = new LoggerConfiguration()
                .WriteTo.UDPSink(IPAddress.Loopback, 9999, formatter)
                .CreateLogger();

            _listener = new UDPListener(9999);
            _listener.Start();
        }

        [Fact]
        public async Task CanLogHelloWorld_WithLogstashJsonFormatter()
        {
            ConfigureTestLogger(new LogstashJsonFormatter());
            _logger.Information("Hello World");
            await Task.Delay(500);
            _listener.ReceivedData.SingleOrDefault().Should().Contain("\"message\":\"Hello World\"");
        }

        [Fact]
        public async Task CanLogHelloWorld_WithDefaultFormatter()
        {
            ConfigureTestLogger();
            _logger.Information("Hello World");
            await Task.Delay(500);
            var receivedData = _listener.ReceivedData.SingleOrDefault().Should().Contain("\"message\":\"Hello World\"");
        }

        [Fact]
        public async Task CanLogHelloWorld_WithRawFormatter()
        {
            ConfigureTestLogger(new RawFormatter());
            _logger.Information("Hello World");
            await Task.Delay(500);
            _listener.ReceivedData.SingleOrDefault().Should().Contain("Information: \"Hello World\"");
        }

        [Fact]
        public async Task CanLogWithProperties()
        {
            ConfigureTestLogger();

            _logger.Information("Hello {location}", "world");
            await Task.Delay(500);
            var stringPayload = _listener.ReceivedData.SingleOrDefault();
            dynamic payload = JsonConvert.DeserializeObject<ExpandoObject>(stringPayload);
            Assert.Equal("Information", payload.level);
            Assert.Equal("Hello \"world\"", payload.message);
            Assert.Equal("world", payload.location);
        }

        public void Dispose()
        {
            _listener.Stop();
        }
    }
}
