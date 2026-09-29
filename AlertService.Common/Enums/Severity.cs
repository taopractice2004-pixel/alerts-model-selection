namespace AlertService.Common.Enums;

/// <summary>
/// Severity level of an alert. Shared by the domain model and the DTOs.
/// </summary>
public enum Severity
{
    Low = 1,
    Medium = 2,
    High = 3,
    Critical = 4
}
