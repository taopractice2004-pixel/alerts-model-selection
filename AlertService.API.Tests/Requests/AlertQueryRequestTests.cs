using System.ComponentModel.DataAnnotations;
using AlertService.DTO.Requests;

namespace AlertService.API.Tests.Requests;

public class AlertQueryRequestTests
{
    private static List<ValidationResult> Validate(AlertQueryRequest request)
    {
        var results = new List<ValidationResult>();
        Validator.TryValidateObject(request, new ValidationContext(request), results, validateAllProperties: true);
        return results;
    }

    [Fact]
    public void Tag_WithThirtyCharacters_Passes()
    {
        Assert.Empty(Validate(new AlertQueryRequest { Tag = new string('x', 30) }));
    }

    [Fact]
    public void Tag_WithThirtyOneCharacters_Fails()
    {
        var results = Validate(new AlertQueryRequest { Tag = new string('x', 31) });

        Assert.Contains(results, r => r.MemberNames.Contains(nameof(AlertQueryRequest.Tag)));
    }

    [Fact]
    public void Tag_WhenOmitted_Passes()
    {
        Assert.Empty(Validate(new AlertQueryRequest()));
    }
}
