using System.ComponentModel.DataAnnotations;
using AlertService.API.Configuration;
using AlertService.API.Tests.TestInfrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace AlertService.API.Tests.Configuration;

// Same collection as HealthChecksTests: Program sets a process-global Serilog logger, so hosts must not boot in parallel.
[Collection("Program host")]
public class AlertSuppressionOptionsTests
{
    [Fact]
    public void AppSettings_ProvidesDefaultWindowOf15Minutes()
    {
        using var factory = new HealthChecksWebApplicationFactory(sqlReachable: true);

        var options = factory.Services.GetRequiredService<IOptions<AlertSuppressionOptions>>().Value;

        Assert.Equal(15, options.DuplicateWindowMinutes);
    }

    [Theory]
    [InlineData(0, false)]
    [InlineData(-1, false)]
    [InlineData(1, true)]
    [InlineData(1440, true)]
    [InlineData(1441, false)]
    public void DuplicateWindowMinutes_ValidatesRange(int minutes, bool expectedValid)
    {
        var options = new AlertSuppressionOptions { DuplicateWindowMinutes = minutes };

        var valid = Validator.TryValidateObject(options, new ValidationContext(options), new List<ValidationResult>(), validateAllProperties: true);

        Assert.Equal(expectedValid, valid);
    }
}
