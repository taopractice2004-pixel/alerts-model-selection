using System.ComponentModel.DataAnnotations;
using AlertService.DTO.Requests;

namespace AlertService.API.Tests.Requests;

public class AddAlertTagsRequestTests
{
    private static List<ValidationResult> Validate(AddAlertTagsRequest request)
    {
        var results = new List<ValidationResult>();
        Validator.TryValidateObject(request, new ValidationContext(request), results, validateAllProperties: true);
        return results;
    }

    [Theory]
    [InlineData("a")]
    [InlineData("  a  ")]
    public void Validate_WithOneCharacterTag_Passes(string tag)
    {
        Assert.Empty(Validate(new AddAlertTagsRequest { Tags = new List<string> { tag } }));
    }

    [Fact]
    public void Validate_WithThirtyCharacterTag_Passes()
    {
        Assert.Empty(Validate(new AddAlertTagsRequest { Tags = new List<string> { new('x', 30) } }));
    }

    [Fact]
    public void Validate_WithThirtyOneCharacterTag_Fails()
    {
        var results = Validate(new AddAlertTagsRequest { Tags = new List<string> { new('x', 31) } });

        Assert.Contains(results, r => r.MemberNames.Contains(nameof(AddAlertTagsRequest.Tags)));
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("\t")]
    public void Validate_WithEmptyOrWhitespaceTag_Fails(string tag)
    {
        var results = Validate(new AddAlertTagsRequest { Tags = new List<string> { "ok", tag } });

        Assert.Contains(results, r => r.MemberNames.Contains(nameof(AddAlertTagsRequest.Tags)));
    }

    [Fact]
    public void Validate_WithNullItem_Fails()
    {
        var results = Validate(new AddAlertTagsRequest { Tags = new List<string> { null! } });

        Assert.Contains(results, r => r.MemberNames.Contains(nameof(AddAlertTagsRequest.Tags)));
    }

    [Fact]
    public void Validate_WithEmptyList_Fails()
    {
        var results = Validate(new AddAlertTagsRequest { Tags = new List<string>() });

        Assert.Contains(results, r => r.MemberNames.Contains(nameof(AddAlertTagsRequest.Tags)));
    }

    [Fact]
    public void Validate_WithNullList_Fails()
    {
        var results = Validate(new AddAlertTagsRequest { Tags = null! });

        Assert.Contains(results, r => r.MemberNames.Contains(nameof(AddAlertTagsRequest.Tags)));
    }
}
