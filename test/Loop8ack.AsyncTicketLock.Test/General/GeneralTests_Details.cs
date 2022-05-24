namespace Loop8ack.AsyncTicketLock.Test.General;

public static class GeneralTests_Details
{
    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    public static void Release_ShouldReturnWhetherHasReleased(int enterCount)
    {
        var ticketLock = new AsyncTicketLock();
        var ticket = new object();

        for (int i = 0; i < enterCount; i++)
            Assert.True(ticketLock.TryEnter(ticket));

        for (int i = 0; i < enterCount; i++)
            Assert.True(ticketLock.Release(ticket));

        Assert.False(ticketLock.Release(ticket));
    }

    [Theory]
    [InlineData(2)]
    [InlineData(3)]
    public static void ReleaseCount_ShouldReturnReleasedCount(int enterCount)
    {
        var ticketLock = new AsyncTicketLock();
        var ticket = new object();

        for (int i = 0; i < enterCount; i++)
            Assert.True(ticketLock.TryEnter(ticket));

        Assert.Equal(1, ticketLock.Release(ticket, 1));
        Assert.Equal(enterCount - 1, ticketLock.Release(ticket, enterCount));
        Assert.Equal(0, ticketLock.Release(ticket, enterCount));
    }

    [Theory]
    [InlineData(2)]
    [InlineData(3)]
    public static void ReleaseAll_ShouldReturnWhetherHasReleased(int enterCount)
    {
        var ticketLock = new AsyncTicketLock();
        var ticket = new object();

        for (int i = 0; i < enterCount; i++)
            Assert.True(ticketLock.TryEnter(ticket));

        Assert.True(ticketLock.ReleaseAll(ticket));
        Assert.False(ticketLock.ReleaseAll(ticket));
    }

    [Fact]
    public static void IsDisposedShouldBeTrueIfWasDisposed()
    {
        var ticketLock = new AsyncTicketLock();

        Assert.False(ticketLock.IsDisposed);

        ticketLock.Dispose();

        Assert.True(ticketLock.IsDisposed);
    }
}
