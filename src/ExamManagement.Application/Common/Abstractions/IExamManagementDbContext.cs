using ExamManagement.Domain.Entities;

namespace ExamManagement.Application.Common.Abstractions;

public interface IExamManagementDbContext
{
    Task<IReadOnlyList<ExamBatch>> ListBatchesAsync(CancellationToken cancellationToken);
    Task<ExamBatch?> FindBatchAsync(Guid batchId, CancellationToken cancellationToken);
    Task<bool> EnrollmentExistsAsync(Guid batchId, string studentId, CancellationToken cancellationToken);
    void AddBatch(ExamBatch batch);
    void AddEnrollment(BatchEnrollment enrollment);
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
