using System.Runtime.InteropServices;

namespace System.Network.EPoll;

#pragma warning disable IDE1006 // Naming Styles

internal interface IArch<TEvent> where TEvent : struct, IEpollEvent
{
    public abstract static int epoll_create1(epoll_flags flags);
    public abstract static int epoll_close(int epfd);
    public abstract static int epoll_ctl(int epfd, epoll_op op, int fd, ref TEvent ee);
    public abstract static int epoll_wait(int epfd, [In, Out] TEvent[] ee, int maxevents, int timeout);
}

internal interface IEpollEvent
{
    public epoll_events Events { get; init; }
    public nint Ptr { get; init; }
}

#pragma warning restore IDE1006 // Naming Styles
