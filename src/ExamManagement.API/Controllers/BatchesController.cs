using ExamManagement.Application.Batches;
using ExamManagement.Application.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ExamManagement.API.Controllers;

[ApiController]
[Authorize]
[Route("api/batches")]
public sealed class BatchesController(IBatchService batchService) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType<IReadOnlyList<BatchDto>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<BatchDto>>> List(CancellationToken cancellationToken) =>
        Ok(await batchService.ListAsync(cancellationToken));

    [HttpPost]
    [Authorize(Roles = $"{AppRoles.Instructor},{AppRoles.Admin}")]
    [ProducesResponseType<BatchDto>(StatusCodes.Status201Created)]
    public async Task<ActionResult<BatchDto>> Create(
        CreateBatchRequest request,
        CancellationToken cancellationToken)
    {
        var batch = await batchService.CreateAsync(request, cancellationToken);
        return Created($"/api/batches/{batch.Id}", batch);
    }

    [HttpPost("{batchId:guid}/enroll")]
    [Authorize(Roles = AppRoles.Student)]
    [ProducesResponseType<EnrollmentDto>(StatusCodes.Status201Created)]
    public async Task<ActionResult<EnrollmentDto>> Enroll(
        Guid batchId,
        CancellationToken cancellationToken)
    {
        var enrollment = await batchService.EnrollAsync(batchId, cancellationToken);
        return Created($"/api/batches/{batchId}/enrollments/{enrollment.Id}", enrollment);
    }
}
