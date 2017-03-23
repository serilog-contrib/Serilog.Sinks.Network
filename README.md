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

var urlLogger = new LoggerConfiguration()
    .WriteTo.TCPSink("tls://some.fqdn.com:12435")
    .CreateLogger();

// you can provide any specific formatter ...
var urlLogger = new LoggerConfiguration()
    .WriteTo.TCPSink("tls://some.fqdn.com:12435", new RawFormatter())
    .CreateLogger();
     
// ... otherwise this will use the default provided LogstashJsonFormatter (described below)
var urlLogger = new LoggerConfiguration()
    .WriteTo.TCPSink("tls://some.fqdn.com:12435")
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
    
// you can provide any specific formatter for UDP too ...
var urlLogger = new LoggerConfiguration()
    .WriteTo.UDPSink("some.url.com", 1337, new RawFormatter())
    .CreateLogger();    
    
```

# Configure from the config file

```
<add key="serilog:minimum-level" value="Verbose" />
<add key="serilog:using:TCPSink" value="Serilog.Sinks.Network" />
<add key="serilog:write-to:TCPSink.uri" value="192.165.25.55" />
<add key="serilog:write-to:TCPSink.port" value="3251" />
```

# JSON structure (LogstashJsonFormatter)

Serilog log JSON tends to look like this:

```
{ 
  "Timestamp": "2016-11-03T16:28:55.0094294+00:00", 
  "Level": "Information", 
  "MessageTemplate": "ping: {ping} and pong: {pong}", 
  "message": "ping: 972 and pong: 973", 
  "Properties": { 
    "ping": 972, 
    "pong": 973, 
    "application": "ping ponger", 
    "type": "example", 
    "environment": "production" 
  } 
}

```
The LogstashJsonFormatter flattens that structure so it is more likely to fit into an existing logstash infrastructure. 

```

{
  "timestamp": "2016-11-03T16:28:55.0094294+00:00",
  "level": "Information",
  "message": "ping: 972 and pong: 973",
  "ping": 972,
  "pong": 973,
  "application": "ping ponger",
  "type": "example",
  "environment": "production",
}

```

# Acknowledgements

Adapted from [Serilog Splunk Sink](https://github.com/serilog/serilog-sinks-splunk) and [Splunk .Net Logging](https://github.com/splunk/splunk-library-dotnetlogging) both Apache 2.0 licensed
