namespace BuildingBlocks.Domain.Interfaces;

public interface IUtcNowProvider
{
    DateTimeOffset UtcNow { get; }
}
