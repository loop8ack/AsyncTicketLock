using System.Diagnostics.CodeAnalysis;

namespace Loop8ack.AsyncTicketLock.Test;

[SuppressMessage("Usage", "VSTHRD003:Avoid awaiting foreign Tasks")]
[SuppressMessage("CodeQuality", "IDE0079:Remove unnecessary suppression")]
internal static class TaskExtensions
{
#if !NET6_0_OR_GREATER
    public static Task WaitAsync(this Task task, TimeSpan timeout)
        => WaitAsync(task, timeout, default);
    public static Task WaitAsync(this Task task, CancellationToken cancellationToken)
        => WaitAsync(task, Timeout.InfiniteTimeSpan, cancellationToken);
    public static async Task WaitAsync(this Task task, TimeSpan timeout, CancellationToken cancellationToken)
    {
        var cancelTask = Task.Delay(timeout, cancellationToken);
        var completedTask = await Task.WhenAny(task, cancelTask);

        if (completedTask == cancelTask)
        {
            await cancelTask;

            throw new TimeoutException();
        }
    }
#endif
}
