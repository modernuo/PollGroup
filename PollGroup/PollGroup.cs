using System;
using System.Net.Sockets;
using System.Runtime.InteropServices;

namespace System.Network
{
    public interface IPollGroup : IDisposable
    {
        void Add(Socket sock, GCHandle handle);
        void Remove(Socket sock);
        int Poll(ref GCHandle[] handles);
    }

    public static class PollGroup
    {
        public static IPollGroup Create()
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.FreeBSD) || RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                return new KQueuePollGroup();
            }

            return new EPollGroup();
        }
    }
}
