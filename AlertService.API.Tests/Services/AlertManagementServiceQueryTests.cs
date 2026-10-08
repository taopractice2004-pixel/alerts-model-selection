using AlertService.Common.Enums;
using AlertService.Data.Interfaces;
using AlertService.DTO.Requests;
using AlertService.Models;
using Moq;

namespace AlertService.API.Tests.Services;

public partial class AlertManagementServiceTests
{
    [Fact]
    public async Task GetAllAsync_MapsEntitiesToPagedResponse()
    {
        _repository.Setup(r => r.GetAllAsync(
                It.Is<AlertQueryOptions>(options =>
                    options.IsActive == null &&
                    options.Severity == null &&
                    options.CreatedFrom == null &&
                    options.CreatedTo == null &&
                    options.Search == null &&
                    options.Tag == null &&
                    options.SortBy == "createdDate" &&
                    options.SortDirection == "desc" &&
                    options.Page == 1 &&
                    options.PageSize == 20),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((new List<Alert> { ExistingAlert(1), ExistingAlert(2) }, 2));

        var result = await _service.GetAllAsync(new AlertQueryRequest());

        Assert.Equal(2, result.Items.Count);
        Assert.Equal(new[] { 1, 2 }, result.Items.Select(r => r.Id));
        Assert.Equal(2, result.TotalCount);
        Assert.Equal(1, result.TotalPages);
    }

    [Fact]
    public async Task GetAllAsync_PassesQueryOptionsToRepository()
    {
        var request = new AlertQueryRequest
        {
            IsActive = true,
            Severity = Severity.Critical,
            CreatedFrom = new DateTime(2026, 8, 1, 0, 0, 0, DateTimeKind.Utc),
            CreatedTo = new DateTime(2026, 9, 1, 0, 0, 0, DateTimeKind.Utc),
            Search = "disk",
            Tag = "Ops",
            SortBy = "title",
            SortDirection = "asc",
            Page = 2,
            PageSize = 10
        };
        _repository.Setup(r => r.GetAllAsync(
                It.Is<AlertQueryOptions>(options =>
                    options.IsActive == true &&
                    options.Severity == Severity.Critical &&
                    options.CreatedFrom == request.CreatedFrom &&
                    options.CreatedTo == request.CreatedTo &&
                    options.Search == "disk" &&
                    options.Tag == "Ops" &&
                    options.SortBy == "title" &&
                    options.SortDirection == "asc" &&
                    options.Page == 2 &&
                    options.PageSize == 10),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((new List<Alert> { ExistingAlert(1) }, 11));

        var result = await _service.GetAllAsync(request);

        _repository.Verify(r => r.GetAllAsync(
            It.Is<AlertQueryOptions>(options =>
                options.IsActive == true &&
                options.Severity == Severity.Critical &&
                options.CreatedFrom == request.CreatedFrom &&
                options.CreatedTo == request.CreatedTo &&
                options.Search == "disk" &&
                options.Tag == "Ops" &&
                options.SortBy == "title" &&
                options.SortDirection == "asc" &&
                options.Page == 2 &&
                options.PageSize == 10),
            It.IsAny<CancellationToken>()), Times.Once);
        Assert.Equal(2, result.Page);
        Assert.Equal(10, result.PageSize);
        Assert.Equal(11, result.TotalCount);
        Assert.Equal(2, result.TotalPages);
    }

    [Fact]
    public async Task GetAllAsync_MapsTagsToResponse()
    {
        _repository.Setup(r => r.GetAllAsync(
                It.Is<AlertQueryOptions>(options =>
                    options.IsActive == null &&
                    options.Severity == null &&
                    options.CreatedFrom == null &&
                    options.CreatedTo == null &&
                    options.Search == null &&
                    options.Tag == null &&
                    options.SortBy == "createdDate" &&
                    options.SortDirection == "desc" &&
                    options.Page == 1 &&
                    options.PageSize == 20),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((new List<Alert> { ExistingAlertWithTags(1, "ops", "disk") }, 1));

        var result = await _service.GetAllAsync(new AlertQueryRequest());

        Assert.Single(result.Items);
        Assert.Equal(new[] { "disk", "ops" }, result.Items[0].Tags);
    }
}
