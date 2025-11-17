using BuildingBlocks.Domain.Interfaces;

namespace BuildingBlocks.Domain.Time;

public sealed class SystemUtcNowProvider : IUtcNowProvider
{
    public DateTimeOffset UtcNow => DateTimeOffset.UtcNow;
}
