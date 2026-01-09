using System.Runtime.InteropServices;

namespace System.Network.EPoll.Architectures;

internal sealed partial class Linux_x64 : IArch<epoll_event_packed>
{
    [LibraryImport("libc", SetLastError = true)]
    public static partial nint epoll_create1(epoll_flags flags);

    [LibraryImport("libc", SetLastError = true)]
    public static partial int epoll_close(nint epfd);

    [LibraryImport("libc", SetLastError = true)]
    public static partial int epoll_ctl(nint epfd, epoll_op op, nint fd, ref epoll_event_packed ee);

    [LibraryImport("libc", SetLastError = true)]
    public static partial int epoll_wait(nint epfd, epoll_event_packed[] ee, int maxevents, int timeout);

    /// <summary>
    /// On Linux, EPOLL_CTL_DEL is synchronous, so socket is always immediately ready.
    /// </summary>
    public static int epoll_sock_is_ready(nint epfd, nint sock) => 0;

    private Linux_x64() { }
}
