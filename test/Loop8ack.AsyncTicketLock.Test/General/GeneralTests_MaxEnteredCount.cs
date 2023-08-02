namespace Loop8ack.AsyncTicketLock.Test;

public static class GeneralTests_MaxEnteredCount
{
    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    public static void MaxEnteredCount_MaxReached_ShouldNotEnter(int maxEnteredCount)
    {
        var ticketLock = new AsyncTicketLock(maxEnteredCount);

        var ticket = new object();

        for (int i = 0; i < maxEnteredCount; i++)
            Assert.True(ticketLock.TryEnter(ticket));

        Assert.False(ticketLock.TryEnter(ticket));
    }

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    public static void MaxEnteredCount_MaxReached_ShouldEnterAfterReleasing(int maxEnteredCount)
    {
        var ticketLock = new AsyncTicketLock(maxEnteredCount);

        var ticket = new object();

        for (int i = 0; i < maxEnteredCount; i++)
            Assert.True(ticketLock.TryEnter(ticket));

        ticketLock.Release(ticket);

        // Try to acquire the ticket after releasing it
        Assert.True(ticketLock.TryEnter(ticket));
    }
}
