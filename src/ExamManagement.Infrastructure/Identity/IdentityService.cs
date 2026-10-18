using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using ExamManagement.Application.Auth;
using ExamManagement.Application.Common;
using ExamManagement.Application.Common.Abstractions;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace ExamManagement.Infrastructure.Identity;

public sealed class IdentityService(
    UserManager<ApplicationUser> userManager,
    SignInManager<ApplicationUser> signInManager,
    IOptions<JwtOptions> jwtOptions,
    IClock clock) : IIdentityService
{
    public async Task<AuthResponse> RegisterStudentAsync(RegisterRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
            throw new ValidationException("Name is required.");
        if (string.IsNullOrWhiteSpace(request.Email))
            throw new ValidationException("Email is required.");
        if (request.Password.Length < 8)
            throw new ValidationException("Password must contain at least 8 characters.");

        var normalizedEmail = request.Email.Trim().ToLowerInvariant();
        if (await userManager.FindByEmailAsync(normalizedEmail) is not null)
            throw new ConflictException("An account with this email already exists.");

        var user = new ApplicationUser
        {
            Name = request.Name.Trim(),
            Email = normalizedEmail,
            UserName = normalizedEmail,
            EmailConfirmed = true
        };

        var result = await userManager.CreateAsync(user, request.Password);
        if (!result.Succeeded)
            throw new ValidationException(string.Join(" ", result.Errors.Select(x => x.Description)));

        await userManager.AddToRoleAsync(user, AppRoles.Student);
        return await CreateResponseAsync(user);
    }

    public async Task<AuthResponse> LoginAsync(LoginRequest request, CancellationToken cancellationToken)
    {
        var user = await userManager.FindByEmailAsync(request.Email.Trim());
        if (user is null || !user.IsActive)
            throw new ForbiddenException("Invalid email or password.");

        var result = await signInManager.CheckPasswordSignInAsync(user, request.Password, lockoutOnFailure: true);
        if (!result.Succeeded)
            throw new ForbiddenException("Invalid email or password.");

        return await CreateResponseAsync(user);
    }

    private async Task<AuthResponse> CreateResponseAsync(ApplicationUser user)
    {
        var options = jwtOptions.Value;
        var roles = await userManager.GetRolesAsync(user);
        var expiresAt = clock.UtcNow.AddMinutes(options.ExpirationMinutes);
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id),
            new(JwtRegisteredClaimNames.Email, user.Email ?? string.Empty),
            new(ClaimTypes.NameIdentifier, user.Id),
            new(ClaimTypes.Name, user.Name),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };
        claims.AddRange(roles.Select(role => new Claim(ClaimTypes.Role, role)));

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(options.SigningKey));
        var token = new JwtSecurityToken(
            options.Issuer,
            options.Audience,
            claims,
            expires: expiresAt.UtcDateTime,
            signingCredentials: new SigningCredentials(key, SecurityAlgorithms.HmacSha256));

        return new AuthResponse(
            new JwtSecurityTokenHandler().WriteToken(token),
            expiresAt,
            new UserDto(user.Id, user.Name, user.Email ?? string.Empty, roles.ToArray()));
    }
}
