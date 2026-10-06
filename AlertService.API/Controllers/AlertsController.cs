using AlertService.API.Services;
using AlertService.DTO.Requests;
using AlertService.DTO.Responses;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;

namespace AlertService.API.Controllers;

[ApiController]
[Route("api/alerts")]
[Produces("application/json")]
public class AlertsController : ControllerBase
{
    private readonly IAlertService _alertService;

    public AlertsController(IAlertService alertService)
    {
        _alertService = alertService;
    }

    /// <summary>Gets alerts with optional filtering, paging, sorting, and title search.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(PagedResponse<AlertResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<PagedResponse<AlertResponse>>> GetAll([FromQuery] AlertQueryRequest request, CancellationToken cancellationToken)
    {
        var alerts = await _alertService.GetAllAsync(request, cancellationToken);
        return Ok(alerts);
    }

    /// <summary>Gets a single alert by id.</summary>
    [HttpGet("{id:int}", Name = nameof(GetById))]
    [ProducesResponseType(typeof(AlertResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<AlertResponse>> GetById(int id, CancellationToken cancellationToken)
    {
        var alert = await _alertService.GetByIdAsync(id, cancellationToken);
        return alert is null ? NotFound() : Ok(alert);
    }

    /// <summary>Gets aggregate alert counts by status and severity.</summary>
    [HttpGet("summary")]
    [ProducesResponseType(typeof(AlertSummaryResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<AlertSummaryResponse>> GetSummary(CancellationToken cancellationToken)
    {
        var summary = await _alertService.GetSummaryAsync(cancellationToken);
        return Ok(summary);
    }

    /// <summary>Gets daily UTC alert volume trends for the last N days.</summary>
    [HttpGet("trends")]
    [ProducesResponseType(typeof(IReadOnlyList<AlertTrendResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<IReadOnlyList<AlertTrendResponse>>> GetTrends(
        [FromQuery]
        [Range(1, 90)]
        int? days,
        CancellationToken cancellationToken)
    {
        var numberOfDays = days ?? 7;
        var trends = await _alertService.GetTrendsAsync(numberOfDays, cancellationToken);
        return Ok(trends);
    }

    /// <summary>Creates a new alert.</summary>
    [HttpPost]
    [ProducesResponseType(typeof(AlertResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(AlertResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<AlertResponse>> Create([FromBody] CreateAlertRequest request, CancellationToken cancellationToken)
    {
        var created = await _alertService.CreateAsync(request, cancellationToken);

        if (created.IsDuplicateSuppressed)
        {
            Response.Headers.Append("X-Duplicate-Suppressed", "true");
            return Ok(created);
        }

        return CreatedAtRoute(nameof(GetById), new { id = created.Id }, created);
    }

    /// <summary>Updates an existing alert.</summary>
    [HttpPut("{id:int}")]
    [ProducesResponseType(typeof(AlertResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<AlertResponse>> Update(int id, [FromBody] UpdateAlertRequest request, CancellationToken cancellationToken)
    {
        var updated = await _alertService.UpdateAsync(id, request, cancellationToken);
        return updated is null ? NotFound() : Ok(updated);
    }

    /// <summary>Deactivates an existing alert.</summary>
    [HttpPatch("{id:int}/deactivate")]
    [ProducesResponseType(typeof(AlertResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<AlertResponse>> Deactivate(int id, CancellationToken cancellationToken)
    {
        var deactivated = await _alertService.DeactivateAsync(id, cancellationToken);
        return deactivated is null ? NotFound() : Ok(deactivated);
    }

    /// <summary>Adds one or more tags to an existing alert.</summary>
    [HttpPost("{id:int}/tags")]
    [ProducesResponseType(typeof(AlertResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<AlertResponse>> AddTags(int id, [FromBody] AddAlertTagsRequest request, CancellationToken cancellationToken)
    {
        var (alert, validationError) = await _alertService.AddTagsAsync(id, request.Tags, cancellationToken);
        if (!string.IsNullOrWhiteSpace(validationError))
        {
            ModelState.AddModelError(nameof(request.Tags), validationError);
            return ValidationProblem(ModelState);
        }

        return alert is null ? NotFound() : Ok(alert);
    }

    /// <summary>Removes a tag assignment from an existing alert.</summary>
    [HttpDelete("{id:int}/tags/{tag}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RemoveTag(int id, string tag, CancellationToken cancellationToken)
    {
        var removed = await _alertService.RemoveTagAsync(id, tag, cancellationToken);
        return removed ? NoContent() : NotFound();
    }

    /// <summary>Deletes an alert.</summary>
    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        var deleted = await _alertService.DeleteAsync(id, cancellationToken);
        return deleted ? NoContent() : NotFound();
    }
}
