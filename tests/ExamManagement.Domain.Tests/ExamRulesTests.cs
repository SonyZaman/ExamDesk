using ExamManagement.Domain.Common;
using ExamManagement.Domain.Entities;
using Xunit;

namespace ExamManagement.Domain.Tests;

public sealed class ExamRulesTests
{
    [Fact]
    public void Exam_rejects_a_submission_deadline_before_the_exam_ends()
    {
        var release = new DateTimeOffset(2026, 11, 1, 3, 0, 0, TimeSpan.Zero);

        var action = () => new Exam(
            Guid.NewGuid(),
            "Programming",
            100,
            release,
            release.AddHours(1),
            release.AddMinutes(30));

        Assert.Throws<DomainException>(action);
    }

    [Fact]
    public void Exam_allows_submission_at_the_exact_deadline()
    {
        var release = new DateTimeOffset(2026, 11, 1, 3, 0, 0, TimeSpan.Zero);
        var deadline = release.AddHours(1).AddMinutes(15);
        var exam = new Exam(Guid.NewGuid(), "Programming", 100, release, release.AddHours(1), deadline);

        Assert.True(exam.CanSubmit(deadline));
        Assert.False(exam.CanSubmit(deadline.AddTicks(1)));
    }

    [Fact]
    public void Evaluation_rejects_marks_above_full_marks()
    {
        var evaluation = new Evaluation(Guid.NewGuid(), "instructor-1");

        var action = () => evaluation.Save(101, 100, null);

        Assert.Throws<DomainException>(action);
    }

    [Fact]
    public void Batch_enrollment_requires_open_status_and_honors_deadline()
    {
        var now = new DateTimeOffset(2026, 11, 1, 3, 0, 0, TimeSpan.Zero);
        var batch = new ExamBatch("MSc Preparation", null, "instructor-1", now.AddDays(1));

        Assert.False(batch.CanEnroll(now));

        batch.OpenEnrollment();

        Assert.True(batch.CanEnroll(now));
        Assert.False(batch.CanEnroll(now.AddDays(2)));
    }
}
