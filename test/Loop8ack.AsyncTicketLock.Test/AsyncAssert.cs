using System.Diagnostics.CodeAnalysis;

namespace Loop8ack.AsyncTicketLock.Test;

[SuppressMessage("Usage", "VSTHRD003:Avoid awaiting foreign Tasks")]
internal static class AsyncAssert
{
    public static Task IsBlockingAsync(Func<CancellationToken, ValueTask> executeAsync)
        => CoreIsBlockingAsync<object?>(async c => { await executeAsync(c); return null; });

    public static Task IsBlockingAsync(Func<CancellationToken, Task> executeAsync)
        => CoreIsBlockingAsync<object?>(async c => { await executeAsync(c); return null; });
    
    public static Task IsBlockingAsync<TResult>(Func<CancellationToken, ValueTask<TResult>> executeAsync)
        => CoreIsBlockingAsync(async c => await executeAsync(c));

    public static Task IsBlockingAsync<TResult>(Func<CancellationToken, Task<TResult>> executeAsync)
        => CoreIsBlockingAsync(async c => await executeAsync(c));

    private static async Task CoreIsBlockingAsync<TResult>(Func<CancellationToken, Task<TResult>> executeAsync)
    {
        using var cts = new CancellationTokenSource();

        var enterTask = executeAsync(cts.Token);

        cts.Cancel();

        await Assert
            .ThrowsAsync<OperationCanceledException>(async () =>
            {
                try
                {
                    await enterTask;
                }
                catch (OperationCanceledException ex)
                    when (ex.CancellationToken != cts.Token)
                {
                }
            });
    }
}
