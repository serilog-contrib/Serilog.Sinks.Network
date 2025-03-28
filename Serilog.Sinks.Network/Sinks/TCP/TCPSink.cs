using System;
using System.IO;
using System.Net;
using System.Text;
using Serilog.Core;
using Serilog.Events;
using Serilog.Formatting;

namespace Serilog.Sinks.Network.Sinks.TCP
{
    public class TCPSink : ILogEventSink, IDisposable
    {
        private readonly ITextFormatter _formatter;
        private readonly TcpSocketWriter _socketWriter;

        public TCPSink(Uri uri, ITextFormatter formatter)
            : this(new TcpSocketWriter(uri), formatter)
        {
        }

        public TCPSink(TcpSocketWriter socketWriter, ITextFormatter formatter)
        {
            _socketWriter = socketWriter;
            _formatter = formatter;
        }

        public void Emit(LogEvent logEvent)
        {
            var sb = new StringBuilder();

            using (var sw = new StringWriter(sb))
                _formatter.Format(logEvent, sw);

            sb.Replace("RenderedMessage", "message");
            _socketWriter.Enqueue(sb.ToString());
        }

        public void Dispose()
        {
            _socketWriter.Dispose();
        }
    }
}