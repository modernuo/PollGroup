using System.Runtime.InteropServices;

namespace System.Network.EPoll.Architectures;

internal sealed partial class Win_x64 : IPackedArch
{
    [LibraryImport("wepoll.dll", SetLastError = true)]
    public static partial int epoll_create1(epoll_flags flags);

    [LibraryImport("wepoll.dll", SetLastError = true)]
    public static partial int epoll_close(int epfd);

    [LibraryImport("wepoll.dll", SetLastError = true)]
    public static partial int epoll_ctl(int epfd, epoll_op op, int fd, ref epoll_event_packed ee);

    [LibraryImport("wepoll.dll", SetLastError = true)]
    public static partial int epoll_wait(int epfd, epoll_event_packed[] ee, int maxevents, int timeout);

    private Win_x64() { }
}
