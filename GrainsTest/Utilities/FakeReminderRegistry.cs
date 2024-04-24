using System.Collections.Concurrent;
using Orleans.Runtime;
using Orleans.Runtime.Services;
using Orleans.Timers;

namespace GrainsTest.Utilities;

public class FakeReminderRegistry : IReminderRegistry
{
    private readonly ConcurrentDictionary<GrainReference, ConcurrentDictionary<string, FakeReminder>> reminders =
        new ConcurrentDictionary<GrainReference, ConcurrentDictionary<string, FakeReminder>>();

    //private IReminderRegistry _reminderRegistryImplementation;

    private ConcurrentDictionary<string, FakeReminder> GetRemindersFor(GrainReference reference) =>
        reminders.GetOrAdd(reference, _ => new ConcurrentDictionary<string, FakeReminder>());
    
    public Task<IGrainReminder> RegisterOrUpdateReminder(GrainId callingGrainId, string reminderName, TimeSpan dueTime, TimeSpan period)
    {
        return Task.FromResult<IGrainReminder>(new FakeReminder(reminderName, dueTime, period));
        //return _reminderRegistryImplementation.RegisterOrUpdateReminder(callingGrainId, reminderName, dueTime, period);
    }

    public Task UnregisterReminder(GrainId callingGrainId, IGrainReminder reminder)
    {
        return Task.CompletedTask;
        //return _reminderRegistryImplementation.UnregisterReminder(callingGrainId, reminder);
    }

    public Task<IGrainReminder> GetReminder(GrainId callingGrainId, string reminderName)
    {
        return new Task<IGrainReminder>(() =>
        {
            var reminder = new FakeReminder(reminderName, TimeSpan.Zero, TimeSpan.Zero);
            return reminder;
        });
        //return _reminderRegistryImplementation.GetReminder(callingGrainId, reminderName);
    }

    public Task<List<IGrainReminder>> GetReminders(GrainId callingGrainId)
    {
        return new Task<List<IGrainReminder>>(() =>
        {
            return new List<IGrainReminder>();
        });
        //return _reminderRegistryImplementation.GetReminders(callingGrainId);
    }
    
    #region Test Helpers

    public Task<FakeReminder> GetReminder(GrainReference grainRef, string reminderName)
    {
        GetRemindersFor(grainRef).TryGetValue(reminderName, out var reminder);
        return Task.FromResult(reminder);
    }

    #endregion Test Helpers
}

public class FakeReminder : IGrainReminder
{
    public FakeReminder(string reminderName, TimeSpan dueTime, TimeSpan period)
    {
        ReminderName = reminderName;
        DueTime = dueTime;
        Period = period;
    }

    public string ReminderName { get; }
    public TimeSpan DueTime { get; }
    public TimeSpan Period { get; }
}