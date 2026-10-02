namespace AlertService.API.Tests.TestInfrastructure;

// Program.cs sets a static Serilog logger, so web hosts must not start in parallel.
[CollectionDefinition(Name)]
public sealed class WebHostCollection
{
    public const string Name = "WebHost";
}
