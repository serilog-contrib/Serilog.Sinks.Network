using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Newtonsoft.Json;
using Serilog.Formatting;
using Serilog.Formatting.Raw;
using Serilog.Sinks.Network.Formatters;
using Xunit;

namespace Serilog.Sinks.Network.Test
{
    public class TCPTests : IDisposable
    {
        private ILogger _logger;
        private TCPServer _server;
        private Random _random = new Random();
        private int _delay = 1000;

        private void ConfigureTestLogger(ITextFormatter formatter = null)
        {
            int port = _random.Next(50000) + 10000;
            _server = new TCPServer(IPAddress.Loopback, port);
            _server.Start();

            _logger = new LoggerConfiguration()
                .WriteTo.TCPSink(IPAddress.Loopback, port, formatter)
                .CreateLogger();
        }

        [Fact]
        public async Task CanLogHelloWorld_WithLogstashJsonFormatter()
        {
            ConfigureTestLogger(new LogstashJsonFormatter());
            _logger.Information("Hello World");
            await Task.Delay(_delay);
            _server.ReceivedData.SingleOrDefault().Should().Contain("\"message\":\"Hello World\"");
        }

        [Fact]
        public async Task CanLogHelloWorld_WithDefaultFormatter()
        {
            ConfigureTestLogger();
            _logger.Information("Hello World");
            await Task.Delay(_delay);
            _server.ReceivedData.SingleOrDefault().Should().Contain("\"message\":\"Hello World\"");
        }

        [Fact]
        public async Task CanLogHelloWorld_WithRawFormatter()
        {
            ConfigureTestLogger(new RawFormatter());
            _logger.Information("Hello World");
            await Task.Delay(_delay);
            _server.ReceivedData.SingleOrDefault().Should().Contain("Information: \"Hello World\"");
        }

        [Fact]
        public async Task CanLogWithProperties()
        {
            ConfigureTestLogger();
            _logger.Information("Hello {location}", "world");
            await Task.Delay(_delay);
            var stringPayload = _server.ReceivedData.SingleOrDefault();
            dynamic payload = JsonConvert.DeserializeObject<ExpandoObject>(stringPayload);
            Assert.Equal("Information", payload.level);
            Assert.Equal("Hello \"world\"", payload.message);
            Assert.Equal("world", payload.location);
        }

        public void Dispose()
        {
            _server.Stop();
        }
    }
}
