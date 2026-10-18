using ExamManagement.Domain.Common;
using ExamManagement.Domain.Enums;

namespace ExamManagement.Domain.Entities;

public sealed class ExamSubmission
{
    private ExamSubmission() { }

    public ExamSubmission(Guid examId, string studentId, DateTimeOffset createdAtUtc)
    {
        Id = Guid.NewGuid();
        ExamId = examId;
        StudentId = studentId;
        CreatedAtUtc = createdAtUtc;
        Status = SubmissionStatus.Draft;
    }

    public Guid Id { get; private set; }
    public Guid ExamId { get; private set; }
    public string StudentId { get; private set; } = string.Empty;
    public DateTimeOffset CreatedAtUtc { get; private set; }
    public DateTimeOffset? SubmittedAtUtc { get; private set; }
    public SubmissionStatus Status { get; private set; }
    public Exam Exam { get; private set; } = null!;
    public ICollection<SubmissionFile> Files { get; private set; } = new List<SubmissionFile>();
    public Evaluation? Evaluation { get; private set; }

    public void Finalize(DateTimeOffset now, DateTimeOffset deadline)
    {
        if (Status == SubmissionStatus.Finalized)
            throw new DomainException("Submission has already been finalized.");
        if (Files.Count == 0)
            throw new DomainException("At least one answer image is required.");
        if (now > deadline)
            throw new DomainException("Submission deadline has passed.");

        SubmittedAtUtc = now;
        Status = SubmissionStatus.Finalized;
    }
}
