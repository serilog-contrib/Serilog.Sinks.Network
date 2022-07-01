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
using System.IO;
using Serilog.Events;
using Serilog.Formatting;
using Serilog.Formatting.Json;

namespace Serilog.Sinks.Network.Formatters
{
    public class FluentdJsonFormatter : ITextFormatter
    {
        private readonly JsonFormatter jsonFormatter;
        private string _defaultTag;

        public FluentdJsonFormatter(string defaultTag)
        {
            jsonFormatter = new JsonFormatter();
            _defaultTag = "\"" + defaultTag + "\"";
        }

        public void Format(LogEvent logEvent, TextWriter output)
        {
            using (TextWriter logEventWriter = new StringWriter())
            {
                jsonFormatter.Format(logEvent, logEventWriter);
                if (logEvent.Properties.TryGetValue("SourceContext", out var sourceContext))
                {
                    _defaultTag = sourceContext.ToString();
                }

                output.Write($"[{_defaultTag}, {DateTimeOffset.Now.ToEpochTime()}, {logEventWriter}]");
            }
        }
    }
}