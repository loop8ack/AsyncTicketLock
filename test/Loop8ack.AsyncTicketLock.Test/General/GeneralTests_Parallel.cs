using System.Diagnostics.CodeAnalysis;

using Microsoft.VisualStudio.Threading;

namespace Loop8ack.AsyncTicketLock.Test.General;

[SuppressMessage("Style", "VSTHRD200:Use \"Async\" suffix for async methods")]
public static class GeneralTests_Parallel
{
    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    public static async Task EnterAsync_ShouldBlockUntilRelease_Release(int enterCount)
    {
        var ticket = new object();

        await EnterAsync_ShouldBlockUntilRelease(
            async ticketLock =>
            {
                for (int i = 0; i < enterCount; i++)
                    await ticketLock.EnterAsync(ticket);
            },
            ticketLock =>
            {
                for (int i = 0; i < enterCount; i++)
                    ticketLock.Release(ticket);
            });
    }

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    public static async Task EnterAsync_ShouldBlockUntilRelease_ReleaseCount(int enterCount)
    {
        var ticket = new object();

        await EnterAsync_ShouldBlockUntilRelease(
            async ticketLock =>
            {
                for (int i = 0; i < enterCount; i++)
                    await ticketLock.EnterAsync(ticket);
            },
            ticketLock => ticketLock.Release(ticket, enterCount));
    }

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    public static async Task EnterAsync_ShouldBlockUntilRelease_ReleaseAll(int enterCount)
    {
        var ticket = new object();

        await EnterAsync_ShouldBlockUntilRelease(
            async ticketLock =>
            {
                for (int i = 0; i < enterCount; i++)
                    await ticketLock.EnterAsync(ticket);
            },
            ticketLock => ticketLock.ReleaseAll(ticket));
    }

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    public static async Task EnterAsync_ShouldBlockUntilRelease_ReleaserDisposed(int enterCount)
    {
        var releasers = new List<AsyncTicketLock.Releaser>();
        var ticket = new object();

        await EnterAsync_ShouldBlockUntilRelease(
            async ticketLock =>
            {
                for (int i = 0; i < enterCount; i++)
                    releasers.Add(await ticketLock.EnterAsync(ticket));
            },
            ticketLock =>
            {
                foreach (var releaser in releasers)
                    releaser.Dispose();
            });
    }

    [SuppressMessage("Usage", "VSTHRD003:Avoid awaiting foreign Tasks")]
    private static async Task EnterAsync_ShouldBlockUntilRelease(Func<AsyncTicketLock, Task> enterAsync, Action<AsyncTicketLock> release)
    {
        var ticketLock = new AsyncTicketLock();

        var event1 = new AsyncManualResetEvent(initialState: false);
        var event2 = new AsyncManualResetEvent(initialState: false);
        var event3 = new AsyncManualResetEvent(initialState: false);

        await Task.Run(async () => await enterAsync(ticketLock));

        _ = Task.Run(async () =>
        {
            await event1.WaitAsync();
            release(ticketLock);
            event2.Set();
        });

        var task = Task.Run(async () =>
        {
            async Task enterAsync()
                => await ticketLock.EnterAsync(new object());

            var task = enterAsync();

            await Assert.ThrowsAsync<TimeoutException>(async ()
                => await task.WaitAsync(TimeSpan.FromMilliseconds(1)));

            event1.Set();

            try
            {
                await event2.WaitAsync();
                await task.WaitAsync(TimeSpan.FromMilliseconds(1));
            }
            finally
            {
                event3.Set();
            }
        });

        await event3.WaitAsync();

        await task.WaitAsync(TimeSpan.FromMilliseconds(1));
    }
}
