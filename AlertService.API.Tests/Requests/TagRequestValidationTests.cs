using System.ComponentModel.DataAnnotations;
using AlertService.DTO.Requests;

namespace AlertService.API.Tests.Requests;

public class TagRequestValidationTests
{
    private static List<ValidationResult> Validate(object request)
    {
        var results = new List<ValidationResult>();
        Validator.TryValidateObject(request, new ValidationContext(request), results, validateAllProperties: true);
        return results;
    }

    [Fact]
    public void AddAlertTagsRequest_WithValidTags_IsValid()
    {
        var results = Validate(new AddAlertTagsRequest { Tags = new List<string> { "urgent", "network" } });

        Assert.Empty(results);
    }

    [Fact]
    public void AddAlertTagsRequest_WithNullList_FailsValidation()
    {
        var results = Validate(new AddAlertTagsRequest { Tags = null! });

        Assert.Contains(results, r => r.MemberNames.Contains(nameof(AddAlertTagsRequest.Tags)));
    }

    [Fact]
    public void AddAlertTagsRequest_WithEmptyList_FailsValidation()
    {
        var results = Validate(new AddAlertTagsRequest { Tags = new List<string>() });

        Assert.Contains(results, r => r.MemberNames.Contains(nameof(AddAlertTagsRequest.Tags)));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("   ")]
    [InlineData("\t")]
    public void AddAlertTagsRequest_WithNullEmptyOrWhitespaceElement_FailsValidation(string? tag)
    {
        var results = Validate(new AddAlertTagsRequest { Tags = new List<string> { "valid", tag! } });

        Assert.Contains(results, r => r.MemberNames.Contains(nameof(AddAlertTagsRequest.Tags)));
    }

    [Theory]
    [InlineData(1)]
    [InlineData(15)]
    [InlineData(30)]
    public void AddAlertTagsRequest_WithTagLengthWithinBounds_IsValid(int length)
    {
        var results = Validate(new AddAlertTagsRequest { Tags = new List<string> { new string('a', length) } });

        Assert.Empty(results);
    }

    [Fact]
    public void AddAlertTagsRequest_WithTagLongerThan30Characters_FailsValidation()
    {
        var results = Validate(new AddAlertTagsRequest { Tags = new List<string> { new string('a', 31) } });

        Assert.Contains(results, r => r.MemberNames.Contains(nameof(AddAlertTagsRequest.Tags)));
    }

    [Fact]
    public void AddAlertTagsRequest_UsesTrimmedLength_ForUpperBound()
    {
        var padded = "  " + new string('a', 30) + "  ";

        var results = Validate(new AddAlertTagsRequest { Tags = new List<string> { padded } });

        Assert.Empty(results);
    }

    [Fact]
    public void AddAlertTagsRequest_UsesTrimmedLength_ForTagThatIsTooLongAfterTrim()
    {
        var padded = "  " + new string('a', 31) + "  ";

        var results = Validate(new AddAlertTagsRequest { Tags = new List<string> { padded } });

        Assert.Contains(results, r => r.MemberNames.Contains(nameof(AddAlertTagsRequest.Tags)));
    }

    [Fact]
    public void AlertQueryRequest_WithNullTag_IsValid()
    {
        var results = Validate(new AlertQueryRequest { Tag = null });

        Assert.Empty(results);
    }

    [Fact]
    public void AlertQueryRequest_WithTagOf30Characters_IsValid()
    {
        var results = Validate(new AlertQueryRequest { Tag = new string('a', 30) });

        Assert.Empty(results);
    }

    [Fact]
    public void AlertQueryRequest_WithTagOf31Characters_FailsValidation()
    {
        var results = Validate(new AlertQueryRequest { Tag = new string('a', 31) });

        Assert.Contains(results, r => r.MemberNames.Contains(nameof(AlertQueryRequest.Tag)));
    }
}
