using System.Net.Sockets;
using System.Runtime.InteropServices;

namespace PollGroup;

public class EpollPollGroup : IPollGroup, IDisposable
{
    [Flags]
    private enum epoll_flags : int
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

    // On Windows, we must use the wepoll library
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

    private int _ephnd;

    private bool _isWindows;

    public EpollPollGroup()
    {
        _isWindows = RuntimeInformation.IsOSPlatform(OSPlatform.Windows);

        if (_isWindows)
        {
            _ephnd = Windows.epoll_create1(epoll_flags.NONE);
        }
        else
        {
            _ephnd = Linux.epoll_create1(epoll_flags.NONE);
        }

        if (_ephnd == 0)
        {
            throw new Exception("Unable to initialize poll group");
        }
    }

    public void Dispose()
    {
        if (_isWindows)
        {
            Windows.epoll_close(_ephnd);
        }
        else
        {
            Linux.epoll_close(_ephnd);
        }
    }

    public void Add(Socket sock, GCHandle handle)
    {
        var ev = new epoll_event();
        ev.events = epoll_events.EPOLLIN | epoll_events.EPOLLERR;
        ev.data.ptr = (IntPtr)handle;

        int rc;
        if (_isWindows)
        {
            rc = Windows.epoll_ctl(_ephnd, epoll_op.EPOLL_CTL_ADD, (int)sock.Handle, ref ev);
        }
        else
        {
            rc = Linux.epoll_ctl(_ephnd, epoll_op.EPOLL_CTL_ADD, (int)sock.Handle, ref ev);
        }

        if (rc != 0)
        {
            throw new Exception($"epoll_ctl failed with error code {Marshal.GetLastWin32Error()}");
        }
    }

    public void Remove(Socket sock)
    {
        var ev = new epoll_event();
        ev.events = epoll_events.EPOLLIN | epoll_events.EPOLLERR;

        int rc;
        if (_isWindows)
        {
            rc = Windows.epoll_ctl(_ephnd, epoll_op.EPOLL_CTL_DEL, (int)sock.Handle, ref ev);
        }
        else
        {
            rc = Linux.epoll_ctl(_ephnd, epoll_op.EPOLL_CTL_DEL, (int)sock.Handle, ref ev);
        }

        if (rc != 0)
        {
            throw new Exception($"epoll_ctl failed with error code {Marshal.GetLastWin32Error()}");
        }
    }

    private epoll_event[] _events = new epoll_event[2048];

    public int Poll(ref GCHandle[] handles)
    {
        int rc;

        if (handles.Length > _events.Length)
        {
            _events = new epoll_event[handles.Length];
        }

        if (_isWindows)
        {
            rc = Windows.epoll_wait(_ephnd, _events, handles.Length, 0);
        }
        else
        {
            rc = Linux.epoll_wait(_ephnd, _events, handles.Length, 0);
        }

        if (rc <= 0)
        {
            return rc;
        }

        for (int i = 0; i < rc; i++)
        {
            handles[i] = ((GCHandle)_events[i].data.ptr);
        }

        return rc;
    }
}