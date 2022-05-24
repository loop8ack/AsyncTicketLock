using System.Diagnostics.CodeAnalysis;

namespace Loop8ack.AsyncTicketLock.Test.General;

[SuppressMessage("Style", "VSTHRD200:Use \"Async\" suffix for async methods")]
public static class GeneralTests_Async_EnterAsync
{
    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    public static async Task EnterAsync_Success_NoOtherEntered(int enterCount)
    {
        var ticketLock = new AsyncTicketLock();
        var ticket = new object();

        for (int i = 0; i < enterCount; i++)
            await Task.Run(async () => await ticketLock.EnterAsync(ticket, TimeSpan.Zero));
    }

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    public static async Task EnterAsync_Success_OtherEntered_Released_Release(int enterCount)
    {
        var ticketLock = new AsyncTicketLock();
        var ticket = new object();

        for (int i = 0; i < enterCount; i++)
            await Task.Run(async () => await ticketLock.EnterAsync(ticket, TimeSpan.Zero));

        for (int i = 0; i < enterCount; i++)
            await Task.Run(() => Assert.True(ticketLock.Release(ticket)));

        await Task.Run(async () => await ticketLock.EnterAsync(new object(), TimeSpan.Zero));
    }

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    public static async Task EnterAsync_Success_OtherEntered_Released_ReleaseCount(int enterCount)
    {
        var ticketLock = new AsyncTicketLock();
        var ticket = new object();

        for (int i = 0; i < enterCount; i++)
            await Task.Run(async () => await ticketLock.EnterAsync(ticket, TimeSpan.Zero));

        await Task.Run(() => Assert.Equal(enterCount, ticketLock.Release(ticket, enterCount)));

        await Task.Run(async () => await ticketLock.EnterAsync(new object(), TimeSpan.Zero));
    }

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    public static async Task EnterAsync_Success_OtherEntered_Released_ReleaseAll(int enterCount)
    {
        var ticketLock = new AsyncTicketLock();
        var ticket = new object();

        for (int i = 0; i < enterCount; i++)
            await Task.Run(async () => await ticketLock.EnterAsync(ticket, TimeSpan.Zero));

        await Task.Run(() => Assert.True(ticketLock.ReleaseAll(ticket)));

        await Task.Run(async () => await ticketLock.EnterAsync(new object(), TimeSpan.Zero));
    }

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    public static async Task EnterAsync_Success_OtherEntered_Released_ReleaserDisposed(int enterCount)
    {
        var ticketLock = new AsyncTicketLock();
        var ticket = new object();

        for (int i = 0; i < enterCount; i++)
        {
            var releaser = await Task.Run(async () => await ticketLock.EnterAsync(ticket, TimeSpan.Zero));

            await Task.Run(releaser.Dispose);
        }

        await Task.Run(async () => await ticketLock.EnterAsync(new object(), TimeSpan.Zero));
    }

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    public static async Task EnterAsync_Blocking_OtherEntered_NotReleased(int enterCount)
    {
        var ticketLock = new AsyncTicketLock();
        var ticket = new object();

        for (int i = 0; i < enterCount; i++)
            await Task.Run(() => Assert.True(ticketLock.TryEnter(ticket)));

        await AsyncAssert.IsBlockingAsync(c => ticketLock.EnterAsync(new object(), c));
    }

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    public static async Task EnterAsync_Blocking_OtherEntered_NotEnoughReleased_Release(int enterCount)
    {
        var ticketLock = new AsyncTicketLock();
        var ticket = new object();

        for (int i = 0; i < enterCount; i++)
            await Task.Run(() => Assert.True(ticketLock.TryEnter(ticket)));

        for (int i = 0; i < enterCount - 1; i++)
            await Task.Run(() => Assert.True(ticketLock.Release(ticket)));

        await AsyncAssert.IsBlockingAsync(c => ticketLock.EnterAsync(new object(), c));
    }

    [Theory]
    [InlineData(2)]
    [InlineData(3)]
    public static async Task EnterAsync_Blocking_OtherEntered_NotEnoughReleased_ReleaseCount(int enterCount)
    {
        var ticketLock = new AsyncTicketLock();
        var ticket = new object();

        for (int i = 0; i < enterCount; i++)
            await Task.Run(() => Assert.True(ticketLock.TryEnter(ticket)));

        enterCount--;

        await Task.Run(() => Assert.Equal(enterCount, ticketLock.Release(ticket, enterCount)));

        await AsyncAssert.IsBlockingAsync(c => ticketLock.EnterAsync(new object(), c));
    }

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    public static async Task EnterAsync_Failure_OtherEntered_NotReleased(int enterCount)
    {
        var ticketLock = new AsyncTicketLock();
        var ticket = new object();

        for (int i = 0; i < enterCount; i++)
            await Task.Run(() => Assert.True(ticketLock.TryEnter(ticket)));

        await Assert.ThrowsAsync<TimeoutException>(
            () => Task.Run(async () => await ticketLock.EnterAsync(new object(), TimeSpan.Zero)));
    }

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    public static async Task EnterAsync_Failure_OtherEntered_NotEnoughReleased_Release(int enterCount)
    {
        var ticketLock = new AsyncTicketLock();
        var ticket = new object();

        for (int i = 0; i < enterCount; i++)
            await Task.Run(() => Assert.True(ticketLock.TryEnter(ticket)));

        for (int i = 0; i < enterCount - 1; i++)
            await Task.Run(() => Assert.True(ticketLock.Release(ticket)));

        await Assert.ThrowsAsync<TimeoutException>(
            () => Task.Run(async () => await ticketLock.EnterAsync(new object(), TimeSpan.Zero)));
    }

    [Theory]
    [InlineData(2)]
    [InlineData(3)]
    public static async Task EnterAsync_Failure_OtherEntered_NotEnoughReleased_ReleaseCount(int enterCount)
    {
        var ticketLock = new AsyncTicketLock();
        var ticket = new object();

        for (int i = 0; i < enterCount; i++)
            await Task.Run(() => Assert.True(ticketLock.TryEnter(ticket)));

        enterCount--;

        await Task.Run(() => Assert.Equal(enterCount, ticketLock.Release(ticket, enterCount)));

        await Assert.ThrowsAsync<TimeoutException>(
            () => Task.Run(async () => await ticketLock.EnterAsync(new object(), TimeSpan.Zero)));
    }

    [Fact]
    public static async Task EnterAsync_Timeout_ShouldNotEntered()
    {
        var ticketLock = new AsyncTicketLock();
        var ticket = new object();

        Assert.True(ticketLock.TryEnter(ticket));

        await Assert.ThrowsAsync<TimeoutException>(async ()
            => await ticketLock.EnterAsync(new object(), TimeSpan.FromMilliseconds(1)));

        Assert.True(ticketLock.Release(ticket));

        Assert.True(ticketLock.TryEnter(new object()));
    }

    [Fact]
    public static async Task EnterAsync_Canceled_ShouldNotEntered()
    {
        var ticketLock = new AsyncTicketLock();
        var ticket = new object();

        Assert.True(ticketLock.TryEnter(ticket));

        using (var cts = new CancellationTokenSource())
        {
            var failTask = ticketLock.EnterAsync(new object(), cts.Token);

            cts.CancelAfter(1);

            await Assert.ThrowsAsync<OperationCanceledException>(async () => await failTask);

            Assert.True(ticketLock.Release(ticket));
        }

        Assert.True(ticketLock.TryEnter(new object()));
    }

    [Fact]
    public static async Task EnterAsync_Canceled_ShouldThrow()
    {
        var ticketLock = new AsyncTicketLock();

        Assert.True(ticketLock.TryEnter(new object()));

        using (var cts = new CancellationTokenSource())
        {
            cts.CancelAfter(1);

            await Assert.ThrowsAsync<OperationCanceledException>(async ()
                => await ticketLock.EnterAsync(new object(), cts.Token));
        }
    }
}
