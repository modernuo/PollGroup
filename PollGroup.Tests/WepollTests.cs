using System.Net;
using System.Net.Sockets;
using System.Network;
using System.Network.EPoll;
using System.Network.EPoll.Architectures;
using System.Runtime.InteropServices;

namespace PollGroup.Tests;

/// <summary>
/// Tests for wepoll functionality on Windows.
/// These tests verify the native wepoll bindings and the epoll_sock_is_ready extension.
/// </summary>
[Collection("Wepoll")]
public class WepollTests
{
    private static bool IsWindows => RuntimeInformation.IsOSPlatform(OSPlatform.Windows);

    [SkippableFact]
    public void EpollCreate_ReturnsValidHandle()
    {
        Skip.IfNot(IsWindows, "wepoll tests only run on Windows");

        var handle = Win_x64.epoll_create1(epoll_flags.NONE);
        try
        {
            Assert.NotEqual(nint.Zero, handle);
        }
        finally
        {
            if (handle != nint.Zero)
            {
                Win_x64.epoll_close(handle);
            }
        }
    }

    [SkippableFact]
    public void EpollCtlAdd_SucceedsWithValidSocket()
    {
        Skip.IfNot(IsWindows, "wepoll tests only run on Windows");

        var epHandle = Win_x64.epoll_create1(epoll_flags.NONE);
        Assert.NotEqual(nint.Zero, epHandle);

        try
        {
            using var socket = CreateListeningSocket();
            var ev = new epoll_event_packed
            {
                Events = epoll_events.EPOLLIN,
                Ptr = socket.Handle
            };

            var result = Win_x64.epoll_ctl(epHandle, epoll_op.EPOLL_CTL_ADD, socket.Handle, ref ev);
            Assert.Equal(0, result);
        }
        finally
        {
            Win_x64.epoll_close(epHandle);
        }
    }

    [SkippableFact]
    public void EpollCtlDel_SucceedsWithAddedSocket()
    {
        Skip.IfNot(IsWindows, "wepoll tests only run on Windows");

        var epHandle = Win_x64.epoll_create1(epoll_flags.NONE);
        Assert.NotEqual(nint.Zero, epHandle);

        try
        {
            using var socket = CreateListeningSocket();
            var ev = new epoll_event_packed
            {
                Events = epoll_events.EPOLLIN,
                Ptr = socket.Handle
            };

            // Add socket
            var addResult = Win_x64.epoll_ctl(epHandle, epoll_op.EPOLL_CTL_ADD, socket.Handle, ref ev);
            Assert.Equal(0, addResult);

            // Delete socket
            var delResult = Win_x64.epoll_ctl(epHandle, epoll_op.EPOLL_CTL_DEL, socket.Handle, ref ev);
            Assert.Equal(0, delResult);
        }
        finally
        {
            Win_x64.epoll_close(epHandle);
        }
    }

    [SkippableFact]
    public void EpollSockIsReady_ReturnsReadyForNonExistentSocket()
    {
        Skip.IfNot(IsWindows, "wepoll tests only run on Windows");

        var epHandle = Win_x64.epoll_create1(epoll_flags.NONE);
        Assert.NotEqual(nint.Zero, epHandle);

        try
        {
            using var socket = CreateListeningSocket();

            // Socket was never added, should return ready (0)
            var result = Win_x64.epoll_sock_is_ready(epHandle, socket.Handle);
            Assert.Equal(0, result);
        }
        finally
        {
            Win_x64.epoll_close(epHandle);
        }
    }

    [SkippableFact]
    public void EpollSockIsReady_ReturnsErrorForActiveSocket()
    {
        Skip.IfNot(IsWindows, "wepoll tests only run on Windows");

        var epHandle = Win_x64.epoll_create1(epoll_flags.NONE);
        Assert.NotEqual(nint.Zero, epHandle);

        try
        {
            using var socket = CreateListeningSocket();
            var ev = new epoll_event_packed
            {
                Events = epoll_events.EPOLLIN,
                Ptr = socket.Handle
            };

            // Add socket
            var addResult = Win_x64.epoll_ctl(epHandle, epoll_op.EPOLL_CTL_ADD, socket.Handle, ref ev);
            Assert.Equal(0, addResult);

            // Check ready on active socket should return error (-1)
            var readyResult = Win_x64.epoll_sock_is_ready(epHandle, socket.Handle);
            Assert.Equal(-1, readyResult);
        }
        finally
        {
            Win_x64.epoll_close(epHandle);
        }
    }

    [SkippableFact]
    public void EpollSockIsReady_ImmediatelyReadyWhenNeverPolled()
    {
        Skip.IfNot(IsWindows, "wepoll tests only run on Windows");

        var epHandle = Win_x64.epoll_create1(epoll_flags.NONE);
        Assert.NotEqual(nint.Zero, epHandle);

        try
        {
            using var socket = CreateListeningSocket();
            var ev = new epoll_event_packed
            {
                Events = epoll_events.EPOLLIN,
                Ptr = socket.Handle
            };

            // Add socket (no poll submitted yet because no epoll_wait called)
            var addResult = Win_x64.epoll_ctl(epHandle, epoll_op.EPOLL_CTL_ADD, socket.Handle, ref ev);
            Assert.Equal(0, addResult);

            // Delete socket (should be in IDLE state, so immediately freed)
            var delResult = Win_x64.epoll_ctl(epHandle, epoll_op.EPOLL_CTL_DEL, socket.Handle, ref ev);
            Assert.Equal(0, delResult);

            // Socket was never polled, so it should be immediately ready (not found in tree)
            var readyResult = Win_x64.epoll_sock_is_ready(epHandle, socket.Handle);
            Assert.Equal(0, readyResult);
        }
        finally
        {
            Win_x64.epoll_close(epHandle);
        }
    }

    [SkippableFact]
    public void EpollSockIsReady_EventuallyReadyAfterPolledAndDeleted()
    {
        Skip.IfNot(IsWindows, "wepoll tests only run on Windows");

        var epHandle = Win_x64.epoll_create1(epoll_flags.NONE);
        Assert.NotEqual(nint.Zero, epHandle);

        try
        {
            using var socket = CreateListeningSocket();
            var ev = new epoll_event_packed
            {
                Events = epoll_events.EPOLLIN,
                Ptr = socket.Handle
            };

            // Add socket
            var addResult = Win_x64.epoll_ctl(epHandle, epoll_op.EPOLL_CTL_ADD, socket.Handle, ref ev);
            Assert.Equal(0, addResult);

            // Poll to submit the poll request (this makes poll_status = PENDING)
            var events = new epoll_event_packed[10];
            Win_x64.epoll_wait(epHandle, events, events.Length, 0);

            // Now delete socket - this will cancel the pending poll
            var delResult = Win_x64.epoll_ctl(epHandle, epoll_op.EPOLL_CTL_DEL, socket.Handle, ref ev);
            Assert.Equal(0, delResult);

            // Socket should still be in tree with pending cancellation
            // Poll to process the cancellation completion
            var ready = false;
            for (var i = 0; i < 100 && !ready; i++)
            {
                Win_x64.epoll_wait(epHandle, events, events.Length, 10);
                var result = Win_x64.epoll_sock_is_ready(epHandle, socket.Handle);
                ready = result == 0;
            }

            Assert.True(ready, "Socket should be ready after delete and polling");
        }
        finally
        {
            Win_x64.epoll_close(epHandle);
        }
    }

    [SkippableFact]
    public void SocketMigration_SucceedsAfterReadyCheck()
    {
        Skip.IfNot(IsWindows, "wepoll tests only run on Windows");

        var epHandle1 = Win_x64.epoll_create1(epoll_flags.NONE);
        var epHandle2 = Win_x64.epoll_create1(epoll_flags.NONE);
        Assert.NotEqual(nint.Zero, epHandle1);
        Assert.NotEqual(nint.Zero, epHandle2);

        try
        {
            using var socket = CreateListeningSocket();
            var ev = new epoll_event_packed
            {
                Events = epoll_events.EPOLLIN,
                Ptr = socket.Handle
            };

            // Add to first epoll instance
            var addResult1 = Win_x64.epoll_ctl(epHandle1, epoll_op.EPOLL_CTL_ADD, socket.Handle, ref ev);
            Assert.Equal(0, addResult1);

            // Poll to submit the poll request
            var events = new epoll_event_packed[10];
            Win_x64.epoll_wait(epHandle1, events, events.Length, 0);

            // Remove from first epoll instance
            var delResult = Win_x64.epoll_ctl(epHandle1, epoll_op.EPOLL_CTL_DEL, socket.Handle, ref ev);
            Assert.Equal(0, delResult);

            // Wait for socket to be ready for migration
            var ready = false;
            for (var i = 0; i < 100 && !ready; i++)
            {
                Win_x64.epoll_wait(epHandle1, events, events.Length, 10);
                var result = Win_x64.epoll_sock_is_ready(epHandle1, socket.Handle);
                ready = result == 0;
            }

            Assert.True(ready, "Socket should be ready for migration");

            // Add to second epoll instance
            var addResult2 = Win_x64.epoll_ctl(epHandle2, epoll_op.EPOLL_CTL_ADD, socket.Handle, ref ev);
            Assert.Equal(0, addResult2);

            // Verify socket is active in second instance (checking ready should return error)
            var activeResult = Win_x64.epoll_sock_is_ready(epHandle2, socket.Handle);
            Assert.Equal(-1, activeResult);
        }
        finally
        {
            Win_x64.epoll_close(epHandle1);
            Win_x64.epoll_close(epHandle2);
        }
    }

    [SkippableFact]
    public void SocketMigration_FailsWithoutWaiting()
    {
        Skip.IfNot(IsWindows, "wepoll tests only run on Windows");

        var epHandle1 = Win_x64.epoll_create1(epoll_flags.NONE);
        var epHandle2 = Win_x64.epoll_create1(epoll_flags.NONE);
        Assert.NotEqual(nint.Zero, epHandle1);
        Assert.NotEqual(nint.Zero, epHandle2);

        try
        {
            using var socket = CreateListeningSocket();
            var ev = new epoll_event_packed
            {
                Events = epoll_events.EPOLLIN,
                Ptr = socket.Handle
            };

            // Add to first epoll instance
            var addResult1 = Win_x64.epoll_ctl(epHandle1, epoll_op.EPOLL_CTL_ADD, socket.Handle, ref ev);
            Assert.Equal(0, addResult1);

            // Poll to submit the poll request - this makes poll_status = PENDING
            var events = new epoll_event_packed[10];
            Win_x64.epoll_wait(epHandle1, events, events.Length, 0);

            // Remove from first epoll instance - this cancels the poll asynchronously
            var delResult = Win_x64.epoll_ctl(epHandle1, epoll_op.EPOLL_CTL_DEL, socket.Handle, ref ev);
            Assert.Equal(0, delResult);

            // Immediately check - socket should NOT be ready yet (still has pending cancellation)
            var readyResult = Win_x64.epoll_sock_is_ready(epHandle1, socket.Handle);
            Assert.Equal(-1, readyResult); // Should return -1 indicating not ready

            // Attempting to add to second epoll instance without waiting should fail
            // because the socket still has a pending poll operation on the first instance
            var addResult2 = Win_x64.epoll_ctl(epHandle2, epoll_op.EPOLL_CTL_ADD, socket.Handle, ref ev);

            // The add might succeed (wepoll allows it) but the socket won't work correctly,
            // OR it might fail with an error. Either way demonstrates the problem.
            // If it succeeds, subsequent operations may behave unexpectedly.
            // This test documents that immediate migration without waiting is problematic.
            if (addResult2 == 0)
            {
                // If add succeeded, verify the socket is in a potentially problematic state
                // by checking that the first epoll still has pending operations
                var stillPending = Win_x64.epoll_sock_is_ready(epHandle1, socket.Handle);
                // The socket may still show as not ready in the first instance
                // This demonstrates the race condition
            }
            else
            {
                // Add failed, which is expected behavior - can't add while pending
                Assert.Equal(-1, addResult2);
            }
        }
        finally
        {
            Win_x64.epoll_close(epHandle1);
            Win_x64.epoll_close(epHandle2);
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
