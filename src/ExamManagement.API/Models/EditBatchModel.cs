using System.ComponentModel.DataAnnotations;
using ExamManagement.Domain.Enums;
namespace ExamManagement.API.Models;
public sealed class EditBatchModel
{
    [Required, StringLength(200)] public string Name { get; set; } = "";
    [StringLength(2000)] public string? Description { get; set; }
    public DateTime? EnrollmentDeadline { get; set; }
    [EnumDataType(typeof(BatchStatus))] public BatchStatus Status { get; set; }
}
