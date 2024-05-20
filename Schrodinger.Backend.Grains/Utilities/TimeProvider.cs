namespace Schrodinger.Backend.Grains.Utilities;

public abstract class TimeProvider
{
    public abstract DateTime UtcNow { get; }
}

public class DefaultTimeProvider : TimeProvider
{
    public override DateTime UtcNow
    {
        get { return DateTime.UtcNow; }
    }
}
