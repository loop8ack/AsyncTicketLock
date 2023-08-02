using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace Loop8ack.AsyncTicketLock;

internal static class ThrowHelper
{
    public static void ThrowIfNull([NotNull] object? argument, [CallerArgumentExpression(nameof(argument))] string? parameterName = null)
    {
        if (argument is null)
            throw new ArgumentNullException(parameterName);
    }

    public static void ThrowIfDisposed<T>(bool isDisposed)
    {
        if (isDisposed)
            throw CreateDisposedException<T>();
    }

    public static ObjectDisposedException CreateDisposedException<T>()
        => new ObjectDisposedException(typeof(T).FullName);
}
