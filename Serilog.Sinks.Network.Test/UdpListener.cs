using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace Serilog.Sinks.Network.Test
{
    public class UDPListener : DataReceiver
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
            Task.Run(async () =>
            {
                while (!_done)
                {
                    UdpReceiveResult receiveByteArray = await _listener.ReceiveAsync();
                    var receivedData = Encoding.ASCII.GetString(receiveByteArray.Buffer, 0, receiveByteArray.Buffer.Length);
                    ReceivedData.Add(receivedData);
                }
            });
        }

        public void Stop()
        {
            _done = true;
            _listener.Dispose();
        }
    }
}
