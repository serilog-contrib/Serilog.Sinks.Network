using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using Serilog.Core;
using Serilog.Events;
using Serilog.Formatting.Json;

namespace Serilog.Sinks.Network.Sinks.UDP
{
    public class UDPSink : ILogEventSink, IDisposable
    {
        private Socket _socket = new Socket(SocketType.Dgram, ProtocolType.Udp);
        private readonly JsonFormatter _formatter;

        public UDPSink(IPAddress ipAddress, int port)
        {
            _socket.Connect(ipAddress, port);
            _formatter = new JsonFormatter(false, null, true);
        }

        public void Emit(LogEvent logEvent)
        {
            var sb = new StringBuilder();

            using (var sw = new StringWriter(sb))
                _formatter.Format(logEvent, sw);

            var result = sb.ToString();
            result = result.Replace("RenderedMessage", "message");

            _socket.Send(Encoding.UTF8.GetBytes(result));
        }

        public void Dispose()
        {
            _socket?.Close();
            _socket?.Dispose();
            _socket = null;
        }
    }
}
