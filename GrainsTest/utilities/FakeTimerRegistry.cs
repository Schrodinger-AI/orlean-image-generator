using System.Collections.Concurrent;
using Orleans.Runtime;
using Orleans.Timers;

/// <summary>
/// Implements a fake timer entry to facilitate unit testing.
/// </summary>
public class FakeTimerEntry : IDisposable
{
    private readonly TaskScheduler scheduler;
    private readonly FakeTimerRegistry owner;
    public Func<object, Task> AsyncCallback { get; }
    public object State { get; }
    public TimeSpan DueTime { get; }
    public TimeSpan DuePeriod { get; }

    public FakeTimerEntry(FakeTimerRegistry owner, TaskScheduler scheduler, Func<object, Task> asyncCallback, object state, TimeSpan dueTime, TimeSpan period)
    {
        this.scheduler = scheduler;
        this.owner = owner;

        AsyncCallback = asyncCallback;
        State = state;
        DueTime = dueTime;
        DuePeriod = period;
    }

    /// <summary>
    /// Ticks the timer action within the activation context.
    /// </summary>
    public async Task TickAsync() => await await Task.Factory.StartNew(AsyncCallback, State, default, TaskCreationOptions.None, scheduler);

    public void Dispose()
    {
        try
        {
            owner.Remove(this);
        }
        catch (Exception)
        {
            // noop
        }
    }
}

/// <summary>
/// Implements a fake timer registry to facilitate unit tests using the test cluster.
/// </summary>
public class FakeTimerRegistry : ITimerRegistry
{
    /// <summary>
    /// We dont have a ConcurrentHashSet yet so this does the job.
    /// </summary>
    private readonly ConcurrentDictionary<FakeTimerEntry, FakeTimerEntry> timers = new ConcurrentDictionary<FakeTimerEntry, FakeTimerEntry>();

    /// <summary>
    /// Returns all fake timer entries.
    /// </summary>
    public IEnumerable<FakeTimerEntry> GetAll() => timers.Keys.ToList();

    /// <summary>
    /// Removes a timer.
    /// </summary>
    public void Remove(FakeTimerEntry entry) => timers.TryRemove(entry, out _);

    public IDisposable RegisterTimer(IGrainContext grainContext, Func<object, Task> asyncCallback, object state, TimeSpan dueTime,
        TimeSpan period)
    {
        var timer = new FakeTimerEntry(this, TaskScheduler.Current, asyncCallback, state, dueTime, period);
        timers[timer] = timer;
        return timer;
    }
}