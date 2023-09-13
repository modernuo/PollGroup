using System.Network.EPoll;
using System.Network.EPoll.Architectures;
using System.Network.KQueuePoll;
using System.Runtime.InteropServices;

namespace System.Network;

public static class PollGroup
{
    public static IPollGroup Create()
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.FreeBSD) || RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            return new KQueuePollGroup();
        }

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            return new PackedEPollGroup<Win_x64>();
        }

        if (RuntimeInformation.ProcessArchitecture is Architecture.Arm or Architecture.Arm64 or Architecture.Armv6)
        {
            return new EPollGroup<Linux_arm64>();
        }

        return new PackedEPollGroup<Linux_x64>();
    }
}
