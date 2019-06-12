using System;
using System.Net.Sockets;
using FluentAssertions;
using Serilog.Sinks.Network.Sinks.TCP;
using Xunit;

namespace Serilog.Sinks.Network.Test
{
    public class LoggingFailureHandlerTest
    {
        [Fact]
        public void canHandleAnExceptionWithNoInnerException()
        {
            var exception = new Exception("the message");
            TcpSocketWriter.UnexpectedErrorLogger(
                exception, 
                (x, socketError) =>
                {
                    x.Should().BeSameAs(exception);
                    socketError.Should().BeNull();
                });
        }
        
        [Fact]
        public void canHandleAnExceptionWithAnInnerException()
        {
            var exception = new Exception("the outer", new Exception("the inner"));
            TcpSocketWriter.UnexpectedErrorLogger(
                exception, 
                (x, socketError) =>
                {
                    x.Should().BeSameAs(exception);
                    socketError.Should().BeNull();
                });
        }
        
        [Fact]
        public void canHandleASocketExceptionWithNoInnerException()
        {
            var exception = new SocketException(997);
            TcpSocketWriter.UnexpectedErrorLogger(
                exception, 
                (x, socketError) =>
                {
                    x.Should().BeSameAs(exception);
                    socketError.Should().Be((SocketError) 997);
                });
        }
        
        [Fact]
        public void canHandleASocketExceptionWithAnInnerException()
        {
            var exception = new Exception("the outer", new SocketException(10044));
            TcpSocketWriter.UnexpectedErrorLogger(
                exception, 
                (x, socketError) =>
                {
                    x.Should().BeSameAs(exception);
                    socketError.Should().Be((SocketError) 10044);
                });
        }
    }
}