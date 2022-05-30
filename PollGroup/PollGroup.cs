using System.Net.Sockets;
using System.Runtime.InteropServices;

namespace System.Network;

public interface IPollGroup : IDisposable
{
    void Add(Socket sock, GCHandle handle);
    void Remove(Socket sock, GCHandle handle);
    int Poll(int maxEvents);
    int Poll(int[] fds);
    int Poll(uint[] u32s);
    int Poll(ulong[] u64s);
    int Poll(IntPtr[] ptrs);
    int Poll(GCHandle[] handles);
}

public static class PollGroup
{
    public static IPollGroup Create()
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.FreeBSD) || RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            return new KQueuePollGroup();
        }

        return new EPollGroup();
    }
}
