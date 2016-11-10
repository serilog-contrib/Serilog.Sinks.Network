using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Newtonsoft.Json;
using Xunit;

namespace Serilog.Sinks.Network.Test
{
    public class UDPTests : IDisposable
    {
        private readonly ILogger _logger;
        private readonly UDPListener _listener;

        public UDPTests()
        {
            _logger = new LoggerConfiguration()
                .WriteTo.UDPSink(IPAddress.Loopback, 9999)
                .CreateLogger();
            
            _listener = new UDPListener(9999);
            _listener.Start();
        }

        [Fact]
        public void CanLogHelloWorld()
        {
            _logger.Information("Hello World");
            Thread.Sleep(500);
            _listener.ReceivedData.SingleOrDefault().Should().Contain("\"message\":\"Hello World\"");
        }

        [Fact]
        public void CanLogWithProperties()
        {
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
