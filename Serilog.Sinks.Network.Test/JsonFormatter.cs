using System;
using System.Net;
using System.Net.Sockets;
using FluentAssertions;
using Serilog.Core;
using Serilog.Formatting;
using Serilog.Sinks.Network.Formatters;
using Xunit;

namespace Serilog.Sinks.Network.Test
{
    public class JsonFormatter
    {
        private static LoggerAndSocket ConfigureTestLogger(ITextFormatter formatter = null)
        {
            var socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            socket.Bind(new IPEndPoint(IPAddress.Loopback, 0));
            socket.Listen();

            var logger = new LoggerConfiguration()
                .WriteTo.TCPSink(IPAddress.Loopback, ((IPEndPoint)socket.LocalEndPoint!).Port, null, null, formatter)
                .CreateLogger();
            
            return new LoggerAndSocket { Logger = logger, Socket = socket };
        }
        
        [Fact]
        public async void MustNotLogATrailingCommaWhenThereAreNoProperties()
        {
            using var fixture = ConfigureTestLogger(new LogstashJsonFormatter());
            var arbitraryMessage = nameof(JsonFormatter) + "MustNotLogATrailingCommaWhenThereAreNoProperties" + Guid.NewGuid();
            
            fixture.Logger.Information(arbitraryMessage);

            var receivedData = await ServerPoller.PollForReceivedData(fixture.Socket);
            var loggedData = receivedData?.TrimEnd('\n');

            var logMessageWithTrailingComma = $"\"message\":\"{arbitraryMessage}\",}}";
            loggedData.Should().NotEndWith(logMessageWithTrailingComma);
        }
        
        [Fact]
        public async void CanStillLogMessagesWithExceptions()
        {
            using var fixture = ConfigureTestLogger(new LogstashJsonFormatter());
            var arbitraryMessage = nameof(JsonFormatter) + "CanStillLogMessagesWithExceptions" + Guid.NewGuid();
            
            fixture.Logger.Information(new Exception("exploding"), arbitraryMessage);

            var receivedData = await ServerPoller.PollForReceivedData(fixture.Socket);

            receivedData.Should().Contain("\"exception\":\"System.Exception: exploding\"}");
        }
    }
}