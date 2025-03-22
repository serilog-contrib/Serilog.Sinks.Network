using System;
using System.Net;
using FluentAssertions;
using Serilog.Core;
using Serilog.Formatting;
using Serilog.Sinks.Network.Formatters;
using Xunit;

namespace Serilog.Sinks.Network.Test
{
    public class JsonFormatter
    {
        private TCPServer _server;
        private Logger _logger;
        

        private void ConfigureTestLogger(ITextFormatter formatter = null)
        {
            var port = new Random().Next(50003) + 10000;
            _server = new TCPServer(IPAddress.Loopback, port);
            _server.Start();

            _logger = new LoggerConfiguration()
                .WriteTo.TCPSink(IPAddress.Loopback, port, formatter)
                .CreateLogger();
        }
        
        [Fact]
        public void MustNotLogATrailingCommaWhenThereAreNoProperties()
        {
            ConfigureTestLogger(new LogstashJsonFormatter());
            var arbitraryMessage = nameof(JsonFormatter) + "MustNotLogATrailingCommaWhenThereAreNoProperties" + Guid.NewGuid();
            
            _logger.Information(arbitraryMessage);

            var receivedData = ServerPoller.PollForReceivedData(_server);
            var loggedData = receivedData?.TrimEnd('\n');

            var logMessageWithTrailingComma = $"\"message\":\"{arbitraryMessage}\",}}";
            loggedData.Should().NotEndWith(logMessageWithTrailingComma);
        }
        
        [Fact]
        public void CanStillLogMessagesWithExceptions()
        {
            ConfigureTestLogger(new LogstashJsonFormatter());
            var arbitraryMessage = nameof(JsonFormatter) + "CanStillLogMessagesWithExceptions" + Guid.NewGuid();
            
            _logger.Information(new Exception("exploding"), arbitraryMessage);

            var receivedData = ServerPoller.PollForReceivedData(_server);

            receivedData.Should().Contain("\"exception\":\"System.Exception: exploding\"}");
        }
    }
}