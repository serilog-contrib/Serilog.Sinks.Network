using System;
using System.Linq;
using System.Net;
using Serilog.Configuration;
using Serilog.Debugging;
using Serilog.Events;
using Serilog.Sinks.Network.Sinks.TCP;
using Serilog.Sinks.Network.Sinks.UDP;

namespace Serilog.Sinks.Network
{
    /// <summary>
    ///     Extends Serilog configuration to write events to the network.
    /// </summary>
    public static class NetworkLoggerConfigurationExtensions
    {
        public static LoggerConfiguration UDPSink(
            this LoggerSinkConfiguration loggerConfiguration,
            string uri,
            int port,
            LogEventLevel restrictedToMinimumLevel = LevelAlias.Minimum)
        {
            var sink = new UDPSink(ResolveAddress(uri), port);
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
            var sink = new TCPSink(ResolveAddress(uri), port);
            return loggerConfiguration.Sink(sink, restrictedToMinimumLevel);
        }

        private static IPAddress ResolveAddress(string uri)
        {
            // Check if it is IP address
            IPAddress address;

            if (IPAddress.TryParse(uri, out address))
                return address;

            address = ResolveIP(uri);
            if (address != null)
                return address;

            SelfLog.WriteLine("Unable to determine the destination IP-Address");
            return IPAddress.Loopback;
        }

        private static IPAddress ResolveIP(string uri)
        {
            try
            {
                var ipHostEntry = Dns.GetHostEntry(uri);
                if (!ipHostEntry.AddressList.Any())
                    return null;
                return ipHostEntry.AddressList.First();
            }
            catch (Exception)
            {
                SelfLog.WriteLine("Could not resolve " + uri);
                return null;
            }
        }
    }
}