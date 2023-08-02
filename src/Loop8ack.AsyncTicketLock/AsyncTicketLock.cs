using System.Diagnostics;
using System.Threading.Channels;

namespace Loop8ack.AsyncTicketLock;

/// <summary>
///     An asynchronous class that functions as a lock based on provided ticket objects instead of the current thread.
///     Only one ticket object can hold the lock at a time, accesses with the same ticket object are allowed, accesses with other ticket objects are not allowed.
/// </summary>
/// <remarks>
///     This object does <strong>not</strong> need to be disposed of, as it does not hold unmanaged resources.
///     Disposing this object has no effect on current users of the semaphore, and they are allowed to release their hold on the semaphore without exception.
///     An <see cref="ObjectDisposedException"/> is thrown back at anyone asking to or waiting to enter the <see cref="AsyncTicketLock"/> after <see cref="Dispose"/> is called.
/// </remarks>
public sealed class AsyncTicketLock : IDisposable
{
    private readonly Channel<State> _channel;
    private readonly State _state;

    /// <summary>
    ///     Gets a value indicating whether the instance has been disposed.
    /// </summary>
    public bool IsDisposed { get; private set; }

    /// <summary>
    ///     Initializes a new instance of the <see cref="AsyncTicketLock"/> class.
    /// </summary>
    public AsyncTicketLock()
        : this(int.MaxValue)
    {
    }
    /// <summary>
    ///     Initializes a new instance of the <see cref="AsyncTicketLock"/> class, specifying
    ///     the maximum number of requests that can be granted for a ticket concurrently.
    /// </summary>
    /// <param name="maximumCount">The maximum number of requests that can be granted for a ticket concurrently.</param>
    public AsyncTicketLock(int maximumCount)
    {
        _channel = Channel.CreateBounded<State>(capacity: 1);
        _state = new(maximumCount);
    }

    /// <summary>
    ///     Requests access to the lock for the specified <paramref name="ticket"/>.
    /// </summary>
    /// <param name="ticket">The object for which the lock is to be obtained.</param>
    /// <param name="timeout">A timeout for waiting for the lock.</param>
    /// <param name="cancellationToken">A token whose cancellation signals lost interest in the lock.</param>
    /// <returns>
    ///     A <see cref="ValueTask{Releaser}"/> whose result is a <see cref="Releaser"/> whose disposal releases the lock.
    ///     <br />
    ///     This <see cref="ValueTask{Releaser}"/> may be canceled if <paramref name="cancellationToken"/> is signaled before access is granted.
    ///     <br />
    ///     This <see cref="ValueTask{Releaser}"/> fails with <see cref="ObjectDisposedException"/> if this instance was disposed before access is granted.
    ///     <br />
    ///     This <see cref="ValueTask{Releaser}"/> fails with <see cref="TimeoutException"/> if the <paramref name="timeout"/> expires before access is granted.
    /// </returns>
    /// <exception cref="ArgumentNullException">Will be thrown if the <paramref name="ticket"/> is <see langword="null"/></exception>
    /// <exception cref="ObjectDisposedException">Thrown when the instance is used after <see cref="Dispose"/> is called</exception>
    public ValueTask<Releaser> EnterAsync(object ticket, TimeSpan timeout, CancellationToken cancellationToken = default)
    {
        if (timeout < TimeSpan.Zero)
            return EnterAsync(ticket, cancellationToken);

        return WithTimeout(ticket, timeout, cancellationToken);

        async ValueTask<Releaser> WithTimeout(object ticket, TimeSpan timeout, CancellationToken cancellationToken)
        {
            using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

            timeoutCts.CancelAfter(timeout);

            try
            {
                return await EnterAsync(ticket, timeoutCts.Token).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
                when (!cancellationToken.IsCancellationRequested)
            {
                throw new TimeoutException();
            }
        }
    }

    /// <summary>
    ///     Requests access to the lock for the specified <paramref name="ticket"/>.
    /// </summary>
    /// <param name="ticket">The object for which the lock is to be obtained.</param>
    /// <param name="cancellationToken">A token whose cancellation signals lost interest in the lock.</param>
    /// <returns>
    ///     A <see cref="ValueTask{Releaser}"/> whose result is a <see cref="Releaser"/> whose disposal releases the lock.
    ///     <br />
    ///     This <see cref="ValueTask{Releaser}"/> may be canceled if <paramref name="cancellationToken"/> is signaled before access is granted.
    ///     <br />
    ///     This <see cref="ValueTask{Releaser}"/> fail with <see cref="ObjectDisposedException"/> if this instance was disposed before access is granted.
    /// </returns>
    /// <exception cref="ArgumentNullException">Will be thrown if the <paramref name="ticket"/> is <see langword="null"/></exception>
    /// <exception cref="ObjectDisposedException">Thrown when the instance is used after <see cref="Dispose"/> is called</exception>
    public async ValueTask<Releaser> EnterAsync(object ticket, CancellationToken cancellationToken = default)
    {
        while (true)
        {
            if (TryEnter(ticket, out Releaser releaser))
                return releaser;

            await _channel.Writer
                .WaitToWriteAsync(cancellationToken)
                .ConfigureAwait(false);
        }
    }

    /// <summary>
    ///     Attempts to acquire access to the lock for the specified <paramref name="ticket"/>.
    /// </summary>
    /// <param name="ticket">The object for which the lock is to be obtained.</param>
    /// <param name="releaser">An <see cref="Releaser"/> whose disposal releases the lock.</param>
    /// <returns><see langword="true"/> if the lock was obtained, <see langword="false"/> otherwise.</returns>
    /// <exception cref="ArgumentNullException">Will be thrown if the <paramref name="ticket"/> is <see langword="null"/></exception>
    /// <exception cref="ObjectDisposedException">Thrown when the instance is used after <see cref="Dispose"/> is called</exception>
    public bool TryEnter(object ticket, out Releaser releaser)
    {
        if (TryEnter(ticket))
        {
            releaser = new Releaser(this, ticket);
            return true;
        }

        releaser = default;
        return false;
    }

    /// <summary>
    ///     Attempts to acquire access to the lock for the specified <paramref name="ticket"/>.
    /// </summary>
    /// <param name="ticket">The object for which the lock is to be obtained.</param>
    /// <returns><see langword="true"/> if the lock was obtained, <see langword="false"/> otherwise.</returns>
    /// <exception cref="ArgumentNullException">Will be thrown if the <paramref name="ticket"/> is <see langword="null"/></exception>
    /// <exception cref="ObjectDisposedException">Thrown when the instance is used after <see cref="Dispose"/> is called</exception>
    public bool TryEnter(object ticket)
    {
        ThrowHelper.ThrowIfDisposed<AsyncTicketLock>(IsDisposed);
        ThrowHelper.ThrowIfNull(ticket);

        lock (_state)
        {
            ThrowHelper.ThrowIfDisposed<AsyncTicketLock>(IsDisposed);

            if (_channel.Reader.TryPeek(out State? state) && state.Ticket is not null)
            {
                if (ReferenceEquals(ticket, state.Ticket))
                    return state.TryIncrementEnteredCount();

                return false;
            }
            else
            {
                var hasWritten = _channel.Writer.TryWrite(_state);

                Debug.Assert(hasWritten);

                _state.Reset(ticket);

                return true;
            }
        }
    }

    /// <summary>
    ///     Attempts to release the lock when the specified <paramref name="ticket"/> has entered the lock.
    /// </summary>
    /// <param name="ticket">The object for which the lock is to be released.</param>
    /// <returns><see langword="true"/> if the lock was released, <see langword="true"/> otherwise.</returns>
    /// <exception cref="ArgumentNullException">Will be thrown if the <paramref name="ticket"/> is <see langword="null"/></exception>
    /// <exception cref="ObjectDisposedException">Thrown when the instance is used after <see cref="Dispose"/> is called</exception>
    public bool Release(object ticket) => CoreRelease(ticket, releaseCount: 1) > 0;

    /// <summary>
    ///     Attempts to release the lock when the specified <paramref name="ticket"/> has entered the lock.
    /// </summary>
    /// <param name="ticket">The object for which the lock is to be released.</param>
    /// <param name="releaseCount">The number of times to release the lock.</param>
    /// <returns>The number of times the lock was released.</returns>
    /// <exception cref="ArgumentNullException">Will be thrown if the <paramref name="ticket"/> is <see langword="null"/></exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="releaseCount"/> is less than 1.</exception>
    /// <exception cref="ObjectDisposedException">Thrown when the instance is used after <see cref="Dispose"/> is called</exception>
    public int Release(object ticket, int releaseCount) => CoreRelease(ticket, releaseCount);

    /// <summary>
    ///     Attempts to release all locks when the specified <paramref name="ticket"/> has entered the lock.
    /// </summary>
    /// <param name="ticket">The object for which the lock is to be released.</param>
    /// <returns><see langword="true"/> the lock was released, <see langword="false"/> otherwise.</returns>
    /// <exception cref="ArgumentNullException">Will be thrown if the <paramref name="ticket"/> is <see langword="null"/></exception>
    /// <exception cref="ObjectDisposedException">Thrown when the instance is used after <see cref="Dispose"/> is called</exception>
    public bool ReleaseAll(object ticket) => CoreRelease(ticket, releaseCount: null) > 0;

    private int CoreRelease(object ticket, int? releaseCount)
    {
        ThrowHelper.ThrowIfDisposed<AsyncTicketLock>(IsDisposed);
        ThrowHelper.ThrowIfNull(ticket);

        if (releaseCount <= 0)
        {
            if (releaseCount == 0)
                return 0;

            throw new ArgumentOutOfRangeException(nameof(releaseCount), releaseCount, "The value must be greater than or equal to 1");
        }

        if (!_channel.Reader.TryPeek(out _))
            return 0;

        lock (_state)
        {
            ThrowHelper.ThrowIfDisposed<AsyncTicketLock>(IsDisposed);

            if (!_channel.Reader.TryPeek(out State? state))
                return 0;

            if (state.Ticket is null || !ReferenceEquals(ticket, state.Ticket))
                return 0;

            int releasedCount = releaseCount.GetValueOrDefault(state.EnteredCount);
            int enteredCount = state.DecrementEnteredCount(releasedCount);

            if (enteredCount <= 0)
            {
                releasedCount += enteredCount;

                var hasFreedChannel = _channel.Reader.TryRead(out State? readState);

                Debug.Assert(hasFreedChannel);
                Debug.Assert(readState is not null);
                Debug.Assert(ReferenceEquals(state.Ticket, readState!.Ticket));
            }

            return releasedCount;
        }
    }

    /// <summary>
    /// Rejects all pending waiters with <see cref="ObjectDisposedException"/> and rejects all subsequent attempts to enter the lock with the same exception.
    /// </summary>
    public void Dispose()
    {
        if (IsDisposed)
            return;

        lock (_state)
        {
            if (IsDisposed)
                return;

            IsDisposed = true;

            _channel.Writer.Complete(ThrowHelper.CreateDisposedException<AsyncTicketLock>());
        }
    }

    /// <summary>
    /// A value whose disposal triggers the release of a lock.
    /// </summary>
    public readonly struct Releaser : IDisposable
    {
        private readonly AsyncTicketLock _toRelease;
        private readonly object _ticket;

        internal Releaser(AsyncTicketLock toRelease, object ticket)
        {
            _toRelease = toRelease;
            _ticket = ticket;
        }

        /// <summary>
        /// Releases the lock.
        /// </summary>
        public void Dispose()
        {
            if (_toRelease is null)
                return;

            if (_toRelease.IsDisposed)
                return;

            try
            {
                _toRelease.Release(_ticket);
            }
            catch (ObjectDisposedException)
                when (_toRelease.IsDisposed)
            {
            }
        }
    }

    [DebuggerDisplay($"EnteredCount: {{{nameof(EnteredCount)},nq}}, UserState: {{{nameof(Ticket)},nq}}")]
    private sealed class State
    {
        private readonly int _maximumCount;

        public State(int maximumCount)
            => _maximumCount = maximumCount;

        public object? Ticket { get; private set; }
        public int EnteredCount { get; private set; }

        public void Reset(object ticket)
        {
            Ticket = ticket;
            EnteredCount = 1;
        }

        public bool TryIncrementEnteredCount()
        {
            if (EnteredCount >= _maximumCount)
                return false;

            EnteredCount++;

            return true;
        }
        public int DecrementEnteredCount(int count)
        {
            EnteredCount -= count;
            return EnteredCount;
        }
    }
}
