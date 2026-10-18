using ExamManagement.Domain.Enums;

namespace ExamManagement.Domain.Entities;

public sealed class BatchEnrollment
{
    private BatchEnrollment() { }

    public BatchEnrollment(Guid batchId, string studentId, DateTimeOffset enrolledAtUtc)
    {
        Id = Guid.NewGuid();
        BatchId = batchId;
        StudentId = studentId;
        EnrolledAtUtc = enrolledAtUtc;
        Status = EnrollmentStatus.Active;
    }

    public Guid Id { get; private set; }
    public Guid BatchId { get; private set; }
    public string StudentId { get; private set; } = string.Empty;
    public DateTimeOffset EnrolledAtUtc { get; private set; }
    public EnrollmentStatus Status { get; private set; }
    public ExamBatch Batch { get; private set; } = null!;
}
