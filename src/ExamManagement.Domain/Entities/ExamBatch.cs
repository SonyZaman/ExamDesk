using ExamManagement.Domain.Common;
using ExamManagement.Domain.Enums;

namespace ExamManagement.Domain.Entities;

public sealed class ExamBatch
{
    private ExamBatch() { }

    public ExamBatch(string name, string? description, string instructorId, DateTimeOffset? enrollmentDeadlineUtc)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new DomainException("Batch name is required.");
        if (string.IsNullOrWhiteSpace(instructorId))
            throw new DomainException("An instructor is required.");

        Id = Guid.NewGuid();
        Name = name.Trim();
        Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim();
        InstructorId = instructorId;
        EnrollmentDeadlineUtc = enrollmentDeadlineUtc;
        Status = BatchStatus.Draft;
        CreatedAtUtc = DateTimeOffset.UtcNow;
    }

    public Guid Id { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public string? Description { get; private set; }
    public string InstructorId { get; private set; } = string.Empty;
    public DateTimeOffset? EnrollmentDeadlineUtc { get; private set; }
    public BatchStatus Status { get; private set; }
    public DateTimeOffset CreatedAtUtc { get; private set; }
    public ICollection<Exam> Exams { get; private set; } = new List<Exam>();
    public ICollection<BatchEnrollment> Enrollments { get; private set; } = new List<BatchEnrollment>();

    public void OpenEnrollment()
    {
        if (Status is BatchStatus.Archived)
            throw new DomainException("An archived batch cannot be reopened.");

        Status = BatchStatus.Open;
    }

    public bool CanEnroll(DateTimeOffset now) =>
        Status == BatchStatus.Open &&
        (EnrollmentDeadlineUtc is null || now <= EnrollmentDeadlineUtc.Value);
    public void Edit(string name, string? description, DateTimeOffset? deadline, BatchStatus status)
    {
        if (string.IsNullOrWhiteSpace(name) || name.Trim().Length > 200) throw new DomainException("Batch name must contain 1–200 characters.");
        if (!Enum.IsDefined(status)) throw new DomainException("Invalid batch status.");
        Name = name.Trim(); Description = description?.Trim(); EnrollmentDeadlineUtc = deadline; Status = status;
    }
}
