using System.Net.Sockets;

namespace System.Runtime.CompilerServices
{
    public class RequiredMemberAttribute : Attribute { }
    public class CompilerFeatureRequiredAttribute : Attribute
    {
        public CompilerFeatureRequiredAttribute(string name) { }
    }
}
namespace System.Diagnostics.CodeAnalysis
{
    [System.AttributeUsage(System.AttributeTargets.Constructor, AllowMultiple = false, Inherited = false)]
    public sealed class SetsRequiredMembersAttribute : Attribute
    {
    }
}

namespace Serilog.Sinks.Network.Test
{
    public record LoggerAndSocket : System.IDisposable
    {
        public required ILogger Logger;
        public required Socket Socket;
        public void Dispose()
        {
            Socket.Dispose();
        }
    }

}