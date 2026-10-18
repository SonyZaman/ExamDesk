namespace ExamManagement.Domain.Entities;

public sealed class ExamQuestionFile
{
    private ExamQuestionFile() { }

    public ExamQuestionFile(Guid examId, string storageKey, string originalFileName, DateTimeOffset uploadedAtUtc)
    {
        Id = Guid.NewGuid();
        ExamId = examId;
        StorageKey = storageKey;
        OriginalFileName = originalFileName;
        UploadedAtUtc = uploadedAtUtc;
    }

    public Guid Id { get; private set; }
    public Guid ExamId { get; private set; }
    public string StorageKey { get; private set; } = string.Empty;
    public string OriginalFileName { get; private set; } = string.Empty;
    public DateTimeOffset UploadedAtUtc { get; private set; }
    public Exam Exam { get; private set; } = null!;
}
