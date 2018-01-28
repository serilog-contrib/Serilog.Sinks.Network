using System.Collections.Generic;

namespace Serilog.Sinks.Network.Test
{
    public interface DataReceiver
    {
        List<string> ReceivedData { get; }
    }
}