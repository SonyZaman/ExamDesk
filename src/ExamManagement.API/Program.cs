using ExamManagement.API.Common;
using ExamManagement.Application.Auth;
using ExamManagement.Application.Batches;
using ExamManagement.Application.Common.Abstractions;
using ExamManagement.Infrastructure;
using ExamManagement.Infrastructure.Persistence;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews();
builder.Services.AddOpenApi();
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<ApiExceptionHandler>();
builder.Services.AddHttpContextAccessor();

builder.Services.AddScoped<IBatchService, BatchService>();
builder.Services.AddScoped<ICurrentUser, HttpCurrentUser>();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddAuthentication(options =>
{
    options.DefaultScheme = "WebOrApi";
    options.DefaultChallengeScheme = "WebOrApi";
}).AddPolicyScheme("WebOrApi", null, options => options.ForwardDefaultSelector = context =>
    context.Request.Path.StartsWithSegments("/api") ? "Bearer" : Microsoft.AspNetCore.Identity.IdentityConstants.ApplicationScheme)
 .AddCookie(Microsoft.AspNetCore.Identity.IdentityConstants.ApplicationScheme, options =>
 {
     options.LoginPath = "/Account/Login";
     options.AccessDeniedPath = "/Account/Denied";
     options.Cookie.HttpOnly = true;
     options.Cookie.SameSite = SameSiteMode.Lax;
 });

var app = builder.Build();

app.UseExceptionHandler();

if (app.Environment.IsDevelopment())
    app.MapOpenApi();

app.UseAuthentication();
app.UseStaticFiles();
app.UseAuthorization();
app.MapControllers();
app.MapControllerRoute("default", "{controller=Portal}/{action=Index}/{id?}");

await app.Services.InitializeDatabaseAsync();
await app.RunAsync();

public partial class Program;
