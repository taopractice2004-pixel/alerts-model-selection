namespace AlertService.API.Services;

/// <summary>
/// Thrown when adding tags to an alert would exceed <see cref="AlertService.Common.Constants.AlertConstants.MaxTagsPerAlert"/>.
/// </summary>
public sealed class TagLimitExceededException : Exception
{
    public TagLimitExceededException(string message) : base(message)
    {
    }
}
