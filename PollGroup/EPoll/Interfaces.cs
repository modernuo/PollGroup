using System.Runtime.InteropServices;

namespace System.Network.EPoll;

#pragma warning disable IDE1006 // Naming Styles

internal interface IArch<TEvent> where TEvent : struct, IEpollEvent
{
    public abstract static nint epoll_create1(epoll_flags flags);
    public abstract static int epoll_close(nint epfd);
    public abstract static int epoll_ctl(nint epfd, epoll_op op, nint fd, ref TEvent ee);
    public abstract static int epoll_wait(nint epfd, [In, Out] TEvent[] ee, int maxevents, int timeout);

    /// <summary>
    /// Check if a socket is ready after removal. On Linux this always returns 0 (ready).
    /// On Windows (wepoll), this checks if the async poll cancellation completed.
    /// </summary>
    public abstract static int epoll_sock_is_ready(nint epfd, nint sock);
}

internal interface IEpollEvent
{
    public epoll_events Events { get; set; }
    public nint Ptr { get; set; }
}

#pragma warning restore IDE1006 // Naming Styles
