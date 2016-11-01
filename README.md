# Serilog Network Sink

[![Build status](https://ci.appveyor.com/api/projects/status/dw7y9d3q9ty7cm5h?svg=true)](https://ci.appveyor.com/project/pauldambra/serilog-sinks-network)


Writes the JSON output from serilog log event to either UDP or TCP

Set up to log via TCP

```csharp
var ip = IPAddress.Parse("1.3.3.7");
var log = new LoggerConfiguration()
    .WriteTo.TCPSink(ip, 1337)
    .CreateLogger();

var urlLogger = new LoggerConfiguration()
    .WriteTo.TCPSink("some.url.com", 1337)
    .CreateLogger();
```

Or maybe UDP

```csharp
var ip = IPAddress.Parse("1.3.3.7");
var log = new LoggerConfiguration()
    .WriteTo.UDPSink(ip, 1337)
    .CreateLogger();

var urlLogger = new LoggerConfiguration()
    .WriteTo.UDPSink("some.url.com", 1337)
    .CreateLogger();
```

# Acknowledgements

Adapted from [Serilog Splunk Sink](https://github.com/serilog/serilog-sinks-splunk) and [Splunk .Net Logging](https://github.com/splunk/splunk-library-dotnetlogging) both Apache 2.0 licensed
