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
using Xunit;

namespace Serilog.Sinks.Network.Test
{
    public class TCPTests : IDisposable
    {
        private readonly ILogger _logger;
        private readonly TCPServer _server;

        public TCPTests()
        {
            _server = new TCPServer(IPAddress.Loopback, 10999);
            _server.Start();

            _logger = new LoggerConfiguration()
                .WriteTo.TCPSink(IPAddress.Loopback, 10999)
                .CreateLogger();
        }

        [Fact]
        public void CanLogHelloWorld()
        {
            _logger.Information("Hello World");
            Thread.Sleep(500);
            _server.ReceivedData.SingleOrDefault().Should().Contain("\"MessageTemplate\":\"Hello World\"");
        }

        [Fact]
        public void CanLogWithProperties()
        {
            _logger.Information("Hello {location}", "world");
            Thread.Sleep(500);
            var stringPayload = _server.ReceivedData.SingleOrDefault();
            dynamic payload = JsonConvert.DeserializeObject<ExpandoObject>(stringPayload);
            Assert.Equal("Information", payload.Level);
            Assert.Equal("Hello {location}", payload.MessageTemplate);
            Assert.Equal("Hello \"world\"", payload.message);
            Assert.Equal("world", payload.Properties.location);
        }


        public void Dispose()
        {
            _server.Stop();
        }
    }

    public class TCPServer
    {
        private bool _done;
        public List<string> ReceivedData { get; }
        private readonly TcpListener _listener;

        public TCPServer(IPAddress ipaddress, int port)
        {
            _listener = new TcpListener(new IPEndPoint(ipaddress, port));
            ReceivedData = new List<string>();
        }

        public void Start()
        {
            Task.Run(() =>
            {
                _listener.Start();

                // Buffer for reading data
                var bytes = new byte[256];

                while (!_done)
                {
                    var client = _listener.AcceptTcpClient();

                    // Get a stream object for reading and writing
                    var stream = client.GetStream();

                    int i;

                    // Loop to receive all the data sent by the client.
                    while ((i = stream.Read(bytes, 0, bytes.Length)) != 0)
                    {
                        // Translate data bytes to a ASCII string.
                        var data = System.Text.Encoding.ASCII.GetString(bytes, 0, i);
                        ReceivedData.Add(data);
                    }

                    // Shutdown and end connection
                    client.Close();
                }
            });
        }

        public void Stop()
        {
            _done = true;
            _listener.Stop();
        }
    }
}
