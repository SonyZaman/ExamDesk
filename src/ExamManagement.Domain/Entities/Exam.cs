using ExamManagement.Domain.Common;
using ExamManagement.Domain.Enums;

namespace ExamManagement.Domain.Entities;

public sealed class Exam
{
    private Exam() { }

    public Exam(
        Guid batchId,
        string title,
        decimal fullMarks,
        DateTimeOffset questionReleaseAtUtc,
        DateTimeOffset examEndAtUtc,
        DateTimeOffset submissionDeadlineAtUtc)
    {
        if (string.IsNullOrWhiteSpace(title))
            throw new DomainException("Exam title is required.");
        if (fullMarks <= 0)
            throw new DomainException("Full marks must be greater than zero.");
        if (examEndAtUtc <= questionReleaseAtUtc)
            throw new DomainException("Exam end time must be after question release time.");
        if (submissionDeadlineAtUtc < examEndAtUtc)
            throw new DomainException("Submission deadline cannot be before the exam ends.");

        Id = Guid.NewGuid();
        BatchId = batchId;
        Title = title.Trim();
        FullMarks = fullMarks;
        QuestionReleaseAtUtc = questionReleaseAtUtc;
        ExamEndAtUtc = examEndAtUtc;
        SubmissionDeadlineAtUtc = submissionDeadlineAtUtc;
        Status = ExamStatus.Scheduled;
    }

    public Guid Id { get; private set; }
    public Guid BatchId { get; private set; }
    public string Title { get; private set; } = string.Empty;
    public decimal FullMarks { get; private set; }
    public DateTimeOffset QuestionReleaseAtUtc { get; private set; }
    public DateTimeOffset ExamEndAtUtc { get; private set; }
    public DateTimeOffset SubmissionDeadlineAtUtc { get; private set; }
    public DateTimeOffset? ResultPublishedAtUtc { get; private set; }
    public ExamStatus Status { get; private set; }
    public ExamBatch Batch { get; private set; } = null!;
    public ICollection<ExamQuestionFile> QuestionFiles { get; private set; } = new List<ExamQuestionFile>();
    public ICollection<ExamSubmission> Submissions { get; private set; } = new List<ExamSubmission>();

    public bool CanViewQuestion(DateTimeOffset now) =>
        Status == ExamStatus.Scheduled && now >= QuestionReleaseAtUtc;

    public bool CanSubmit(DateTimeOffset now) =>
        Status == ExamStatus.Scheduled &&
        now >= QuestionReleaseAtUtc &&
        now <= SubmissionDeadlineAtUtc;

    public void PublishResults(DateTimeOffset now) => ResultPublishedAtUtc = now;
    public void Edit(string title, decimal fullMarks, DateTimeOffset release, DateTimeOffset end, DateTimeOffset deadline)
    {
        var validated = new Exam(BatchId, title, fullMarks, release, end, deadline);
        Title = validated.Title;
        FullMarks = validated.FullMarks;
        QuestionReleaseAtUtc = release;
        ExamEndAtUtc = end;
        SubmissionDeadlineAtUtc = deadline;
    }
}
