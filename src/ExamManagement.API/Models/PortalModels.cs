using System.ComponentModel.DataAnnotations;
using ExamManagement.Domain.Entities;
namespace ExamManagement.API.Models;
public sealed class LoginModel
{
    [Required, EmailAddress] public string Email { get; set; } = "";
    [Required, DataType(DataType.Password)] public string Password { get; set; } = "";
}
public sealed class RegisterModel
{
    [Required, StringLength(160)] public string Name { get; set; } = "";
    [Required, EmailAddress] public string Email { get; set; } = "";
    [Required, MinLength(8), DataType(DataType.Password)] public string Password { get; set; } = "";
}
public sealed class ExamModel
{
    public Guid BatchId { get; set; }
    [Required, StringLength(200)] public string Title { get; set; } = "";
    [Range(1, 10000)] public decimal FullMarks { get; set; } = 100;
    public DateTime Release { get; set; } = DateTime.Today.AddDays(1).AddHours(9);
    public DateTime End { get; set; } = DateTime.Today.AddDays(1).AddHours(10);
    public DateTime Deadline { get; set; } = DateTime.Today.AddDays(1).AddHours(10).AddMinutes(15);
}
public sealed record DashboardModel(List<ExamBatch> Batches, List<Exam> Exams, List<ExamResult> Results);
public sealed record ExamPage(Exam Exam, bool CanManage, bool Enrolled, ExamSubmission? Submission, List<ExamSubmission> Submissions, List<ExamResult> Results, Dictionary<string,string> Names);
