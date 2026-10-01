namespace AlertService.API.Services;

/// <summary>
/// Raised when a business rule that cannot be expressed via model validation is violated
/// (for example, exceeding the maximum number of tags per alert). The exception message is a
/// safe, user-facing description and must not contain internal details.
/// </summary>
public class AlertValidationException : Exception
{
    public AlertValidationException(string message)
        : base(message)
    {
    }
}
