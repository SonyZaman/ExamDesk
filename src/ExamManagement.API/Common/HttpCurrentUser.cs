using System.Security.Claims;
using ExamManagement.Application.Common.Abstractions;

namespace ExamManagement.API.Common;

public sealed class HttpCurrentUser(IHttpContextAccessor accessor) : ICurrentUser
{
    private ClaimsPrincipal? Principal => accessor.HttpContext?.User;

    public string? UserId => Principal?.FindFirstValue(ClaimTypes.NameIdentifier);

    public bool IsInRole(string role) => Principal?.IsInRole(role) ?? false;
}
