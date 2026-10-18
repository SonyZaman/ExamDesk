using ExamManagement.Application.Common.Abstractions;
using ExamManagement.Domain.Entities;
using ExamManagement.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace ExamManagement.Infrastructure.Persistence;

public sealed class ExamManagementDbContext(DbContextOptions<ExamManagementDbContext> options)
    : IdentityDbContext<ApplicationUser>(options), IExamManagementDbContext
{
    public DbSet<ExamBatch> ExamBatches => Set<ExamBatch>();
    public DbSet<BatchEnrollment> BatchEnrollments => Set<BatchEnrollment>();
    public DbSet<Exam> Exams => Set<Exam>();
    public DbSet<ExamQuestionFile> ExamQuestionFiles => Set<ExamQuestionFile>();
    public DbSet<ExamSubmission> ExamSubmissions => Set<ExamSubmission>();
    public DbSet<SubmissionFile> SubmissionFiles => Set<SubmissionFile>();
    public DbSet<Evaluation> Evaluations => Set<Evaluation>();
    public DbSet<ExamResult> ExamResults => Set<ExamResult>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();

    public async Task<IReadOnlyList<ExamBatch>> ListBatchesAsync(CancellationToken cancellationToken) =>
        await ExamBatches
            .AsNoTracking()
            .Include(x => x.Exams)
            .Include(x => x.Enrollments)
            .ToListAsync(cancellationToken);

    public Task<ExamBatch?> FindBatchAsync(Guid batchId, CancellationToken cancellationToken) =>
        ExamBatches.FirstOrDefaultAsync(x => x.Id == batchId, cancellationToken);

    public Task<bool> EnrollmentExistsAsync(Guid batchId, string studentId, CancellationToken cancellationToken) =>
        BatchEnrollments.AnyAsync(x => x.BatchId == batchId && x.StudentId == studentId, cancellationToken);

    public void AddBatch(ExamBatch batch) => ExamBatches.Add(batch);

    public void AddEnrollment(BatchEnrollment enrollment) => BatchEnrollments.Add(enrollment);

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<ApplicationUser>(entity =>
        {
            entity.Property(x => x.Name).HasMaxLength(160).IsRequired();
            entity.Property(x => x.IsActive).HasDefaultValue(true);
        });

        builder.Entity<ExamBatch>(entity =>
        {
            entity.ToTable("ExamBatches");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Name).HasMaxLength(200).IsRequired();
            entity.Property(x => x.Description).HasMaxLength(2000);
            entity.Property(x => x.InstructorId).HasMaxLength(450).IsRequired();
            entity.Property(x => x.Status).HasConversion<string>().HasMaxLength(32);
            entity.HasIndex(x => x.InstructorId);
            entity.HasMany(x => x.Exams).WithOne(x => x.Batch).HasForeignKey(x => x.BatchId).OnDelete(DeleteBehavior.Restrict);
            entity.HasMany(x => x.Enrollments).WithOne(x => x.Batch).HasForeignKey(x => x.BatchId).OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<BatchEnrollment>(entity =>
        {
            entity.ToTable("BatchEnrollments");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.StudentId).HasMaxLength(450).IsRequired();
            entity.Property(x => x.Status).HasConversion<string>().HasMaxLength(32);
            entity.HasIndex(x => new { x.BatchId, x.StudentId }).IsUnique();
        });

        builder.Entity<Exam>(entity =>
        {
            entity.ToTable("Exams");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Title).HasMaxLength(200).IsRequired();
            entity.Property(x => x.FullMarks).HasPrecision(10, 2);
            entity.Property(x => x.Status).HasConversion<string>().HasMaxLength(32);
        });

        builder.Entity<ExamQuestionFile>(entity =>
        {
            entity.ToTable("ExamQuestionFiles");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.StorageKey).HasMaxLength(1000).IsRequired();
            entity.Property(x => x.OriginalFileName).HasMaxLength(255).IsRequired();
            entity.HasOne(x => x.Exam).WithMany(x => x.QuestionFiles).HasForeignKey(x => x.ExamId).OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<ExamSubmission>(entity =>
        {
            entity.ToTable("ExamSubmissions");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.StudentId).HasMaxLength(450).IsRequired();
            entity.Property(x => x.Status).HasConversion<string>().HasMaxLength(32);
            entity.HasIndex(x => new { x.ExamId, x.StudentId }).IsUnique();
            entity.HasOne(x => x.Exam).WithMany(x => x.Submissions).HasForeignKey(x => x.ExamId).OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<SubmissionFile>(entity =>
        {
            entity.ToTable("SubmissionFiles");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.StorageKey).HasMaxLength(1000).IsRequired();
            entity.Property(x => x.OriginalFileName).HasMaxLength(255).IsRequired();
            entity.HasIndex(x => new { x.SubmissionId, x.PageNumber }).IsUnique();
            entity.HasOne(x => x.Submission).WithMany(x => x.Files).HasForeignKey(x => x.SubmissionId).OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<Evaluation>(entity =>
        {
            entity.ToTable("Evaluations");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.EvaluatorId).HasMaxLength(450).IsRequired();
            entity.Property(x => x.Marks).HasPrecision(10, 2);
            entity.Property(x => x.Feedback).HasMaxLength(5000);
            entity.Property(x => x.Status).HasConversion<string>().HasMaxLength(32);
            entity.HasIndex(x => x.SubmissionId).IsUnique();
            entity.HasOne(x => x.Submission).WithOne(x => x.Evaluation).HasForeignKey<Evaluation>(x => x.SubmissionId).OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<ExamResult>(entity =>
        {
            entity.ToTable("ExamResults");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.StudentId).HasMaxLength(450).IsRequired();
            entity.Property(x => x.Marks).HasPrecision(10, 2);
            entity.HasIndex(x => new { x.ExamId, x.StudentId }).IsUnique();
            entity.HasOne(x => x.Exam).WithMany().HasForeignKey(x => x.ExamId).OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<AuditLog>(entity =>
        {
            entity.ToTable("AuditLogs");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.ActorId).HasMaxLength(450);
            entity.Property(x => x.Action).HasMaxLength(120).IsRequired();
            entity.Property(x => x.EntityType).HasMaxLength(120).IsRequired();
            entity.Property(x => x.EntityId).HasMaxLength(450).IsRequired();
            entity.HasIndex(x => x.CreatedAtUtc);
        });
    }
}
