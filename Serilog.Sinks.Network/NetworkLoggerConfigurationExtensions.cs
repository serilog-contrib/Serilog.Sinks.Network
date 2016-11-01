using System;
using System.Linq;
using System.Net;
using Serilog.Configuration;
using Serilog.Events;
using Serilog.Sinks.Network.Sinks.TCP;
using Serilog.Sinks.Network.Sinks.UDP;

namespace Serilog.Sinks.Network
{
    /// <summary>
    /// Extends Serilog configuration to write events to the network.
    /// </summary>
    public static class NetworkLoggerConfigurationExtensions
    {
        private static IPAddress ToIP(string uri)
        {
            var ipHostEntry = Dns.GetHostEntry(uri);
            if (!ipHostEntry.AddressList.Any())
            {
                throw new ArgumentException("Could not resolve " + uri);
            }
            return ipHostEntry.AddressList.First();
        }

        public static LoggerConfiguration UDPSink(
            this LoggerSinkConfiguration loggerConfiguration,
            string uri, 
            int port,
            LogEventLevel restrictedToMinimumLevel = LevelAlias.Minimum)
        {
            var sink = new UDPSink(ToIP(uri), port);
            return loggerConfiguration.Sink(sink, restrictedToMinimumLevel);
        }

        public static LoggerConfiguration UDPSink(
            this LoggerSinkConfiguration loggerConfiguration,
            IPAddress ipAddress,
            int port,
            LogEventLevel restrictedToMinimumLevel = LevelAlias.Minimum)
        {
            var sink = new UDPSink(ipAddress, port);

            return loggerConfiguration.Sink(sink, restrictedToMinimumLevel);
        }

        public static LoggerConfiguration TCPSink(
            this LoggerSinkConfiguration loggerConfiguration,
            IPAddress ipAddress,
            int port,
            LogEventLevel restrictedToMinimumLevel = LevelAlias.Minimum)
        {
            var sink = new TCPSink(ipAddress, port);

            return loggerConfiguration.Sink(sink, restrictedToMinimumLevel);
        }

        public static LoggerConfiguration TCPSink(
            this LoggerSinkConfiguration loggerConfiguration,
            string uri, 
            int port,
            LogEventLevel restrictedToMinimumLevel = LevelAlias.Minimum)
        {
            var sink = new TCPSink(ToIP(uri), port);
            return loggerConfiguration.Sink(sink, restrictedToMinimumLevel);
        }
    }
}
