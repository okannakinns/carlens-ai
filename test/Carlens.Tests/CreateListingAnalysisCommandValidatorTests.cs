using Carlens.Application.Features.Listings.Commands;

namespace Carlens.Tests;

public sealed class CreateListingAnalysisCommandValidatorTests
{
    private readonly CreateListingAnalysisCommandValidator _validator = new();

    [Fact]
    public async Task ValidateAsync_AcceptsArabamListingUrl()
    {
        var command = new CreateListingAnalysisCommand(
            "https://www.arabam.com/ilan/test-listing/123456");

        var result = await _validator.ValidateAsync(command);

        Assert.True(result.IsValid);
    }

    [Fact]
    public async Task ValidateAsync_RejectsUnsupportedHost()
    {
        var command = new CreateListingAnalysisCommand(
            "https://example.com/ilan/test-listing/123456");

        var result = await _validator.ValidateAsync(command);

        Assert.False(result.IsValid);
    }
}
