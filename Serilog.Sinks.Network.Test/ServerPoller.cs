using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Serilog.Sinks.Network.Test
{
    internal static class ServerPoller
    {
        public static async Task<string> PollForReceivedData(Socket socket, bool udp = false)
        {
            var buffer = new byte[1000];
            var cts = new CancellationTokenSource();
            cts.CancelAfter(TimeSpan.FromSeconds(30.0));
            var result = new List<byte>();

            Socket clientSocket;
            if (udp)
            {
                clientSocket = socket;
            }
            else
            {
                clientSocket = await socket.AcceptAsync(cts.Token);
            }
            var isDone = false;
            while (!isDone)
            {
                int readResult = await clientSocket.ReceiveAsync(buffer, SocketFlags.None, cts.Token);
                for (var i = 0; i < readResult; i++)
                {
                    result.Add(buffer[i]);
                }

                if (readResult < buffer.Length)
                {
                    isDone = true;
                }
            }
            
            return Encoding.ASCII.GetString(result.ToArray());
        }
    }
}