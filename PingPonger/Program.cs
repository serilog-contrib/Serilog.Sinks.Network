using System;
using System.Linq;
using System.Net;
using System.Threading;
using CommandLine;
using Serilog;
using Serilog.Sinks.Network;
using Serilog.Sinks.Network.Formatters;

namespace PingPonger
{
    internal class Options
    {
        [Option('u', "udp", Required = false,
                HelpText = "Ping Pong over UDP")]
        public bool UDP { get; set; }

        [Option('t', "tcp", Required = false,
                HelpText = "Ping Pong over TCP")]
        public bool TCP { get; set; }

        [Option('i', "ip", Required = false,
                HelpText = "IP to send to")]
        public string? IP { get; set; }

        [Option('l', "url", Required = false,
        HelpText = "URL to send to")]
        public string? Url { get; set; }

        [Option('p', "port", Required = false,
                HelpText = "the Port to send to")]
        public int Port { get; set; }
    }

    public class Program
    {
        public static int Main(string[] args)
        {
            try
            {
                var options = new Options();
                var parseResult = Parser.Default.ParseArguments<Options>(args)
                    .WithNotParsed(errs =>
                    {
                        Console.WriteLine("Failed parsing command line arguments:");
                        errs.ToList().ForEach(e => Console.WriteLine(@"error: {0}", e));
                        args.ToList().ForEach(a => Console.WriteLine(@"arg: {0}", a));
                    })
                    .WithParsed(opts => options = opts);

                if (parseResult.Tag == ParserResultType.NotParsed)
                {
                    return 1;
                }

                if ((!options.UDP && !options.TCP) || (options.TCP && options.UDP))
                {
                    Console.WriteLine("You must select either TCP or UDP");
                    return 1;
                }

                var logConfig = new LoggerConfiguration();

                logConfig.Enrich.WithProperty("application", "woodlandtrust_website");
                logConfig.Enrich.WithProperty("type", "SiteMapGenerator");
                logConfig.Enrich.WithProperty("environment", "production");

                logConfig.WriteTo.ColoredConsole();

                if (options.Url?.Length > 0)
                {
                    if (options.UDP)
                    {
                        logConfig.WriteTo.UDPSink(options.Url, options.Port);
                    }
                    if (options.TCP)
                    {
                        if (options.Port != 0)
                        {
                            logConfig.WriteTo.TCPSink(options.Url, options.Port);
                        }
                        else
                        {
                            // URL option value may already have a port in it, e.g. "tcp://localhost:8888"
                            logConfig.WriteTo.TCPSink(options.Url);
                        }
                    }
                }
                else if (options.IP?.Length > 0)
                {
                    if (!IPAddress.TryParse(options.IP, out var ipAddress))
                    {
                        Console.WriteLine("Could not parse " + options.IP + " as an IP address");
                        return 1;
                    }

                    if (options.UDP)
                    {
                        logConfig.WriteTo.UDPSink(ipAddress, options.Port);
                    }
                    if (options.TCP)
                    {
                        logConfig.WriteTo.TCPSink(ipAddress, options.Port);
                    }
                }
                else
                {
                    Console.WriteLine("You must provide a URL or IP to connect to");
                    return 1;
                }


                var logger = logConfig.CreateLogger();

                var cancelled = false;
                Console.CancelKeyPress += delegate { cancelled = true; };

                var i = 0;
                while (!cancelled)
                {
                    logger.Information("ping: {ping} and pong: {pong}", i++, i++);
                    Thread.Sleep(1500);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("ping ponging failed");
                Console.WriteLine(ex);
                return 1;
            }

            return 0;
        }
    }
}
