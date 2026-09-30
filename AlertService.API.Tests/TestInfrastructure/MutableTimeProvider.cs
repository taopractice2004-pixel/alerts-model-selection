namespace AlertService.API.Tests.TestInfrastructure;

/// <summary>A <see cref="TimeProvider"/> whose current time can be advanced from tests.</summary>
public sealed class MutableTimeProvider : TimeProvider
{
    private DateTimeOffset _utcNow;

    public MutableTimeProvider(DateTimeOffset utcNow) => _utcNow = utcNow;

    public override DateTimeOffset GetUtcNow() => _utcNow;

    public void SetUtcNow(DateTimeOffset utcNow) => _utcNow = utcNow;

    public void Advance(TimeSpan duration) => _utcNow += duration;
}
