using System.Net.Sockets;
using System.Runtime.InteropServices;

namespace System.Network;

public sealed class EPollGroup : IPollGroup
{
    [Flags]
    private enum epoll_flags
    {
        NONE = 0,
        CLOEXEC = 0x02000000,
        NONBLOCK = 0x04000,
    }

    [Flags]
    private enum epoll_events : uint
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

    private enum epoll_op
    {
        EPOLL_CTL_ADD = 1,
        EPOLL_CTL_DEL = 2,
        EPOLL_CTL_MOD = 3,
    }

    [StructLayout(LayoutKind.Explicit, Size = 8)]
    private struct epoll_data
    {
        [FieldOffset(0)]
        public int fd;
        [FieldOffset(0)]
        public IntPtr ptr;
        [FieldOffset(0)]
        public uint u32;
        [FieldOffset(0)]
        public ulong u64;
    }

    [StructLayout(LayoutKind.Explicit, Pack = 4)]
    private struct epoll_event
    {
        [FieldOffset(0)]
        public epoll_events events;
        [FieldOffset(4)]
        public epoll_data data;
    }

    private static class Windows
    {
        [DllImport("wepoll.dll", SetLastError = true)]
        public static extern int epoll_create1(epoll_flags flags);

        [DllImport("wepoll.dll", SetLastError = true)]
        public static extern int epoll_close(int epfd);

        [DllImport("wepoll.dll", SetLastError = true)]
        public static extern int epoll_ctl(int epfd, epoll_op op, int fd, ref epoll_event ee);

        [DllImport("wepoll.dll", SetLastError = true)]
        public static extern int epoll_wait(int epfd, [In, Out] epoll_event[] ee, int maxevents, int timeout);
    }

    private static class Linux
    {
        [DllImport("libc", SetLastError = true)]
        public static extern int epoll_create1(epoll_flags flags);

        [DllImport("libc", SetLastError = true)]
        public static extern int epoll_close(int epfd);

        [DllImport("libc", SetLastError = true)]
        public static extern int epoll_ctl(int epfd, epoll_op op, int fd, ref epoll_event ee);

        [DllImport("libc", SetLastError = true)]
        public static extern int epoll_wait(int epfd, [In, Out] epoll_event[] ee, int maxevents, int timeout);
    }

    private readonly int _epHndle;
    private readonly bool _isWindows;

    public EPollGroup()
    {
        _isWindows = RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
        _epHndle = _isWindows ? Windows.epoll_create1(epoll_flags.NONE) : Linux.epoll_create1(epoll_flags.NONE);

        if (_epHndle == 0)
        {
            throw new Exception("Unable to initialize poll group");
        }
    }

    public void Dispose()
    {
        _ = _isWindows ? Windows.epoll_close(_epHndle) : Linux.epoll_close(_epHndle);
    }

    public void Add(Socket socket, GCHandle handle)
    {
        var ev = new epoll_event
        {
            events = epoll_events.EPOLLIN | epoll_events.EPOLLERR
        };
        ev.data.ptr = (IntPtr)handle;

        var rc = _isWindows ?
            Windows.epoll_ctl(_epHndle, epoll_op.EPOLL_CTL_ADD, (int)socket.Handle, ref ev) :
            Linux.epoll_ctl(_epHndle, epoll_op.EPOLL_CTL_ADD, (int)socket.Handle, ref ev);

        if (rc != 0)
        {
            throw new Exception($"epoll_ctl failed with error code {Marshal.GetLastWin32Error()}");
        }
    }

    public void Remove(Socket socket, GCHandle handle)
    {
        var ev = new epoll_event
        {
            events = epoll_events.EPOLLIN | epoll_events.EPOLLERR
        };
        ev.data.ptr = (IntPtr)handle;

        var rc = _isWindows ?
            Windows.epoll_ctl(_epHndle, epoll_op.EPOLL_CTL_DEL, (int)socket.Handle, ref ev) :
            Linux.epoll_ctl(_epHndle, epoll_op.EPOLL_CTL_DEL, (int)socket.Handle, ref ev);

        if (rc != 0)
        {
            throw new Exception($"epoll_ctl failed with error code {Marshal.GetLastWin32Error()}");
        }
    }

    private epoll_event[] _events = new epoll_event[2048];

    public int Poll(int maxEvents)
    {
        if (maxEvents > _events.Length)
        {
            var newLength = Math.Max(maxEvents, _events.Length + (_events.Length >> 2));
            _events = new epoll_event[newLength];
        }

        return _isWindows ?
            Windows.epoll_wait(_epHndle, _events, maxEvents, 0) :
            Linux.epoll_wait(_epHndle, _events, maxEvents, 0);
    }
    
    public int Poll(int[] fds)
    {
        var rc = Poll(fds.Length);
        
        if (rc <= 0)
        {
            return rc;
        }

        for (var i = 0; i < rc; i++)
        {
            fds[i] = _events[i].data.fd;
        }

        return rc;
    }
    
    public int Poll(uint[] u32s)
    {
        var rc = Poll(u32s.Length);

        if (rc <= 0)
        {
            return rc;
        }

        for (var i = 0; i < rc; i++)
        {
            u32s[i] = _events[i].data.u32;
        }

        return rc;
    }
    
    public int Poll(ulong[] u64s)
    {
        var rc = Poll(u64s.Length);
        
        if (rc <= 0)
        {
            return rc;
        }

        for (var i = 0; i < rc; i++)
        {
            u64s[i] = _events[i].data.u64;
        }

        return rc;
    }
    
    public int Poll(GCHandle[] handles)
    {
        var rc = Poll(handles.Length);
        
        if (rc <= 0)
        {
            return rc;
        }
    
        for (var i = 0; i < rc; i++)
        {
            handles[i] = (GCHandle)_events[i].data.ptr;
        }
    
        return rc;
    }
    
    public int Poll(IntPtr[] ptrs)
    {
        var rc = Poll(ptrs.Length);
        
        if (rc <= 0)
        {
            return rc;
        }
    
        for (var i = 0; i < rc; i++)
        {
            ptrs[i] = _events[i].data.ptr;
        }
    
        return rc;
    }
}
