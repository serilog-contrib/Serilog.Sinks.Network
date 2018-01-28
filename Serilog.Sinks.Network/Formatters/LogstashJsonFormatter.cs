// Adapted from RawJsonFormatter in Serilog.Sinks.Seq Copyright 2016 Serilog Contributors
// 
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
// 
//     http://www.apache.org/licenses/LICENSE-2.0
// 
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Serilog.Events;
using Serilog.Formatting;
using Serilog.Formatting.Json;

namespace Serilog.Sinks.Network.Formatters
{
  public class LogstashJsonFormatter : ITextFormatter
  {
    private static readonly JsonValueFormatter ValueFormatter = new JsonValueFormatter();
    
    public void Format(LogEvent logEvent, TextWriter output)
    {
      FormatContent(logEvent, output);
      output.WriteLine();
    }

    private static void FormatContent(LogEvent logEvent, TextWriter output)
    {
      if (logEvent == null) throw new ArgumentNullException(nameof(logEvent));
      if (output == null) throw new ArgumentNullException(nameof(output));

      output.Write('{');

      WritePropertyAndValue(output, "timestamp", logEvent.Timestamp.ToString("o"));
      output.Write(",");
      
      WritePropertyAndValue(output, "level", logEvent.Level.ToString());
      output.Write(",");
      
      WritePropertyAndValue(output, "message", logEvent.MessageTemplate.Render(logEvent.Properties));
      
      if (logEvent.Exception != null)
      {
        output.Write(",");
        WritePropertyAndValue(output, "exception", logEvent.Exception.ToString());
      }
      
      WriteProperties(logEvent.Properties, output);
      
      output.Write('}');
    }

    private static void WritePropertyAndValue(TextWriter output, string propertyKey, string propertyValue)
    {
      JsonValueFormatter.WriteQuotedJsonString(propertyKey, output);
      output.Write(":");
      JsonValueFormatter.WriteQuotedJsonString(propertyValue, output);
    }

    private static void WriteProperties(IReadOnlyDictionary<string, LogEventPropertyValue> properties, TextWriter output)
    {
      if (properties.Any()) output.Write(",");
      
      var precedingDelimiter = "";
      foreach (var property in properties)
      {
        output.Write(precedingDelimiter);
        precedingDelimiter = ",";

        var camelCasePropertyKey = property.Key[0].ToString().ToLower() + property.Key.Substring(1);
        JsonValueFormatter.WriteQuotedJsonString(camelCasePropertyKey, output);
        output.Write(':');
        ValueFormatter.Format(property.Value, output);
      }
    }
  }
}