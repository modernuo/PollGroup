using System.Runtime.InteropServices;

namespace System.Network.EPoll.Architectures;

internal sealed partial class Linux_arm64 : IArch<epoll_event>
{
    [LibraryImport("libc", SetLastError = true)]
    public static partial int epoll_create1(epoll_flags flags);

    [LibraryImport("libc", SetLastError = true)]
    public static partial int epoll_close(int epfd);

    [LibraryImport("libc", SetLastError = true)]
    public static partial int epoll_ctl(int epfd, epoll_op op, nint fd, ref epoll_event ee);

    [LibraryImport("libc", SetLastError = true)]
    public static partial int epoll_wait(int epfd, epoll_event[] ee, int maxevents, int timeout);

    private Linux_arm64() { }
}
