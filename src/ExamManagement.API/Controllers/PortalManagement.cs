using ExamManagement.API.Models;
using ExamManagement.Domain.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
namespace ExamManagement.API.Controllers;
public sealed partial class PortalController
{
    private static DateTime Local(DateTimeOffset value) => value.ToOffset(TimeSpan.FromHours(6)).DateTime;
    private static DateTimeOffset Utc(DateTime value) => new DateTimeOffset(DateTime.SpecifyKind(value, DateTimeKind.Unspecified), TimeSpan.FromHours(6)).ToUniversalTime();
    [HttpGet, Authorize(Roles = "Admin,Instructor")]
    public async Task<IActionResult> EditBatch(Guid id)
    {
        var batch = await db.ExamBatches.FindAsync(id);
        if (batch is null) return NotFound(); if (!Own(batch)) return Forbid();
        return View(new EditBatchModel { Name = batch.Name, Description = batch.Description, Status = batch.Status, EnrollmentDeadline = batch.EnrollmentDeadlineUtc is {} deadline ? Local(deadline) : null });
    }
    [HttpPost, Authorize(Roles = "Admin,Instructor")]
    public async Task<IActionResult> EditBatch(Guid id, EditBatchModel model)
    {
        var batch = await db.ExamBatches.FindAsync(id);
        if (batch is null) return NotFound(); if (!Own(batch)) return Forbid();
        if (ModelState.IsValid)
        {
            try { batch.Edit(model.Name, model.Description, model.EnrollmentDeadline is {} deadline ? Utc(deadline) : null, model.Status); await db.SaveChangesAsync(); TempData["Success"] = "Batch updated."; return RedirectToAction("Index"); }
            catch (DomainException ex) { ModelState.AddModelError("", ex.Message); }
        }
        return View(model);
    }
    [HttpGet, Authorize(Roles = "Admin,Instructor")]
    public async Task<IActionResult> EditExam(Guid id)
    {
        var exam = await Load(id); if (exam is null) return NotFound(); if (!Own(exam.Batch)) return Forbid();
        return View(new ExamModel { BatchId = exam.BatchId, Title = exam.Title, FullMarks = exam.FullMarks, Release = Local(exam.QuestionReleaseAtUtc), End = Local(exam.ExamEndAtUtc), Deadline = Local(exam.SubmissionDeadlineAtUtc) });
    }
    [HttpPost, Authorize(Roles = "Admin,Instructor")]
    public async Task<IActionResult> EditExam(Guid id, ExamModel model)
    {
        var exam = await Load(id); if (exam is null) return NotFound(); if (!Own(exam.Batch)) return Forbid();
        var marks = await db.Evaluations.Where(x => x.Submission.ExamId == id).Select(x => x.Marks).ToListAsync();
        marks.AddRange(await db.ExamResults.Where(x => x.ExamId == id).Select(x => x.Marks).ToListAsync());
        if (marks.Any(x => x > model.FullMarks)) ModelState.AddModelError("FullMarks", "Full marks cannot be lower than an existing grade.");
        if (ModelState.IsValid)
        {
            try { exam.Edit(model.Title, model.FullMarks, Utc(model.Release), Utc(model.End), Utc(model.Deadline)); await db.SaveChangesAsync(); TempData["Success"] = "Exam updated. Existing submissions and published marks were preserved."; return RedirectToAction("Exam", new { id }); }
            catch (DomainException ex) { ModelState.AddModelError("", ex.Message); }
        }
        return View(model);
    }
    [HttpGet, Authorize(Roles = "Admin,Instructor")]
    public async Task<IActionResult> DeleteExam(Guid id)
    {
        var exam = await Load(id); if (exam is null) return NotFound(); if (!Own(exam.Batch)) return Forbid();
        ViewBag.Kind = "exam"; ViewBag.Name = exam.Title; return View("ConfirmDelete");
    }
    [HttpGet, Authorize(Roles = "Admin,Instructor")]
    public async Task<IActionResult> DeleteBatch(Guid id)
    {
        var batch = await db.ExamBatches.FindAsync(id); if (batch is null) return NotFound(); if (!Own(batch)) return Forbid();
        ViewBag.Kind = "batch"; ViewBag.Name = batch.Name; return View("ConfirmDelete");
    }
    private async Task RemoveExams(List<Guid> ids)
    {
        db.ExamResults.RemoveRange(await db.ExamResults.Where(x => ids.Contains(x.ExamId)).ToListAsync());
        db.Evaluations.RemoveRange(await db.Evaluations.Where(x => ids.Contains(x.Submission.ExamId)).ToListAsync());
        db.SubmissionFiles.RemoveRange(await db.SubmissionFiles.Where(x => ids.Contains(x.Submission.ExamId)).ToListAsync());
        db.ExamSubmissions.RemoveRange(await db.ExamSubmissions.Where(x => ids.Contains(x.ExamId)).ToListAsync());
        db.ExamQuestionFiles.RemoveRange(await db.ExamQuestionFiles.Where(x => ids.Contains(x.ExamId)).ToListAsync());
        db.Exams.RemoveRange(await db.Exams.Where(x => ids.Contains(x.Id)).ToListAsync());
    }
    [HttpPost, ActionName("DeleteExam"), Authorize(Roles = "Admin,Instructor")]
    public async Task<IActionResult> DeleteExamConfirmed(Guid id)
    {
        var exam = await Load(id); if (exam is null) return NotFound(); if (!Own(exam.Batch)) return Forbid();
        await using var transaction = await db.Database.BeginTransactionAsync();
        await RemoveExams([id]); await db.SaveChangesAsync(); await transaction.CommitAsync();
        TempData["Success"] = "Exam and its submissions, evaluations, and results deleted."; return RedirectToAction("Index");
    }
    [HttpPost, ActionName("DeleteBatch"), Authorize(Roles = "Admin,Instructor")]
    public async Task<IActionResult> DeleteBatchConfirmed(Guid id)
    {
        var batch = await db.ExamBatches.FindAsync(id); if (batch is null) return NotFound(); if (!Own(batch)) return Forbid();
        await using var transaction = await db.Database.BeginTransactionAsync();
        await RemoveExams(await db.Exams.Where(x => x.BatchId == id).Select(x => x.Id).ToListAsync());
        db.BatchEnrollments.RemoveRange(await db.BatchEnrollments.Where(x => x.BatchId == id).ToListAsync());
        db.ExamBatches.Remove(batch); await db.SaveChangesAsync(); await transaction.CommitAsync();
        TempData["Success"] = "Batch and all associated exams, enrollments, submissions, and results deleted."; return RedirectToAction("Index");
    }
}
