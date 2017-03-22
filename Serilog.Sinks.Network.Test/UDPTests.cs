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
        public void CanLogHelloWorld_WithLogstashJsonFormatter()
        {
            ConfigureTestLogger(new LogstashJsonFormatter());
            _logger.Information("Hello World");
            Thread.Sleep(500);
            _listener.ReceivedData.SingleOrDefault().Should().Contain("\"message\":\"Hello World\"");
        }

        [Fact]
        public void CanLogHelloWorld_WithDefaultFormatter()
        {
            ConfigureTestLogger();
            _logger.Information("Hello World");
            Thread.Sleep(500);
            var receivedData = _listener.ReceivedData.SingleOrDefault().Should().Contain("\"message\":\"Hello World\"");
        }

        [Fact]
        public void CanLogHelloWorld_WithRawFormatter()
        {
            ConfigureTestLogger(new RawFormatter());
            _logger.Information("Hello World");
            Thread.Sleep(500);
            _listener.ReceivedData.SingleOrDefault().Should().Contain("Information: \"Hello World\"");
        }



        [Fact]
        public void CanLogWithProperties()
        {
            ConfigureTestLogger();

            _logger.Information("Hello {location}", "world");
            Thread.Sleep(500);
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
    
    public class UDPListener
    {
        private bool _done;
        public List<string> ReceivedData { get; }
        private readonly UdpClient _listener;
        private IPEndPoint _ipEndPoint;

        public UDPListener(int port)
        {
            _listener = new UdpClient(port);
            _ipEndPoint = new IPEndPoint(IPAddress.Any, port);
            ReceivedData = new List<string>();
        }

        public void Start()
        {
            Task.Run(() =>
            {
                while (!_done)
                {
                    var receiveByteArray = _listener.Receive(ref _ipEndPoint);
                    var receivedData = Encoding.ASCII.GetString(receiveByteArray, 0, receiveByteArray.Length);
                    ReceivedData.Add(receivedData);
                }
            });
        }
        
        public void Stop()
        {
            _done = true;
            _listener.Close();
        }
    }
}
