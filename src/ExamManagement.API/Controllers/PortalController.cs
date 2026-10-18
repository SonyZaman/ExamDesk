using ExamManagement.API.Models;
using ExamManagement.Application.Batches;
using ExamManagement.Domain.Entities;
using ExamManagement.Domain.Enums;
using ExamManagement.Infrastructure.Identity;
using ExamManagement.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
namespace ExamManagement.API.Controllers;
[Authorize, AutoValidateAntiforgeryToken]
public sealed partial class PortalController(ExamManagementDbContext db, UserManager<ApplicationUser> users, IBatchService batches, IWebHostEnvironment env) : Controller
{
    private string Uid => users.GetUserId(User)!;
    private bool Own(ExamBatch batch) => User.IsInRole("Admin") || (User.IsInRole("Instructor") && batch.InstructorId == Uid);
    public async Task<IActionResult> Index()
    {
        var all = await db.ExamBatches.Include(x => x.Enrollments).Include(x => x.Exams).ToListAsync();
        var visible = all.Where(x => Own(x) || x.Enrollments.Any(e => e.StudentId == Uid) || x.Status == BatchStatus.Open).ToList();
        var exams = visible.Where(x => Own(x) || x.Enrollments.Any(e => e.StudentId == Uid)).SelectMany(x => x.Exams).OrderBy(x => x.QuestionReleaseAtUtc).ToList();
        return View(new DashboardModel(visible, exams, await db.ExamResults.Where(x => x.StudentId == Uid).ToListAsync()));
    }
    [HttpPost, Authorize(Roles = "Admin,Instructor")]
    public async Task<IActionResult> CreateBatch(string name, string? description)
    {
        if (string.IsNullOrWhiteSpace(name) || name.Length > 200) { TempData["Error"] = "Enter a batch name of up to 200 characters."; return RedirectToAction("Index"); }
        await batches.CreateAsync(new(name, description, null, true), HttpContext.RequestAborted);
        TempData["Success"] = "Batch created and enrollment opened.";
        return RedirectToAction("Index");
    }
    [HttpPost, Authorize(Roles = "Student")]
    public async Task<IActionResult> Enroll(Guid id)
    {
        try { await batches.EnrollAsync(id, HttpContext.RequestAborted); TempData["Success"] = "You are enrolled. Your exams now appear below."; }
        catch (ExamManagement.Application.Common.ConflictException ex) { TempData["Error"] = ex.Message; }
        catch (DbUpdateException) { TempData["Error"] = "Enrollment already exists. Refresh the page."; }
        return RedirectToAction("Index");
    }
    [Authorize(Roles = "Admin,Instructor")]
    public async Task<IActionResult> CreateExam(Guid id)
    {
        var batch = await db.ExamBatches.FindAsync(id);
        return batch is null ? NotFound() : !Own(batch) ? Forbid() : View(new ExamModel { BatchId = id });
    }
    [HttpPost, Authorize(Roles = "Admin,Instructor")]
    public async Task<IActionResult> CreateExam(ExamModel model)
    {
        var batch = await db.ExamBatches.FindAsync(model.BatchId);
        if (batch is null) return NotFound();
        if (!Own(batch)) return Forbid();
        if (ModelState.IsValid)
        {
            try
            {
                DateTimeOffset Utc(DateTime value) => new DateTimeOffset(DateTime.SpecifyKind(value, DateTimeKind.Unspecified), TimeSpan.FromHours(6)).ToUniversalTime();
                var exam = new Exam(batch.Id, model.Title, model.FullMarks, Utc(model.Release), Utc(model.End), Utc(model.Deadline));
                db.Exams.Add(exam); await db.SaveChangesAsync(); return RedirectToAction("Exam", new { id = exam.Id });
            }
            catch (ExamManagement.Domain.Common.DomainException ex) { ModelState.AddModelError("", ex.Message); }
        }
        return View(model);
    }
    private Task<Exam?> Load(Guid id) => db.Exams.Include(x => x.Batch).ThenInclude(x => x.Enrollments).Include(x => x.QuestionFiles).FirstOrDefaultAsync(x => x.Id == id);
    private bool Enrolled(Exam exam) => User.IsInRole("Student") && exam.Batch.Enrollments.Any(x => x.StudentId == Uid && x.Status == EnrollmentStatus.Active);
    public async Task<IActionResult> Exam(Guid id)
    {
        var exam = await Load(id);
        if (exam is null) return NotFound();
        if (!Own(exam.Batch) && !Enrolled(exam)) return Forbid();
        var canManage = Own(exam.Batch);
        var submissions = await db.ExamSubmissions.Include(x => x.Files).Include(x => x.Evaluation).Where(x => x.ExamId == id && (canManage || x.StudentId == Uid)).ToListAsync();
        var results = await db.ExamResults.Where(x => x.ExamId == id).ToListAsync();
        var names = await users.Users.ToDictionaryAsync(x => x.Id, x => x.Name);
        return View(new ExamPage(exam, Own(exam.Batch), Enrolled(exam), submissions.FirstOrDefault(x => x.StudentId == Uid), submissions, results, names));
    }
    private string Storage => Path.Combine(env.ContentRootPath, "App_Data", "uploads");
    private async Task<string> Store(IFormFile file)
    {
        Directory.CreateDirectory(Storage); var key = Guid.NewGuid().ToString("N");
        await using var stream = System.IO.File.Create(Path.Combine(Storage, key)); await file.CopyToAsync(stream); return key;
    }
    private static async Task<bool> Valid(IFormFile file, bool pdf)
    {
        if (file.Length is <= 0 or > 10 * 1024 * 1024) return false;
        await using var stream = file.OpenReadStream(); var bytes = new byte[8]; var count = await stream.ReadAsync(bytes);
        return pdf ? count >= 5 && bytes.AsSpan(0,5).SequenceEqual("%PDF-"u8) :
            (count >= 3 && bytes[0] == 255 && bytes[1] == 216 && bytes[2] == 255) || (count == 8 && bytes.AsSpan().SequenceEqual(new byte[] {137,80,78,71,13,10,26,10}));
    }
    [HttpPost, Authorize(Roles = "Admin,Instructor"), RequestSizeLimit(11000000)]
    public async Task<IActionResult> Question(Guid id, IFormFile? file)
    {
        var exam = await Load(id); if (exam is null) return NotFound(); if (!Own(exam.Batch)) return Forbid();
        if (file is null || !await Valid(file, true)) TempData["Error"] = "Select a PDF up to 10 MB.";
        else { db.ExamQuestionFiles.Add(new(id, await Store(file), Path.GetFileName(file.FileName), DateTimeOffset.UtcNow)); await db.SaveChangesAsync(); TempData["Success"] = "Question PDF uploaded."; }
        return RedirectToAction("Exam", new { id });
    }
    public async Task<IActionResult> DownloadQuestion(Guid id)
    {
        var exam = await Load(id); if (exam is null) return NotFound();
        if (!Own(exam.Batch) && !(Enrolled(exam) && exam.CanViewQuestion(DateTimeOffset.UtcNow) && DateTimeOffset.UtcNow <= exam.ExamEndAtUtc)) return Forbid();
        var file = exam.QuestionFiles.OrderByDescending(x => x.UploadedAtUtc).FirstOrDefault(); if (file is null) return NotFound();
        Response.Headers.CacheControl = "no-store";
        return PhysicalFile(Path.Combine(Storage, file.StorageKey), "application/pdf", "questions.pdf");
    }
    [HttpPost, Authorize(Roles = "Student"), RequestSizeLimit(55000000)]
    public async Task<IActionResult> Submit(Guid id, List<IFormFile> files)
    {
        var exam = await Load(id); if (exam is null) return NotFound(); if (!Enrolled(exam)) return Forbid();
        if (!exam.CanSubmit(DateTimeOffset.UtcNow) || await db.ExamSubmissions.AnyAsync(x => x.ExamId == id && x.StudentId == Uid)) TempData["Error"] = "Submission is closed or you have already submitted.";
        else if (files.Count is < 1 or > 5) TempData["Error"] = "Select between 1 and 5 JPG or PNG images.";
        else
        {
            foreach (var file in files) if (!await Valid(file, false)) { TempData["Error"] = "Each image must be JPG/PNG and at most 10 MB."; return RedirectToAction("Exam", new { id }); }
            var submission = new ExamSubmission(id, Uid, DateTimeOffset.UtcNow);
            foreach (var file in files) submission.Files.Add(new SubmissionFile(submission.Id, await Store(file), Path.GetFileName(file.FileName), submission.Files.Count + 1));
            if (!exam.CanSubmit(DateTimeOffset.UtcNow)) { TempData["Error"] = "Upload completed after the deadline."; return RedirectToAction("Exam", new { id }); }
            submission.Finalize(DateTimeOffset.UtcNow, exam.SubmissionDeadlineAtUtc); db.ExamSubmissions.Add(submission);
            try { await db.SaveChangesAsync(); TempData["Success"] = "Answer sheets submitted successfully."; }
            catch (DbUpdateException) { TempData["Error"] = "A submission already exists."; }
        }
        return RedirectToAction("Exam", new { id });
    }
    public async Task<IActionResult> Answer(Guid id)
    {
        var file = await db.SubmissionFiles.Include(x => x.Submission).ThenInclude(x => x.Exam).ThenInclude(x => x.Batch).FirstOrDefaultAsync(x => x.Id == id);
        if (file is null) return NotFound(); if (file.Submission.StudentId != Uid && !Own(file.Submission.Exam.Batch)) return Forbid();
        Response.Headers.CacheControl = "no-store";
        var path = Path.Combine(Storage, file.StorageKey); await using var stream = System.IO.File.OpenRead(path); var signature = stream.ReadByte();
        return PhysicalFile(path, signature == 137 ? "image/png" : "image/jpeg");
    }
    [HttpPost, Authorize(Roles = "Admin,Instructor")]
    public async Task<IActionResult> Grade(Guid id, decimal marks, string? feedback)
    {
        var submission = await db.ExamSubmissions.Include(x => x.Exam).ThenInclude(x => x.Batch).Include(x => x.Evaluation).FirstOrDefaultAsync(x => x.Id == id);
        if (submission is null) return NotFound(); if (!Own(submission.Exam.Batch)) return Forbid();
        if (!ModelState.IsValid || marks < 0 || marks > submission.Exam.FullMarks || submission.Exam.ResultPublishedAtUtc != null || submission.Evaluation?.Status == EvaluationStatus.Finalized)
            TempData["Error"] = "Enter valid marks. Finalized grades cannot be changed.";
        else
        {
            var evaluation = submission.Evaluation ?? new Evaluation(id, Uid); evaluation.Save(marks, submission.Exam.FullMarks, feedback);
            if (submission.Evaluation is null) db.Evaluations.Add(evaluation);
            await db.SaveChangesAsync(); TempData["Success"] = "Evaluation saved as a draft.";
        }
        return RedirectToAction("Exam", new { id = submission.ExamId });
    }
    [HttpPost, Authorize(Roles = "Admin,Instructor")]
    public async Task<IActionResult> Publish(Guid id)
    {
        var exam = await Load(id); if (exam is null) return NotFound(); if (!Own(exam.Batch)) return Forbid();
        var submissions = await db.ExamSubmissions.Include(x => x.Evaluation).Where(x => x.ExamId == id).ToListAsync();
        if (exam.ResultPublishedAtUtc != null || DateTimeOffset.UtcNow <= exam.SubmissionDeadlineAtUtc || submissions.Any(x => x.Evaluation is null))
            TempData["Error"] = "Wait until submissions close and grade every submission before publishing. Published results are locked.";
        else
        {
            var now = DateTimeOffset.UtcNow;
            foreach (var submission in submissions) submission.Evaluation!.Finalize(now);
            foreach (var enrollment in exam.Batch.Enrollments.Where(x => x.Status == EnrollmentStatus.Active))
                db.ExamResults.Add(new(id, enrollment.StudentId, submissions.FirstOrDefault(x => x.StudentId == enrollment.StudentId)?.Evaluation?.Marks ?? 0, now));
            exam.PublishResults(now);
            await db.SaveChangesAsync(); TempData["Success"] = "Results published. Missing submissions count as zero.";
        }
        return RedirectToAction("Exam", new { id });
    }
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Users() => View(await users.Users.ToListAsync());
    public async Task<IActionResult> Leaderboard(Guid id)
    {
        var batch = await db.ExamBatches.Include(x => x.Exams).Include(x => x.Enrollments).FirstOrDefaultAsync(x => x.Id == id);
        if (batch is null) return NotFound();
        if (!Own(batch) && !batch.Enrollments.Any(x => x.StudentId == Uid && x.Status == EnrollmentStatus.Active)) return Forbid();
        ViewBag.Batch = batch;
        ViewBag.Names = await users.Users.ToDictionaryAsync(x => x.Id, x => x.Name);
        var ids = batch.Exams.Select(x => x.Id).ToList();
        return View(await db.ExamResults.Where(x => ids.Contains(x.ExamId)).ToListAsync());
    }
    [HttpPost, Authorize(Roles = "Admin")]
    public async Task<IActionResult> SetRole(string id, string role)
    {
        if (id == Uid || !new[] { "Admin", "Instructor", "Student" }.Contains(role)) return BadRequest();
        var user = await users.FindByIdAsync(id); if (user is null) return NotFound();
        var current = await users.GetRolesAsync(user);
        await users.RemoveFromRolesAsync(user, current); await users.AddToRoleAsync(user, role); await users.UpdateSecurityStampAsync(user);
        TempData["Success"] = "Role updated. The user should sign in again."; return RedirectToAction("Users");
    }
}
