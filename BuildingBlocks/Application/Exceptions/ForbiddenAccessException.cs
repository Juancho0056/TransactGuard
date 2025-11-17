using System;

namespace BuildingBlocks.Application.Exceptions;

public class ForbiddenAccessException : Exception
{
    public ForbiddenAccessException()
    {
    }

    public ForbiddenAccessException(string message)
        : base(message)
    {
    }
}
