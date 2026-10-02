using Xunit;

namespace AlertService.API.Tests.TestInfrastructure;

[CollectionDefinition(Name, DisableParallelization = true)]
public sealed class WebApplicationFactoryCollection
{
    public const string Name = "WebApplicationFactory";
}