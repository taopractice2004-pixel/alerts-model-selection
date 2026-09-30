using AlertService.API.Services;
using AlertService.Common.Constants;
using AlertService.DTO.Requests;
using AlertService.DTO.Responses;
using Microsoft.AspNetCore.Mvc;

namespace AlertService.API.Controllers;

[ApiController]
[Route("api/alerts")]
[Produces("application/json")]
public class AlertsController : ControllerBase
{
    private const string DuplicateSuppressedHeader = "X-Duplicate-Suppressed";

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

    /// <summary>
    /// Gets per-day alert-creation counts with a per-severity breakdown over the last <c>days</c>
    /// UTC calendar days (default 7, min 1, max 90), oldest first.
    /// </summary>
    [HttpGet("trends")]
    [ProducesResponseType(typeof(AlertTrendResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<AlertTrendResponse>> GetTrends([FromQuery] AlertTrendQueryRequest request, CancellationToken cancellationToken)
    {
        var trends = await _alertService.GetTrendsAsync(request, cancellationToken);
        return Ok(trends);
    }

    /// <summary>
    /// Creates a new alert, or suppresses a near-duplicate. When an active alert with the same
    /// title and severity was created within the configured window, returns 200 OK with the
    /// existing alert and header <c>X-Duplicate-Suppressed: true</c> instead of creating a row.
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(AlertResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(AlertResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<AlertResponse>> Create([FromBody] CreateAlertRequest request, CancellationToken cancellationToken)
    {
        var result = await _alertService.CreateAsync(request, cancellationToken);
        if (result.Suppressed)
        {
            Response.Headers[DuplicateSuppressedHeader] = "true";
            return Ok(result.Alert);
        }

        return CreatedAtRoute(nameof(GetById), new { id = result.Alert.Id }, result.Alert);
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

    /// <summary>Deletes an alert.</summary>
    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        var deleted = await _alertService.DeleteAsync(id, cancellationToken);
        return deleted ? NoContent() : NotFound();
    }

    /// <summary>Adds one or more tags to an alert.</summary>
    [HttpPost("{id:int}/tags")]
    [ProducesResponseType(typeof(AlertResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<AlertResponse>> AddTags(int id, [FromBody] AddTagsRequest request, CancellationToken cancellationToken)
    {
        var result = await _alertService.AddTagsAsync(id, request, cancellationToken);
        return result.Status switch
        {
            AddTagsStatus.Success => Ok(result.Alert),
            AddTagsStatus.AlertNotFound => NotFound(),
            AddTagsStatus.TagLimitExceeded => ValidationProblem(new ValidationProblemDetails
            {
                Status = StatusCodes.Status400BadRequest,
                Detail = $"An alert can have at most {AlertConstants.MaxTagsPerAlert} tags."
            }),
            _ => throw new InvalidOperationException($"Unhandled add-tags status '{result.Status}'.")
        };
    }

    /// <summary>Removes a single tag from an alert.</summary>
    [HttpDelete("{id:int}/tags/{tag}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RemoveTag(int id, string tag, CancellationToken cancellationToken)
    {
        var result = await _alertService.RemoveTagAsync(id, tag, cancellationToken);
        return result switch
        {
            RemoveTagStatus.Removed => NoContent(),
            RemoveTagStatus.AlertNotFound => NotFound(),
            RemoveTagStatus.TagNotFound => NotFound(),
            _ => throw new InvalidOperationException($"Unhandled remove-tag status '{result}'.")
        };
    }
}
