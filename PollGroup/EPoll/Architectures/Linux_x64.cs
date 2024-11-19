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

    private Linux_x64() { }
}
