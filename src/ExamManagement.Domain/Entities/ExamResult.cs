namespace ExamManagement.Domain.Entities;

public sealed class ExamResult
{
    private ExamResult() { }

    public ExamResult(Guid examId, string studentId, decimal marks, DateTimeOffset publishedAtUtc)
    {
        Id = Guid.NewGuid();
        ExamId = examId;
        StudentId = studentId;
        Marks = marks;
        PublishedAtUtc = publishedAtUtc;
    }

    public Guid Id { get; private set; }
    public Guid ExamId { get; private set; }
    public string StudentId { get; private set; } = string.Empty;
    public decimal Marks { get; private set; }
    public DateTimeOffset PublishedAtUtc { get; private set; }
    public Exam Exam { get; private set; } = null!;
}
