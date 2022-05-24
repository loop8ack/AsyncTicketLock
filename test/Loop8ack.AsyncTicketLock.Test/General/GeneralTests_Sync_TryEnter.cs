namespace Loop8ack.AsyncTicketLock.Test.General;

public static class GeneralTests_Sync_TryEnter
{
    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    public static void TryEnter_Success_NoOtherEntered(int enterCount)
    {
        var ticketLock = new AsyncTicketLock();
        var ticket = new object();

        for (int i = 0; i < enterCount; i++)
            Assert.True(ticketLock.TryEnter(ticket));
    }

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    public static void TryEnter_Success_OtherEntered_Released_Release(int enterCount)
    {
        var ticketLock = new AsyncTicketLock();
        var ticket = new object();

        for (int i = 0; i < enterCount; i++)
            Assert.True(ticketLock.TryEnter(ticket));

        for (int i = 0; i < enterCount; i++)
            Assert.True(ticketLock.Release(ticket));

        Assert.True(ticketLock.TryEnter(new object()));
    }

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    public static void TryEnter_Success_OtherEntered_Released_ReleaseCount(int enterCount)
    {
        var ticketLock = new AsyncTicketLock();
        var ticket = new object();

        for (int i = 0; i < enterCount; i++)
            Assert.True(ticketLock.TryEnter(ticket));

        Assert.Equal(enterCount, ticketLock.Release(ticket, enterCount));

        Assert.True(ticketLock.TryEnter(new object()));
    }

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    public static void TryEnter_Success_OtherEntered_Released_ReleaseAll(int enterCount)
    {
        var ticketLock = new AsyncTicketLock();
        var ticket = new object();

        for (int i = 0; i < enterCount; i++)
            Assert.True(ticketLock.TryEnter(ticket));

        Assert.True(ticketLock.ReleaseAll(ticket));

        Assert.True(ticketLock.TryEnter(new object()));
    }

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    public static void TryEnter_Success_OtherEntered_Released_ReleaserDisposed(int enterCount)
    {
        var ticketLock = new AsyncTicketLock();
        var ticket = new object();

        for (int i = 0; i < enterCount; i++)
        {
            Assert.True(ticketLock.TryEnter(ticket, out AsyncTicketLock.Releaser releaser));

            releaser.Dispose();
        }

        Assert.True(ticketLock.TryEnter(new object()));
    }

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    public static void TryEnter_Failure_OtherEntered_NotReleased(int enterCount)
    {
        var ticketLock = new AsyncTicketLock();
        var ticket = new object();

        for (int i = 0; i < enterCount; i++)
            Assert.True(ticketLock.TryEnter(ticket));

        Assert.False(ticketLock.TryEnter(new object()));
    }

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    public static void TryEnter_Failure_OtherEntered_NotEnoughReleased_Release(int enterCount)
    {
        var ticketLock = new AsyncTicketLock();
        var ticket = new object();

        for (int i = 0; i < enterCount; i++)
            Assert.True(ticketLock.TryEnter(ticket));

        for (int i = 0; i < enterCount - 1; i++)
            Assert.True(ticketLock.Release(ticket));

        Assert.False(ticketLock.TryEnter(new object()));
    }

    [Theory]
    [InlineData(2)]
    [InlineData(3)]
    public static void TryEnter_Failure_OtherEntered_NotEnoughReleased_ReleaseCount(int enterCount)
    {
        var ticketLock = new AsyncTicketLock();
        var ticket = new object();

        for (int i = 0; i < enterCount; i++)
            Assert.True(ticketLock.TryEnter(ticket));

        enterCount--;

        Assert.Equal(enterCount, ticketLock.Release(ticket, enterCount));

        Assert.False(ticketLock.TryEnter(new object()));
    }
}
