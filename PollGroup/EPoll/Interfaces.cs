using System.Runtime.InteropServices;

namespace System.Network.EPoll;

#pragma warning disable IDE1006 // Naming Styles

internal interface IArch
{
    public abstract static int epoll_create1(epoll_flags flags);
    public abstract static int epoll_close(int epfd);
    public abstract static int epoll_ctl(int epfd, epoll_op op, int fd, ref epoll_event ee);
    public abstract static int epoll_wait(int epfd, [In, Out] epoll_event[] ee, int maxevents, int timeout);
}

internal interface IPackedArch
{
    public abstract static int epoll_create1(epoll_flags flags);
    public abstract static int epoll_close(int epfd);
    public abstract static int epoll_ctl(int epfd, epoll_op op, int fd, ref epoll_event_packed ee);
    public abstract static int epoll_wait(int epfd, [In, Out] epoll_event_packed[] ee, int maxevents, int timeout);
}

#pragma warning restore IDE1006 // Naming Styles
