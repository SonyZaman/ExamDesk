namespace ExamManagement.Application.Common.Abstractions;

public interface ICurrentUser
{
    string? UserId { get; }
    bool IsInRole(string role);
}
