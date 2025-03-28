using System;
using System.Linq;
using System.Net;
using Serilog.Configuration;
using Serilog.Debugging;
using Serilog.Events;
using Serilog.Formatting;
using Serilog.Sinks.Network.Formatters;
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
            ITextFormatter textFormatter = null,
            LogEventLevel restrictedToMinimumLevel = LevelAlias.Minimum)
        {
            return UDPSink(loggerConfiguration, ResolveAddress(uri), port, textFormatter, restrictedToMinimumLevel);
        }

        public static LoggerConfiguration UDPSink(
            this LoggerSinkConfiguration loggerConfiguration,
            IPAddress ipAddress,
            int port,
            ITextFormatter textFormatter = null,
            LogEventLevel restrictedToMinimumLevel = LevelAlias.Minimum)
        {
            var sink = new UDPSink(ipAddress, port, textFormatter ?? new LogstashJsonFormatter());
            return loggerConfiguration.Sink(sink, restrictedToMinimumLevel);
        }

        public static LoggerConfiguration TCPSink(
            this LoggerSinkConfiguration loggerConfiguration,
            IPAddress ipAddress,
            int port,
            int? writeTimeoutMs = null,
            int? disposeTimeoutMs = null,
            ITextFormatter textFormatter = null,
            LogEventLevel restrictedToMinimumLevel = LevelAlias.Minimum)
        {
            return TCPSink(loggerConfiguration, $"tcp://{ipAddress}:{port}", writeTimeoutMs, disposeTimeoutMs,  textFormatter, restrictedToMinimumLevel);
        }

        public static LoggerConfiguration TCPSink(
            this LoggerSinkConfiguration loggerConfiguration,
            string host,
            int port,
            int? writeTimeoutMs = null,
            int? disposeTimeoutMs = null,
            ITextFormatter textFormatter = null,
            LogEventLevel restrictedToMinimumLevel = LevelAlias.Minimum)
        {
            return TCPSink(loggerConfiguration, $"{host}:{port}", writeTimeoutMs, disposeTimeoutMs, textFormatter, restrictedToMinimumLevel);
        }

        public static LoggerConfiguration TCPSink(
            this LoggerSinkConfiguration loggerConfiguration,
            string uri,
            int? writeTimeoutMs = null,
            int? disposeTimeoutMs = null,
            ITextFormatter textFormatter = null,
            LogEventLevel restrictedToMinimumLevel = LevelAlias.Minimum)
        {
            var socketWriter = new TcpSocketWriter(
                BuildUri(uri),
                writeTimeoutMs: writeTimeoutMs,
                disposeTimeoutMs: disposeTimeoutMs);
            var sink = new TCPSink(socketWriter, textFormatter ?? new LogstashJsonFormatter());
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
                var ipHostEntry = Dns.GetHostEntryAsync(uri).Result;
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

        private static Uri BuildUri(string s)
        {
            Uri uri;
            try
            {
                uri = new Uri(s);
            }
            catch (UriFormatException ex)
            {
                throw new ArgumentNullException("Uri should be in the format tcp://server:port", ex);
            }
            if (uri.Port == 0)
                throw new UriFormatException("Uri port cannot be 0");
            if (!(uri.Scheme.ToLower() == "tcp" || uri.Scheme.ToLower() == "tls"))
                throw new UriFormatException("Uri scheme must be tcp or tls");
            return uri;
        }
    }
}