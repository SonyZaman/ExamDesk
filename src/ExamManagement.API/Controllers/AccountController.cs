using ExamManagement.API.Models;
using ExamManagement.Infrastructure.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
namespace ExamManagement.API.Controllers;
[AutoValidateAntiforgeryToken]
public sealed class AccountController(UserManager<ApplicationUser> users, SignInManager<ApplicationUser> signIn) : Controller
{
    [HttpGet] public IActionResult Login() => View(new LoginModel());
    [HttpPost] public async Task<IActionResult> Login(LoginModel model)
    {
        if (ModelState.IsValid)
        {
            var user = await users.FindByEmailAsync(model.Email);
            if (user is { IsActive: true } && (await signIn.PasswordSignInAsync(user, model.Password, false, true)).Succeeded)
                return RedirectToAction("Index", "Portal");
            ModelState.AddModelError("", "Email or password is incorrect, or the account is locked.");
        }
        return View(model);
    }
    [HttpGet] public IActionResult Register() => View(new RegisterModel());
    [HttpPost] public async Task<IActionResult> Register(RegisterModel model)
    {
        if (ModelState.IsValid)
        {
            var user = new ApplicationUser { Name = model.Name.Trim(), Email = model.Email.Trim(), UserName = model.Email.Trim() };
            var result = await users.CreateAsync(user, model.Password);
            if (result.Succeeded)
            {
                var role = await users.AddToRoleAsync(user, "Student");
                if (!role.Succeeded) throw new InvalidOperationException("Unable to assign student role.");
                await signIn.SignInAsync(user, false);
                return RedirectToAction("Index", "Portal");
            }
            foreach (var error in result.Errors) ModelState.AddModelError("", error.Description);
        }
        return View(model);
    }
    [Authorize, HttpPost] public async Task<IActionResult> Logout() { await signIn.SignOutAsync(); return RedirectToAction("Login"); }
    public IActionResult Denied() => View();
}
