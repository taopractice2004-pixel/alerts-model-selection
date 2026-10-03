using System.ComponentModel.DataAnnotations;
using AlertService.DTO.Requests;

namespace AlertService.API.Tests.Requests;

public class AlertTrendsQueryRequestTests
{
    private static List<ValidationResult> Validate(AlertTrendsQueryRequest request)
    {
        var results = new List<ValidationResult>();
        Validator.TryValidateObject(request, new ValidationContext(request), results, validateAllProperties: true);
        return results;
    }

    [Fact]
    public void Default_UsesSevenDays_AndIsValid()
    {
        var request = new AlertTrendsQueryRequest();

        Assert.Equal(7, request.Days);
        Assert.Empty(Validate(request));
    }

    [Theory]
    [InlineData(1)]
    [InlineData(90)]
    public void Days_AtBoundaries_IsValid(int days)
    {
        Assert.Empty(Validate(new AlertTrendsQueryRequest { Days = days }));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(91)]
    public void Days_OutOfRange_FailsValidation(int days)
    {
        var results = Validate(new AlertTrendsQueryRequest { Days = days });

        Assert.Contains(results, r => r.MemberNames.Contains(nameof(AlertTrendsQueryRequest.Days)));
    }
}
