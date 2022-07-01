# Serilog Network Sink

[![Build status](https://ci.appveyor.com/api/projects/status/dw7y9d3q9ty7cm5h?svg=true)](https://ci.appveyor.com/project/pauldambra/serilog-sinks-network)

Writes the JSON output from serilog log event to either UDP or TCP

# Versions

Serilog Network Sink is targeted **NetStandard 1.3** from version 2.x. It can be used for **.NET Core** based projects.

1.x versions are still targeted to standard .Net Framework 4.5

# Usage

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

# Usage with Fluentd input_forwarder plugin (json mode)

Set up to log to Fluentd via TCP

```csharp

var urlLogger = new LoggerConfiguration()
    .WriteTo.FluentdTCPSink("127.0.0.1", 24224, "applogs")
    .CreateLogger();

```
Set up to log to Fluentd via UDP

```csharp

var urlLogger = new LoggerConfiguration()
    .WriteTo.FluentdUDPSink("127.0.0.1", 24224, "applogs")
    .CreateLogger();

```

Sample of fluentd.conf
```
<source>
  @type forward
  port 24224
</source>

<match applogs>
  @type copy
  <store>
    @type stdout
  </store>
  <store>
    @type elasticsearch
    host elastic
    port 9200
    logstash_format true
    logstash_prefix serilog
    buffer_type memory
    flush_interval 10s
    retry_limit 17
    retry_wait 1.0
    reload_connections false
    reconnect_on_error true
    reload_on_failure true
    request_timeout 300s
    num_threads 2
  </store>
</match>
```

# Acknowledgements

Adapted from [Serilog Splunk Sink](https://github.com/serilog/serilog-sinks-splunk) and [Splunk .Net Logging](https://github.com/splunk/splunk-library-dotnetlogging) both Apache 2.0 licensed
