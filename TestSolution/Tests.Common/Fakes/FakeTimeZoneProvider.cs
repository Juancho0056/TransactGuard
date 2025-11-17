using System;
using BuildingBlocks.Domain.Interfaces;

namespace Tests.Common.Fakes;

public sealed class FakeTimeZoneProvider : ITimeZoneProvider
{
    private readonly TimeZoneInfo _timeZone;

    public FakeTimeZoneProvider()
        : this(TimeZoneInfo.Utc)
    {
    }

    public FakeTimeZoneProvider(TimeZoneInfo timeZone)
    {
        _timeZone = timeZone;
    }

    public TimeZoneInfo GetTimeZone() => _timeZone;
}
