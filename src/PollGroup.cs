using System.Net.Sockets;
using System.Runtime.InteropServices;

namespace PollGroup;

public interface IPollGroup : IDisposable
{
    void Add(Socket sock, GCHandle handle);
    void Remove(Socket sock);
    int Poll(ref GCHandle[] handles);
}

public static class PollGroup
{
    public static IPollGroup Create()
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux) ||
            RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            return new EpollPollGroup();
        }

        throw new Exception("Unsupported platform");
    }
}
