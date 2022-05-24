using System.Diagnostics.CodeAnalysis;

namespace Loop8ack.AsyncTicketLock.Test.General;

[SuppressMessage("Style", "VSTHRD200:Use \"Async\" suffix for async methods")]
public static class GeneralTests_Async_TryEnter
{
    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    public static async Task TryEnter_Success_NoOtherEntered(int enterCount)
    {
        var ticketLock = new AsyncTicketLock();
        var ticket = new object();

        for (int i = 0; i < enterCount; i++)
            await Task.Run(() => Assert.True(ticketLock.TryEnter(ticket)));
    }

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    public static async Task TryEnter_Success_OtherEntered_Released_Release(int enterCount)
    {
        var ticketLock = new AsyncTicketLock();
        var ticket = new object();

        for (int i = 0; i < enterCount; i++)
            await Task.Run(() => Assert.True(ticketLock.TryEnter(ticket)));

        for (int i = 0; i < enterCount; i++)
            await Task.Run(() => Assert.True(ticketLock.Release(ticket)));

        await Task.Run(() => Assert.True(ticketLock.TryEnter(new object())));
    }

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    public static async Task TryEnter_Success_OtherEntered_Released_ReleaseCount(int enterCount)
    {
        var ticketLock = new AsyncTicketLock();
        var ticket = new object();

        for (int i = 0; i < enterCount; i++)
            await Task.Run(() => Assert.True(ticketLock.TryEnter(ticket)));

        await Task.Run(() => Assert.Equal(enterCount, ticketLock.Release(ticket, enterCount)));

        await Task.Run(() => Assert.True(ticketLock.TryEnter(new object())));
    }

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    public static async Task TryEnter_Success_OtherEntered_Released_ReleaseAll(int enterCount)
    {
        var ticketLock = new AsyncTicketLock();
        var ticket = new object();

        for (int i = 0; i < enterCount; i++)
            await Task.Run(() => Assert.True(ticketLock.TryEnter(ticket)));

        await Task.Run(() => Assert.True(ticketLock.ReleaseAll(ticket)));

        await Task.Run(() => Assert.True(ticketLock.TryEnter(new object())));
    }

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    public static async Task TryEnter_Success_OtherEntered_Released_ReleaserDisposed(int enterCount)
    {
        var ticketLock = new AsyncTicketLock();
        var ticket = new object();

        for (int i = 0; i < enterCount; i++)
        {
            var releaser = await Task.Run(() =>
            {
                Assert.True(ticketLock.TryEnter(ticket, out AsyncTicketLock.Releaser releaser));

                return releaser;
            });

            await Task.Run(releaser.Dispose);
        }

        await Task.Run(() => Assert.True(ticketLock.TryEnter(new object())));
    }

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    public static async Task TryEnter_Failure_OtherEntered_NotReleased(int enterCount)
    {
        var ticketLock = new AsyncTicketLock();
        var ticket = new object();

        for (int i = 0; i < enterCount; i++)
            await Task.Run(() => Assert.True(ticketLock.TryEnter(ticket)));

        await Task.Run(() => Assert.False(ticketLock.TryEnter(new object())));
    }

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    public static async Task TryEnter_Failure_OtherEntered_NotEnoughReleased_Release(int enterCount)
    {
        var ticketLock = new AsyncTicketLock();
        var ticket = new object();

        for (int i = 0; i < enterCount; i++)
            await Task.Run(() => Assert.True(ticketLock.TryEnter(ticket)));

        for (int i = 0; i < enterCount - 1; i++)
            await Task.Run(() => Assert.True(ticketLock.Release(ticket)));

        await Task.Run(() => Assert.False(ticketLock.TryEnter(new object())));
    }

    [Theory]
    [InlineData(2)]
    [InlineData(3)]
    public static async Task TryEnter_Failure_OtherEntered_NotEnoughReleased_ReleaseCount(int enterCount)
    {
        var ticketLock = new AsyncTicketLock();
        var ticket = new object();

        for (int i = 0; i < enterCount; i++)
            await Task.Run(() => Assert.True(ticketLock.TryEnter(ticket)));

        enterCount--;

        await Task.Run(() => Assert.Equal(enterCount, ticketLock.Release(ticket, enterCount)));

        await Task.Run(() => Assert.False(ticketLock.TryEnter(new object())));
    }
}
