using System.Diagnostics.CodeAnalysis;

namespace Loop8ack.AsyncTicketLock.Test.General;

[SuppressMessage("Usage", "VSTHRD003:Avoid awaiting foreign Tasks")]
[SuppressMessage("Style", "VSTHRD200:Use \"Async\" suffix for async methods")]
public static class GeneralTests_Exceptions
{
    [Fact]
    public static async Task EnterShouldThrowIfDisposed()
    {
        var ticketLock = new AsyncTicketLock();
        var ticket = new object();

        ticketLock.Dispose();

        await Assert.ThrowsAsync<ObjectDisposedException>(async () => await ticketLock.EnterAsync(ticket, Timeout.InfiniteTimeSpan));
        await Assert.ThrowsAsync<ObjectDisposedException>(async () => await ticketLock.EnterAsync(ticket, TimeSpan.Zero));
        await Assert.ThrowsAsync<ObjectDisposedException>(async () => await ticketLock.EnterAsync(ticket, TimeSpan.FromMilliseconds(1)));
        await Assert.ThrowsAsync<ObjectDisposedException>(async () => await ticketLock.EnterAsync(ticket));

        Assert.Throws<ObjectDisposedException>(() => ticketLock.TryEnter(ticket, out _));
        Assert.Throws<ObjectDisposedException>(() => ticketLock.TryEnter(ticket));
    }

    [Fact]
    public static async Task EnterAsyncTaskShouldThrowIfDisposed()
    {
        var ticketLock = new AsyncTicketLock();

        Assert.True(ticketLock.TryEnter(new object()));

        var failTask = ticketLock.EnterAsync(new object()).AsTask();

        await Assert.ThrowsAsync<TimeoutException>(async ()
            => await failTask.WaitAsync(TimeSpan.FromMilliseconds(1)));

        ticketLock.Dispose();

        await Assert.ThrowsAsync<ObjectDisposedException>(async () => await failTask);
    }

    [Fact]
    public static void DisposeShouldNotThrowIfDisposed()
    {
        var ticketLock = new AsyncTicketLock();

        ticketLock.Dispose();
        ticketLock.Dispose();
    }

    [Fact]
    public static void ReleaseShouldThrowIfDisposed()
    {
        var ticketLock = new AsyncTicketLock();
        var ticket = new object();

        Assert.True(ticketLock.TryEnter(ticket));

        ticketLock.Dispose();

        Assert.Throws<ObjectDisposedException>(() => ticketLock.Release(ticket));
        Assert.Throws<ObjectDisposedException>(() => ticketLock.Release(ticket, 1));
        Assert.Throws<ObjectDisposedException>(() => ticketLock.ReleaseAll(ticket));
    }

    [Fact]
    public static void ReleaserDisposeShouldNotThrowIfDisposed()
    {
        var ticketLock = new AsyncTicketLock();
        var ticket = new object();

        Assert.True(ticketLock.TryEnter(ticket, out AsyncTicketLock.Releaser releaser));

        ticketLock.Dispose();

        releaser.Dispose();
    }

    [Fact]
    public static async Task NullTicketParameterShouldThrow()
    {
        var ticketLock = new AsyncTicketLock();

        await Assert.ThrowsAsync<ArgumentNullException>(async () => await ticketLock.EnterAsync(null!, Timeout.InfiniteTimeSpan));
        await Assert.ThrowsAsync<ArgumentNullException>(async () => await ticketLock.EnterAsync(null!, TimeSpan.Zero));
        await Assert.ThrowsAsync<ArgumentNullException>(async () => await ticketLock.EnterAsync(null!, TimeSpan.FromMilliseconds(1)));

        Assert.Throws<ArgumentNullException>(() => ticketLock.TryEnter(null!, out _));
        Assert.Throws<ArgumentNullException>(() => ticketLock.TryEnter(null!));

        Assert.Throws<ArgumentNullException>(() => ticketLock.Release(null!));
        Assert.Throws<ArgumentNullException>(() => ticketLock.Release(null!, 1));
        Assert.Throws<ArgumentNullException>(() => ticketLock.ReleaseAll(null!));
    }

    [Fact]
    public static void ReleaseCountShouldThroIfNegativeCount()
    {
        var ticketLock = new AsyncTicketLock();

        Assert.Equal(0, ticketLock.Release(new object(), 0));

        Assert.Throws<ArgumentOutOfRangeException>(() => ticketLock.Release(new object(), -1));
    }
}
