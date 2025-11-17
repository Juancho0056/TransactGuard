using System.Collections.Generic;

namespace BuildingBlocks.Application.Abstractions.Identity;

public interface IUser
{
    string? Id { get; }

    IReadOnlyCollection<string>? Roles { get; }
}
