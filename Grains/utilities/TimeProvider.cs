namespace Grains.utilities;

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