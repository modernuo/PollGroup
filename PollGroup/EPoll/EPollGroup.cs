using System.Net.Sockets;
using System.Runtime.InteropServices;

namespace System.Network.EPoll
{
    internal sealed class EPollGroup<T> : IPollGroup where T : IArch
    {
        private readonly int _epHndle;
        private epoll_event[] _events;

        public EPollGroup()
        {
            _events = new epoll_event[2048];
            _epHndle = T.epoll_create1(epoll_flags.NONE);

            if (_epHndle == 0)
            {
                throw new Exception("Unable to initialize poll group");
            }
        }

        public void Add(Socket socket, GCHandle handle)
        {
            var ev = new epoll_event
            {
                events = epoll_events.EPOLLIN | epoll_events.EPOLLERR
            };

            ev.data.ptr = (nint)handle;

            T.epoll_ctl(_epHndle, epoll_op.EPOLL_CTL_ADD, (int)socket.Handle, ref ev);
        }

        public void Dispose()
        {
            T.epoll_close(_epHndle);
        }

        public int Poll(int maxEvents)
        {
            if (maxEvents > _events.Length)
            {
                var newLength = Math.Max(maxEvents, _events.Length + (_events.Length >> 2));
                _events = new epoll_event[newLength];
            }

            return T.epoll_wait(_epHndle, _events, maxEvents, 0);
        }

        public int Poll(nint[] ptrs)
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

        public void Remove(Socket socket, GCHandle handle)
        {
            var ev = new epoll_event
            {
                events = epoll_events.EPOLLIN | epoll_events.EPOLLERR
            };

            ev.data.ptr = (nint)handle;

            int rc = T.epoll_ctl(_epHndle, epoll_op.EPOLL_CTL_DEL, (int)socket.Handle, ref ev);

            if (rc != 0)
            {
                throw new Exception($"epoll_ctl failed with error code {Marshal.GetLastWin32Error()}");
            }
        }
    }
}
