namespace ExamManagement.Domain.Entities;

public sealed class SubmissionFile
{
    private SubmissionFile() { }

    public SubmissionFile(Guid submissionId, string storageKey, string originalFileName, int pageNumber)
    {
        Id = Guid.NewGuid();
        SubmissionId = submissionId;
        StorageKey = storageKey;
        OriginalFileName = originalFileName;
        PageNumber = pageNumber;
    }

    public Guid Id { get; private set; }
    public Guid SubmissionId { get; private set; }
    public string StorageKey { get; private set; } = string.Empty;
    public string OriginalFileName { get; private set; } = string.Empty;
    public int PageNumber { get; private set; }
    public ExamSubmission Submission { get; private set; } = null!;
}
