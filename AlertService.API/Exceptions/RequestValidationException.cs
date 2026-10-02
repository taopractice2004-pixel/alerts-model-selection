namespace AlertService.API.Exceptions;

public sealed class RequestValidationException : Exception
{
    public RequestValidationException(IDictionary<string, string[]> errors)
        : base("One or more validation errors occurred.")
    {
        Errors = errors;
    }

    public IDictionary<string, string[]> Errors { get; }
}