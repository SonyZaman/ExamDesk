namespace ExamManagement.Application.Auth;

public sealed record RegisterRequest(string Name, string Email, string Password);
public sealed record LoginRequest(string Email, string Password);
public sealed record AuthResponse(string AccessToken, DateTimeOffset ExpiresAtUtc, UserDto User);
public sealed record UserDto(string Id, string Name, string Email, IReadOnlyCollection<string> Roles);

public interface IIdentityService
{
    Task<AuthResponse> RegisterStudentAsync(RegisterRequest request, CancellationToken cancellationToken);
    Task<AuthResponse> LoginAsync(LoginRequest request, CancellationToken cancellationToken);
}
