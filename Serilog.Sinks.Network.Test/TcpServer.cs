using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace Serilog.Sinks.Network.Test
{
    public class TCPServer : DataReceiver
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
                try
                {
                    _listener.Start();
                    Console.WriteLine("Listener started on " + _listener.LocalEndpoint.ToString());
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex);
                }

                // Buffer for reading data
                var bytes = new byte[256];

                while (!_done)
                {
                    var client = _listener.AcceptTcpClientAsync().Result;

                    // Get a stream object for reading and writing
                    var stream = client.GetStream();

                    int i;

                    // Loop to receive all the data sent by the client.
                    while ((i = stream.Read(bytes, 0, bytes.Length)) != 0)
                    {
                        // Translate data bytes to a ASCII string.
                        var data = System.Text.Encoding.ASCII.GetString(bytes, 0, i);
                        ReceivedData.Add(data);
                        Console.WriteLine("message received: " + data);
                    }

                    // Shutdown and end connection
                    client.Dispose();
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