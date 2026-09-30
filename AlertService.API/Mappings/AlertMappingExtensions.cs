using AlertService.DTO.Requests;
using AlertService.DTO.Responses;
using AlertService.Models;

namespace AlertService.API.Mappings;

/// <summary>
/// Manual entity/DTO mapping. Kept explicit instead of using AutoMapper for clarity.
/// </summary>
public static class AlertMappingExtensions
{
    public static AlertResponse ToResponse(this Alert alert) => new()
    {
        Id = alert.Id,
        Title = alert.Title,
        Description = alert.Description,
        Severity = alert.Severity,
        CreatedDate = alert.CreatedDate,
        IsActive = alert.IsActive,
        Tags = alert.AlertTags
            .Select(at => at.Tag.Name)
            .OrderBy(name => name, StringComparer.Ordinal)
            .ToList()
    };

    public static Alert ToEntity(this CreateAlertRequest request, DateTime createdDateUtc) => new()
    {
        Title = request.Title.Trim(),
        Description = request.Description?.Trim(),
        Severity = request.Severity,
        IsActive = request.IsActive,
        CreatedDate = createdDateUtc
    };

    public static void ApplyUpdate(this Alert alert, UpdateAlertRequest request)
    {
        alert.Title = request.Title.Trim();
        alert.Description = request.Description?.Trim();
        alert.Severity = request.Severity;
        alert.IsActive = request.IsActive;
    }
}
