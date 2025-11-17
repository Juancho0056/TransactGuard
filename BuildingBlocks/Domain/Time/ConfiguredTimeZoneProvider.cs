using BuildingBlocks.Domain.Guards;
using BuildingBlocks.Domain.Interfaces;
using Microsoft.Extensions.Options;

namespace BuildingBlocks.Domain.Time;

public sealed class ConfiguredTimeZoneProvider : ITimeZoneProvider
{
    private readonly TimeZoneInfo _timeZone;

    public ConfiguredTimeZoneProvider(IOptions<TimeZoneOptions> options)
    {
        Guard.AgainstNull(options, parameterName: nameof(options));
        var optionsValue = options.Value ?? throw new ArgumentNullException(nameof(options));
        if (string.IsNullOrWhiteSpace(optionsValue.TimeZoneId))
        {
            throw new ArgumentException("Time zone id must be provided.", nameof(options));
        }

        _timeZone = TimeZoneInfo.FindSystemTimeZoneById(optionsValue.TimeZoneId);
    }

    public TimeZoneInfo GetTimeZone() => _timeZone;
}
