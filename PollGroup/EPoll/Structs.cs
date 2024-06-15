using System.Network.EPoll;
using System.Runtime.InteropServices;

namespace System.Network;

#pragma warning disable IDE1006 // Naming Styles

[Flags]
internal enum epoll_flags
{
    NONE = 0,
    CLOEXEC = 0x02000000,
    NONBLOCK = 0x04000,
}

[Flags]
internal enum epoll_events : uint
{
    EPOLLIN = 0x001,
    EPOLLPRI = 0x002,
    EPOLLOUT = 0x004,
    EPOLLRDNORM = 0x040,
    EPOLLRDBAND = 0x080,
    EPOLLWRNORM = 0x100,
    EPOLLWRBAND = 0x200,
    EPOLLMSG = 0x400,
    EPOLLERR = 0x008,
    EPOLLHUP = 0x010,
    EPOLLRDHUP = 0x2000,
    EPOLLONESHOT = 1 << 30,
    EPOLLET = unchecked((uint)(1 << 31))
}

internal enum epoll_op
{
    EPOLL_CTL_ADD = 1,
    EPOLL_CTL_DEL = 2,
    EPOLL_CTL_MOD = 3,
}

[StructLayout(LayoutKind.Explicit, Size = 8)]
internal struct epoll_data
{
    [FieldOffset(0)]
    public int fd;
    [FieldOffset(0)]
    public nint ptr;
    [FieldOffset(0)]
    public uint u32;
    [FieldOffset(0)]
    public ulong u64;
}

[StructLayout(LayoutKind.Explicit, Pack = 4)]
internal struct epoll_event_packed : IEpollEvent
{
    [FieldOffset(0)]
    private epoll_events _events;
    [FieldOffset(4)]
    private epoll_data _data;

    public epoll_events Events
    {
        get => _events;
        set => _events = value;
    }

    public nint Ptr
    {
        get => _data.ptr;
        set => _data.ptr = value;
    }
}

[StructLayout(LayoutKind.Sequential)]
internal struct epoll_event : IEpollEvent
{
    private epoll_events _events;
    private epoll_data _data;

    public epoll_events Events
    {
        get => _events;
        set => _events = value;
    }

    public nint Ptr
    {
        get => _data.ptr;
        set => _data.ptr = value;
    }
}

#pragma warning restore IDE1006 // Naming Styles
