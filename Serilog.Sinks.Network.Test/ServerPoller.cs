using System;
using System.Diagnostics;
using System.Linq;

namespace Serilog.Sinks.Network.Test
{
    internal static class ServerPoller
    {
        public static string PollForReceivedData(DataReceiver dataReceiver)
        {
            var stopwatch = Stopwatch.StartNew();
            string receivedData = null;
            while (string.IsNullOrEmpty(receivedData))
            {
                receivedData = dataReceiver.ReceivedData.SingleOrDefault();
                if (stopwatch.Elapsed > TimeSpan.FromSeconds(5))
                {
                    throw new NoDataReceivedWithinThreeSeconds();
                }
            }

            return receivedData;
        }
    }

    internal class NoDataReceivedWithinThreeSeconds : Exception
    {
    }
}