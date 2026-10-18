using ExamManagement.Application.Common;
using ExamManagement.Application.Common.Abstractions;
using ExamManagement.Domain.Entities;
using ExamManagement.Domain.Enums;

namespace ExamManagement.Application.Batches;

public sealed class BatchService(
    IExamManagementDbContext db,
    ICurrentUser currentUser,
    IClock clock) : IBatchService
{
    public async Task<BatchDto> CreateAsync(CreateBatchRequest request, CancellationToken cancellationToken)
    {
        var userId = RequireUser();
        if (!currentUser.IsInRole(AppRoles.Instructor) && !currentUser.IsInRole(AppRoles.Admin))
            throw new ForbiddenException("Only instructors and administrators can create exam batches.");

        if (request.EnrollmentDeadlineUtc <= clock.UtcNow)
            throw new ValidationException("Enrollment deadline must be in the future.");

        var batch = new ExamBatch(request.Name, request.Description, userId, request.EnrollmentDeadlineUtc);
        if (request.OpenForEnrollment)
            batch.OpenEnrollment();

        db.AddBatch(batch);
        await db.SaveChangesAsync(cancellationToken);
        return Map(batch);
    }

    public async Task<IReadOnlyList<BatchDto>> ListAsync(CancellationToken cancellationToken)
    {
        _ = RequireUser();

        var batches = await db.ListBatchesAsync(cancellationToken);
        if (currentUser.IsInRole(AppRoles.Student))
            batches = batches.Where(x => x.Status == BatchStatus.Open).ToList();

        return batches.Select(Map).ToList();
    }

    public async Task<EnrollmentDto> EnrollAsync(Guid batchId, CancellationToken cancellationToken)
    {
        var studentId = RequireUser();
        if (!currentUser.IsInRole(AppRoles.Student))
            throw new ForbiddenException("Only students can enroll in exam batches.");

        var batch = await db.FindBatchAsync(batchId, cancellationToken)
            ?? throw new NotFoundException("Exam batch was not found.");

        if (!batch.CanEnroll(clock.UtcNow))
            throw new ConflictException("Enrollment is not open for this batch.");

        if (await db.EnrollmentExistsAsync(batchId, studentId, cancellationToken))
            throw new ConflictException("You are already enrolled in this batch.");

        var enrollment = new BatchEnrollment(batchId, studentId, clock.UtcNow);
        db.AddEnrollment(enrollment);
        await db.SaveChangesAsync(cancellationToken);

        return new EnrollmentDto(enrollment.Id, enrollment.BatchId, enrollment.StudentId, enrollment.EnrolledAtUtc);
    }

    private string RequireUser() =>
        currentUser.UserId ?? throw new ForbiddenException("An authenticated user is required.");

    private static BatchDto Map(ExamBatch batch) => new(
        batch.Id,
        batch.Name,
        batch.Description,
        batch.InstructorId,
        batch.EnrollmentDeadlineUtc,
        batch.Status,
        batch.Exams.Count,
        batch.Enrollments.Count);
}
