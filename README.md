# Loop8ack.AsyncTicketLock

[![Nuget](https://img.shields.io/nuget/v/Loop8ack.AsyncTicketLock)](https://www.nuget.org/packages/Loop8ack.AsyncTicketLock)

An asynchronous class that functions as a lock based on provided ticket objects instead of the current thread.
Only one ticket object can hold the lock at a time, accesses with the same ticket object are allowed, accesses with other ticket objects are not allowed.

I addressed the original idea in the following forum topic:\
https://mycsharp.de/forum/threads/124593/async-task-synchronisation

Special thanks to [gfoidl](https://github.com/gfoidl), who contributed the basic implementation and who gave me advice and support.

## How to Use

### Enter/Release with using

``` csharp
var ticketLock = new AsyncTicketLock();
var ticket = new object();

// async
using (await ticketLock.EnterAsync(ticket))
{
    // Do work
}

// sync
if (ticketLock.TryEnter(ticket, out var releaser))
{
    using (releaser)
    {
        // ...
    }
}
```

### Enter/Release manually

``` csharp
var ticketLock = new AsyncTicketLock();
var ticket = new object();

// async
await ticketLock.EnterAsync(ticket);
// Do work
ticketLock.Release(ticket); // true

// sync
if (ticketLock.TryEnter(ticket))
{
    ticketLock.TryEnter(new object()); // false

    ticketLock.Release(ticket);
}
```

### Nested lock

``` csharp
var ticketLock = new AsyncTicketLock();
var ticket = new object();

await ticketLock.EnterAsync(ticket);
await ticketLock.EnterAsync(ticket);
await ticketLock.EnterAsync(ticket);
await ticketLock.EnterAsync(ticket);

ticketLock.Release(ticket, 2); // returns 2
ticketLock.Release(ticket);    // returns true
ticketLock.Release(ticket, 2); // returns 1
ticketLock.ReleaseAll(ticket); // returns false
```

### With timeout

``` csharp
var ticketLock = new AsyncTicketLock();
var ticket1 = new object();
var ticket2 = new object();

await ticketLock.EnterAsync(ticket1);

// TimeoutException after one second
await ticketLock.EnterAsync(ticket2, TimeSpan.FromSeconds(1));
```

### With maximum entered count

```csharp
var ticketLock = new AsyncTicketLock(3);
var ticket = new object();

ticketLock.TryEnter(ticket); // true
ticketLock.TryEnter(ticket); // true
ticketLock.TryEnter(ticket); // true
ticketLock.TryEnter(ticket); // false

ticketLock.Release(ticket, 2);

ticketLock.TryEnter(ticket); // true
ticketLock.TryEnter(ticket); // true
ticketLock.TryEnter(ticket); // false
```

## Release Notes

### 1.1.0

- Added support for `MaxEnteredCount` parameter: Users can now limit the number of times a ticket object can enter the lock.

## License

This project is licensed under the [MIT License](LICENSE).
