using Microsoft.AspNetCore.Identity;

namespace ExamManagement.Infrastructure.Identity;

public sealed class ApplicationUser : IdentityUser
{
    public string Name { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
}
