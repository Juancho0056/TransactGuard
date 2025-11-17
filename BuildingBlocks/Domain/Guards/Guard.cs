using BuildingBlocks.Domain.Messages;

namespace BuildingBlocks.Domain.Guards;

public static class Guard
{
    public static void AgainstNull(object? value, string? message = null, string? parameterName = null)
    {
        if (value is null)
        {
            throw new ArgumentNullException(parameterName ?? "value", message ?? DefaultMessage.IsRequired);
        }
    }

    public static void AgainstEmptyGuid(Guid value, string? parameterName = null)
    {
        if (value == Guid.Empty)
        {
            throw new ArgumentException(DefaultMessage.InvalidValue, parameterName ?? "value");
        }
    }

    public static void AgainstNegativeOrZero(decimal value, string? parameterName = null)
    {
        if (value <= 0)
        {
            throw new ArgumentOutOfRangeException(parameterName ?? "value", value, DefaultMessage.InvalidValue);
        }
    }
}
