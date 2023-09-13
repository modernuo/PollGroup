using System.Net.Sockets;
using System.Runtime.InteropServices;

namespace System.Network;

public sealed class EPollGroup : IPollGroup
{
    private enum SupportedArchitecture
    {
        Unknown,
        Windows,
        Linux_x64,
        Linux_arm64
    }

    private static class Windows
    {
        [DllImport("wepoll.dll", SetLastError = true)]
        public static extern int epoll_create1(epoll_flags flags);

        [DllImport("wepoll.dll", SetLastError = true)]
        public static extern int epoll_close(int epfd);

        [DllImport("wepoll.dll", SetLastError = true)]
        public static extern int epoll_ctl(int epfd, epoll_op op, int fd, ref epoll_event_packed ee);

        [DllImport("wepoll.dll", SetLastError = true)]
        public static extern int epoll_wait(int epfd, [In, Out] epoll_event_packed[] ee, int maxevents, int timeout);
    }

    private static class Linux_x64
    {
        [DllImport("libc", SetLastError = true)]
        public static extern int epoll_create1(epoll_flags flags);

        [DllImport("libc", SetLastError = true)]
        public static extern int epoll_close(int epfd);

        [DllImport("libc", SetLastError = true)]
        public static extern int epoll_ctl(int epfd, epoll_op op, int fd, ref epoll_event_packed ee);

        [DllImport("libc", SetLastError = true)]
        public static extern int epoll_wait(int epfd, [In, Out] epoll_event_packed[] ee, int maxevents, int timeout);
    }

    private static class Linux_aarch64
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
    private readonly SupportedArchitecture _supportedArchitecture;
    private readonly bool _isArm;

    public EPollGroup()
    {
        _isArm = RuntimeInformation.ProcessArchitecture is Architecture.Arm or Architecture.Arm64 or Architecture.Armv6;

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            _supportedArchitecture = SupportedArchitecture.Windows;
        }
        else if (_isArm)
        {
            _supportedArchitecture = SupportedArchitecture.Linux_arm64;
        }
        else
        {
            _supportedArchitecture = SupportedArchitecture.Linux_x64;
        }

        _epHndle = _supportedArchitecture switch
        {
            SupportedArchitecture.Windows => Windows.epoll_create1(epoll_flags.NONE),
            SupportedArchitecture.Linux_x64 => Linux_x64.epoll_create1(epoll_flags.NONE),
            SupportedArchitecture.Linux_arm64 => Linux_aarch64.epoll_create1(epoll_flags.NONE)
        };

        if (_isArm)
        {
            _events = new epoll_event[2048];
        }
        else
        {
            _eventsPacked = new epoll_event_packed[2048];
        }

        if (_epHndle == 0)
        {
            throw new Exception("Unable to initialize poll group");
        }
    }

    public void Dispose()
    {
        _ = _supportedArchitecture switch
        {
            SupportedArchitecture.Windows     => Windows.epoll_close(_epHndle),
            SupportedArchitecture.Linux_x64   => Linux_x64.epoll_close(_epHndle),
            SupportedArchitecture.Linux_arm64 => Linux_aarch64.epoll_close(_epHndle)
        };
    }

    public void Add(Socket socket, GCHandle handle)
    {
        int rc;

        if (_isArm)
        {
            var ev = new epoll_event
            {
                events = epoll_events.EPOLLIN | epoll_events.EPOLLERR
            };
            ev.data.ptr = (IntPtr)handle;

            rc = Linux_aarch64.epoll_ctl(_epHndle, epoll_op.EPOLL_CTL_ADD, (int)socket.Handle, ref ev);
        }
        else
        {
            var ev = new epoll_event_packed
            {
                events = epoll_events.EPOLLIN | epoll_events.EPOLLERR
            };
            ev.data.ptr = (IntPtr)handle;

            rc = _supportedArchitecture switch
            {
                SupportedArchitecture.Windows => Windows.epoll_ctl(
                    _epHndle,
                    epoll_op.EPOLL_CTL_ADD,
                    (int)socket.Handle,
                    ref ev
                ),
                // SupportedArchitecture.Linux_x64
                _ => Linux_x64.epoll_ctl(
                    _epHndle,
                    epoll_op.EPOLL_CTL_ADD,
                    (int)socket.Handle,
                    ref ev
                )
            };
        }

        if (rc != 0)
        {
            throw new Exception($"epoll_ctl failed with error code {Marshal.GetLastWin32Error()}");
        }
    }

    public void Remove(Socket socket, GCHandle handle)
    {
        int rc;

        if (_isArm)
        {
            var ev = new epoll_event
            {
                events = epoll_events.EPOLLIN | epoll_events.EPOLLERR
            };
            ev.data.ptr = (IntPtr)handle;

            rc = Linux_aarch64.epoll_ctl(_epHndle, epoll_op.EPOLL_CTL_DEL, (int)socket.Handle, ref ev);
        }
        else
        {
            var ev = new epoll_event_packed
            {
                events = epoll_events.EPOLLIN | epoll_events.EPOLLERR
            };
            ev.data.ptr = (IntPtr)handle;

            rc = _supportedArchitecture switch
            {
                SupportedArchitecture.Windows => Windows.epoll_ctl(
                    _epHndle,
                    epoll_op.EPOLL_CTL_DEL,
                    (int)socket.Handle,
                    ref ev
                ),
                // SupportedArchitecture.Linux_x64
                _ => Linux_x64.epoll_ctl(
                    _epHndle,
                    epoll_op.EPOLL_CTL_DEL,
                    (int)socket.Handle,
                    ref ev
                )
            };
        }

        if (rc != 0)
        {
            throw new Exception($"epoll_ctl failed with error code {Marshal.GetLastWin32Error()}");
        }
    }

    private epoll_event_packed[] _eventsPacked;
    private epoll_event[] _events;

    public int Poll(int maxEvents)
    {
        if (_isArm)
        {
            if (maxEvents > _events.Length)
            {
                var newLength = Math.Max(maxEvents, _events.Length + (_events.Length >> 2));
                _events = new epoll_event[newLength];
            }

            return Linux_aarch64.epoll_wait(_epHndle, _events, maxEvents, 0);
        }

        if (maxEvents > _events.Length)
        {
            var newLength = Math.Max(maxEvents, _eventsPacked.Length + (_eventsPacked.Length >> 2));
            _eventsPacked = new epoll_event_packed[newLength];
        }

        return _supportedArchitecture switch
        {
            SupportedArchitecture.Windows => Windows.epoll_wait(_epHndle, _eventsPacked, maxEvents, 0),
            // SupportedArchitecture.Linux_x64
            _ => Linux_x64.epoll_wait(_epHndle, _eventsPacked, maxEvents, 0)
        };
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
