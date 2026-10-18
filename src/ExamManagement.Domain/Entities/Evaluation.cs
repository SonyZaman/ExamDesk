using ExamManagement.Domain.Common;
using ExamManagement.Domain.Enums;

namespace ExamManagement.Domain.Entities;

public sealed class Evaluation
{
    private Evaluation() { }

    public Evaluation(Guid submissionId, string evaluatorId)
    {
        Id = Guid.NewGuid();
        SubmissionId = submissionId;
        EvaluatorId = evaluatorId;
        Status = EvaluationStatus.Draft;
    }

    public Guid Id { get; private set; }
    public Guid SubmissionId { get; private set; }
    public string EvaluatorId { get; private set; } = string.Empty;
    public decimal Marks { get; private set; }
    public string? Feedback { get; private set; }
    public EvaluationStatus Status { get; private set; }
    public DateTimeOffset? FinalizedAtUtc { get; private set; }
    public ExamSubmission Submission { get; private set; } = null!;

    public void Save(decimal marks, decimal fullMarks, string? feedback)
    {
        if (marks < 0 || marks > fullMarks)
            throw new DomainException($"Marks must be between 0 and {fullMarks}.");
        if (Status == EvaluationStatus.Finalized)
            throw new DomainException("A finalized evaluation cannot be edited.");

        Marks = marks;
        Feedback = string.IsNullOrWhiteSpace(feedback) ? null : feedback.Trim();
    }

    public void Finalize(DateTimeOffset now)
    {
        Status = EvaluationStatus.Finalized;
        FinalizedAtUtc = now;
    }
}
