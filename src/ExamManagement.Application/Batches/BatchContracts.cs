using ExamManagement.Domain.Enums;

namespace ExamManagement.Application.Batches;

public sealed record CreateBatchRequest(
    string Name,
    string? Description,
    DateTimeOffset? EnrollmentDeadlineUtc,
    bool OpenForEnrollment = false);

public sealed record BatchDto(
    Guid Id,
    string Name,
    string? Description,
    string InstructorId,
    DateTimeOffset? EnrollmentDeadlineUtc,
    BatchStatus Status,
    int ExamCount,
    int EnrollmentCount);

public sealed record EnrollmentDto(Guid Id, Guid BatchId, string StudentId, DateTimeOffset EnrolledAtUtc);

public interface IBatchService
{
    Task<BatchDto> CreateAsync(CreateBatchRequest request, CancellationToken cancellationToken);
    Task<IReadOnlyList<BatchDto>> ListAsync(CancellationToken cancellationToken);
    Task<EnrollmentDto> EnrollAsync(Guid batchId, CancellationToken cancellationToken);
}
