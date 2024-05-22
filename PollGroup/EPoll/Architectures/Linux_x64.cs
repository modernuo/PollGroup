using System.Runtime.InteropServices;

namespace System.Network.EPoll.Architectures;

internal sealed partial class Linux_x64 : IArch<epoll_event_packed>
{
    [LibraryImport("libc", SetLastError = true)]
    public static partial int epoll_create1(epoll_flags flags);

    [LibraryImport("libc", SetLastError = true)]
    public static partial int epoll_close(int epfd);

    [LibraryImport("libc", SetLastError = true)]
    public static partial int epoll_ctl(int epfd, epoll_op op, int fd, ref epoll_event_packed ee);

    [LibraryImport("libc", SetLastError = true)]
    public static partial int epoll_wait(int epfd, epoll_event_packed[] ee, int maxevents, int timeout);

    private Linux_x64() { }
}
