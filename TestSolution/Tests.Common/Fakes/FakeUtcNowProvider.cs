using BuildingBlocks.Domain.Interfaces;

namespace Tests.Common.Fakes;

public sealed class FakeUtcNowProvider : IUtcNowProvider
{
    private DateTimeOffset _utcNow;

    public FakeUtcNowProvider()
        : this(DateTimeOffset.UtcNow)
    {
    }

    public FakeUtcNowProvider(DateTimeOffset utcNow)
    {
        _utcNow = utcNow;
    }

    public DateTimeOffset UtcNow => _utcNow;

    public void Advance(TimeSpan by) => _utcNow = _utcNow.Add(by);

    public void Set(DateTimeOffset utcNow) => _utcNow = utcNow;
}
