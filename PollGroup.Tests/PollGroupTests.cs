using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;

namespace PollGroup.Tests;

/// <summary>
/// Tests for the IPollGroup interface and PollGroup factory.
/// </summary>
[Collection("PollGroup")]
public class PollGroupTests
{
    private static bool IsWindows => RuntimeInformation.IsOSPlatform(OSPlatform.Windows);

    [Fact]
    public void PollGroupCreate_ReturnsValidInstance()
    {
        using var pollGroup = System.Network.PollGroup.Create();
        Assert.NotNull(pollGroup);
    }

    [Fact]
    public void Add_SucceedsWithValidSocket()
    {
        using var pollGroup = System.Network.PollGroup.Create();
        using var socket = CreateListeningSocket();
        var handle = GCHandle.Alloc(socket, GCHandleType.Normal);

        try
        {
            pollGroup.Add(socket, handle);
            // No exception = success
        }
        finally
        {
            handle.Free();
        }
    }

    [Fact]
    public void Remove_SucceedsWithAddedSocket()
    {
        using var pollGroup = System.Network.PollGroup.Create();
        using var socket = CreateListeningSocket();
        var handle = GCHandle.Alloc(socket, GCHandleType.Normal);

        try
        {
            pollGroup.Add(socket, handle);
            pollGroup.Remove(socket, handle);
            // No exception = success
        }
        finally
        {
            handle.Free();
        }
    }

    [Fact]
    public void IsSocketReady_ReturnsTrueOnLinuxMacOS()
    {
        if (IsWindows)
        {
            return; // Skip on Windows, tested separately
        }

        using var pollGroup = System.Network.PollGroup.Create();
        using var socket = CreateListeningSocket();
        var handle = GCHandle.Alloc(socket, GCHandleType.Normal);

        try
        {
            pollGroup.Add(socket, handle);
            pollGroup.Remove(socket, handle);

            // On Linux/macOS, socket should immediately be ready
            Assert.True(pollGroup.IsSocketReady(socket));
        }
        finally
        {
            handle.Free();
        }
    }

    [SkippableFact]
    public void IsSocketReady_ImmediateWhenNeverPolled()
    {
        Skip.IfNot(IsWindows, "Windows-specific test");

        using var pollGroup = System.Network.PollGroup.Create();
        using var socket = CreateListeningSocket();
        var handle = GCHandle.Alloc(socket, GCHandleType.Normal);

        try
        {
            // Add socket (no poll submitted yet)
            pollGroup.Add(socket, handle);

            // Remove socket (should be in IDLE state, so immediately freed)
            pollGroup.Remove(socket, handle);

            // Socket was never polled, should be immediately ready
            Assert.True(pollGroup.IsSocketReady(socket));
        }
        finally
        {
            handle.Free();
        }
    }

    [SkippableFact]
    public void IsSocketReady_EventuallyReadyAfterPolledAndRemoved()
    {
        Skip.IfNot(IsWindows, "Windows-specific test");

        using var pollGroup = System.Network.PollGroup.Create();
        using var socket = CreateListeningSocket();
        var handle = GCHandle.Alloc(socket, GCHandleType.Normal);

        try
        {
            // Add socket
            pollGroup.Add(socket, handle);

            // Poll to submit the poll request
            var handles = new GCHandle[10];
            pollGroup.Poll(handles);

            // Remove socket
            pollGroup.Remove(socket, handle);

            // Poll to process the cancellation completion
            var ready = false;
            for (var i = 0; i < 100 && !ready; i++)
            {
                pollGroup.Poll(handles);
                ready = pollGroup.IsSocketReady(socket);
                if (!ready)
                {
                    Thread.Sleep(10);
                }
            }

            Assert.True(ready, "Socket should eventually be ready after removal and polling");
        }
        finally
        {
            handle.Free();
        }
    }

    [SkippableFact]
    public void SocketMigration_WorksCorrectlyOnWindows()
    {
        Skip.IfNot(IsWindows, "Windows-specific test");

        using var pollGroup1 = System.Network.PollGroup.Create();
        using var pollGroup2 = System.Network.PollGroup.Create();
        using var socket = CreateListeningSocket();
        var handle = GCHandle.Alloc(socket, GCHandleType.Normal);

        try
        {
            // Add to first poll group
            pollGroup1.Add(socket, handle);

            // Poll to submit the poll request
            var handles = new GCHandle[10];
            pollGroup1.Poll(handles);

            // Remove from first poll group
            pollGroup1.Remove(socket, handle);

            // Wait for socket to be ready for migration
            var ready = false;
            for (var i = 0; i < 100 && !ready; i++)
            {
                pollGroup1.Poll(handles);
                ready = pollGroup1.IsSocketReady(socket);
                if (!ready)
                {
                    Thread.Sleep(10);
                }
            }

            Assert.True(ready, "Socket should be ready for migration");

            // Add to second poll group
            pollGroup2.Add(socket, handle);

            // Verify socket is working in second poll group
            Assert.False(pollGroup2.IsSocketReady(socket));
        }
        finally
        {
            handle.Free();
        }
    }

    [Fact]
    public void Poll_ReturnsZeroWhenNoEvents()
    {
        using var pollGroup = System.Network.PollGroup.Create();
        using var socket = CreateListeningSocket();
        var handle = GCHandle.Alloc(socket, GCHandleType.Normal);

        try
        {
            pollGroup.Add(socket, handle);

            // No incoming connections, should return 0 events
            var handles = new GCHandle[10];
            var count = pollGroup.Poll(handles);

            Assert.Equal(0, count);
        }
        finally
        {
            handle.Free();
        }
    }

    private static Socket CreateListeningSocket()
    {
        var socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        socket.Bind(new IPEndPoint(IPAddress.Loopback, 0));
        socket.Listen(10);
        return socket;
    }
}
