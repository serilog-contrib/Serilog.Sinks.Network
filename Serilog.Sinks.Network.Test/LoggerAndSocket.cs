using System.Net.Sockets;

namespace Serilog.Sinks.Network.Test
{
    public record LoggerAndSocket : System.IDisposable
    {
        public required ILogger Logger;
        public required Socket Socket;
        public void Dispose()
        {
            Socket.Dispose();
        }
    }

}