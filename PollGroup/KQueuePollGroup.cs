using System.Net.Sockets;
using System.Runtime.InteropServices;

namespace System.Network;

public class KQueuePollGroup : IPollGroup
{
    [StructLayout(LayoutKind.Sequential)]
    private struct kevent
    {
        public IntPtr ident;
        public kqueue_filter filter;
        public kqueue_flags flags;
        public kqueue_fflags fflags;
        public IntPtr data;
        public IntPtr udata;
    }

    [Flags]
    private enum kqueue_filter : short
    {
        READ = -1,
        WRITE = -2,
        AIO = -3,
        VNODE = -4,
        PROC = -5,
        SIGNAL = -6,
        TIMER = -7,
        MACHPORT = -8,
        FS = -9,
        USER = -10,
        UNUSED = -11,
        VM = -12,
        EXCEPT = -15
    }

    [Flags]
    private enum kqueue_flags : ushort
    {
        ADD = 0x0001,
        DELETE = 0x0002,
        ENABLE = 0x0004,
        DISABLE = 0x0008,
        ONESHOT = 0x0010,
        CLEAR = 0x0020,
        RECEIPT = 0x0040,
        DISPATCH = 0x0080,
        UDATA_SPECIFIC = 0x0100,
        DISPATCH2 = (DISPATCH | UDATA_SPECIFIC),
        VANISHED = 0x0200,
        SYSFLAGS = 0xF000,
        FLAG0 = 0x1000,
        FLAG1 = 0x2000,
        EOF = 0x8000,
        ERROR = 0x4000
    }

    [Flags]
    private enum kqueue_fflags : uint
    {
        TRIGGER = 0x01000000,
        FFNOP = 0x00000000,
        FFAND = 0x40000000,
        FFOR = 0x80000000,
        FFCOPY = 0xc0000000,
        FFCTRLMASK = 0xc0000000,
        FFFLAGSMASK = 0x00ffffff,
        LOWAT = 0x00000001,
        DELETE = 0x00000001,
        WRITE = 0x00000002,
        EXTEND = 0x00000004,
        ATTRIB = 0x00000008,
        LINK = 0x00000010,
        RENAME = 0x00000020,
        REVOKE = 0x00000040,
        NONE = 0x00000080,
    }

    [StructLayout(LayoutKind.Sequential)]
    struct timespec
    {
        public long tv_sec;
        public long tv_nsec;

        public timespec(long sec, long nsec)
        {
            tv_sec = sec;
            tv_nsec = nsec;
        }
    }

    private static readonly kevent[] _singleEvent = new kevent[1];
    private static readonly IntPtr _zeroTimeoutPtr;
    // ReSharper disable once PrivateFieldCanBeConvertedToLocalVariable
    private static readonly timespec _zeroTimeout;

    static KQueuePollGroup()
    {
        _zeroTimeout = new timespec(0, 0);
        _zeroTimeoutPtr = Marshal.AllocHGlobal(Marshal.SizeOf<timespec>());
        Marshal.StructureToPtr(_zeroTimeout, _zeroTimeoutPtr, false);
    }

    private static class BSD
    {
        [DllImport ("libc", SetLastError = true)]
        public static extern int close (int fd);

        [DllImport("libc", SetLastError = true)]
        public static extern int kqueue();

        [DllImport("libc", SetLastError = true)]
        public static extern int kevent(int kq, kevent[]? changelist, int nchanges, [In, Out] kevent[]? eventlist, int nevents, IntPtr timeout);

        public static int kevent(
            int kq,
            IntPtr ident,
            kqueue_filter filter,
            kqueue_flags flags,
            kqueue_fflags fflags = 0,
            IntPtr data = default,
            IntPtr udata = default
        )
        {
            _singleEvent[0] = new kevent
            {
                ident = ident,
                filter = filter,
                flags = flags,
                fflags = fflags,
                data = data,
                udata = udata
            };

            var rc = kevent(kq, _singleEvent, 1, null, 0, _zeroTimeoutPtr);
            if (rc != 0)
            {
                throw new Exception($"kqueue failed to {flags} with error code {Marshal.GetLastWin32Error()}");
            }

            if (_singleEvent[0].flags.HasFlag(kqueue_flags.ERROR))
            {
                throw new IOException($"kqueue failed to {flags} with error {_singleEvent[0].data}");
            }

            return rc;
        }
    }

    private readonly int _kqueueHndle;

    public KQueuePollGroup()
    {
        _kqueueHndle = BSD.kqueue();

        if (_kqueueHndle == 0)
        {
            throw new Exception("Unable to initialize poll group");
        }
    }

    public void Dispose()
    {
        BSD.close(_kqueueHndle);
        Marshal.FreeHGlobal(_zeroTimeoutPtr);
    }

    public void Add(Socket socket, GCHandle handle)
    {
        var rc = BSD.kevent(
            _kqueueHndle,
            socket.Handle,
            kqueue_filter.READ | kqueue_filter.WRITE,
            kqueue_flags.ADD | kqueue_flags.CLEAR,
            udata: (IntPtr)handle
        );

        if (rc != 0)
        {
            throw new Exception($"kevent failed with error code {Marshal.GetLastWin32Error()}");
        }
    }

    public void Remove(Socket socket, GCHandle handle)
    {
        var rc = BSD.kevent(
            _kqueueHndle,
            socket.Handle,
            kqueue_filter.READ | kqueue_filter.WRITE,
            kqueue_flags.DELETE,
            udata: (IntPtr)handle
        );

        if (rc != 0)
        {
            throw new Exception($"kevent failed with error code {Marshal.GetLastWin32Error()}");
        }
    }

    private kevent[] _events = new kevent[2048];

    public int Poll(int maxEvents)
    {
        if (maxEvents > _events.Length)
        {
            var newLength = Math.Max(maxEvents, _events.Length + (_events.Length >> 2));
            _events = new kevent[newLength];
        }

        return BSD.kevent(_kqueueHndle, null, 0, _events, _events.Length, _zeroTimeoutPtr);
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
            ptrs[i] = _events[i].udata;
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
            handles[i] = (GCHandle)_events[i].udata;
        }

        return rc;
    }
}
