namespace BuildingBlocks.Domain.Interfaces;

public interface IAuditableEntity
{
    string? CreatedBy { get; set; }
    DateTimeOffset Created { get; set; }
    string? LastModifiedBy { get; set; }
    DateTimeOffset? LastModified { get; set; }
}
