using System.Net.Sockets;
using System.Runtime.InteropServices;

namespace System.Network;

public interface IPollGroup : IDisposable
{
    void Add(Socket sock, GCHandle handle);
    void Remove(Socket sock, GCHandle handle);
    int Poll(int maxEvents);
    int Poll(nint[] ptrs);
    int Poll(GCHandle[] handles);

    /// <summary>
    /// Check if a socket is ready for migration after being removed.
    /// On Windows (wepoll), epoll_ctl(EPOLL_CTL_DEL) cancels poll asynchronously,
    /// so we need to wait for completion before adding to another poll group.
    /// On Linux/macOS, removal is synchronous so this always returns true.
    /// </summary>
    /// <param name="sock">The socket to check</param>
    /// <returns>true if the socket is ready for migration, false if still pending</returns>
    bool IsSocketReady(Socket sock);
}
