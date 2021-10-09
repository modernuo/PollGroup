/*************************************************************************
 * ModernUO                                                              *
 * Copyright 2019-2021 - ModernUO Development Team                       *
 * Email: hi@modernuo.com                                                *
 * File: PollGroup.cs                                                    *
 *                                                                       *
 * This program is free software: you can redistribute it and/or modify  *
 * it under the terms of the GNU General Public License as published by  *
 * the Free Software Foundation, either version 3 of the License, or     *
 * (at your option) any later version.                                   *
 *                                                                       *
 * You should have received a copy of the GNU General Public License     *
 * along with this program.  If not, see <http://www.gnu.org/licenses/>. *
 *************************************************************************/

using System;
using System.Net.Sockets;
using System.Runtime.InteropServices;

namespace Server.Network
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
