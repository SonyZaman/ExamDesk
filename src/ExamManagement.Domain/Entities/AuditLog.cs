namespace ExamManagement.Domain.Entities;

public sealed class AuditLog
{
    private AuditLog() { }

    public AuditLog(string? actorId, string action, string entityType, string entityId, DateTimeOffset createdAtUtc)
    {
        Id = Guid.NewGuid();
        ActorId = actorId;
        Action = action;
        EntityType = entityType;
        EntityId = entityId;
        CreatedAtUtc = createdAtUtc;
    }

    public Guid Id { get; private set; }
    public string? ActorId { get; private set; }
    public string Action { get; private set; } = string.Empty;
    public string EntityType { get; private set; } = string.Empty;
    public string EntityId { get; private set; } = string.Empty;
    public DateTimeOffset CreatedAtUtc { get; private set; }
}
