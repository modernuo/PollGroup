using System.Runtime.InteropServices;

namespace System.Network.EPoll.Architectures;

internal sealed partial class Win_x64 : IArch<epoll_event_packed>
{
    [LibraryImport("wepoll.dll", SetLastError = true)]
    public static partial nint epoll_create1(epoll_flags flags);

    [LibraryImport("wepoll.dll", SetLastError = true)]
    public static partial int epoll_close(nint epfd);

    [LibraryImport("wepoll.dll", SetLastError = true)]
    public static partial int epoll_ctl(nint epfd, epoll_op op, nint fd, ref epoll_event_packed ee);

    [LibraryImport("wepoll.dll", SetLastError = true)]
    public static partial int epoll_wait(nint epfd, epoll_event_packed[] ee, int maxevents, int timeout);

    /// <summary>
    /// Check if a socket is ready after being removed from an epoll instance.
    /// When epoll_ctl(EPOLL_CTL_DEL) is called on a socket with a pending poll
    /// operation, the poll is cancelled asynchronously. This function allows
    /// checking whether the cancellation has completed, which is necessary
    /// before adding the socket to another epoll instance.
    /// </summary>
    /// <param name="epfd">The epoll handle</param>
    /// <param name="sock">The socket to check</param>
    /// <returns>0 if socket is ready for migration, -1 if still pending (errno = EAGAIN)</returns>
    [LibraryImport("wepoll.dll", SetLastError = true)]
    public static partial int epoll_sock_is_ready(nint epfd, nint sock);

    private Win_x64() { }
}
