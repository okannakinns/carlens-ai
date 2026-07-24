using FluentValidation;

namespace Carlens.Application.Features.Listings.Commands;

public sealed class CreateListingAnalysisCommandValidator
    : AbstractValidator<CreateListingAnalysisCommand>
{
    public CreateListingAnalysisCommandValidator()
    {
        RuleFor(command => command.ListingUrl)
            .NotEmpty()
            .MaximumLength(1000)
            .Must(BeSupportedArabamListingUrl)
            .WithMessage("Geçerli bir Arabam.com ilan bağlantısı girin.");
    }

    private static bool BeSupportedArabamListingUrl(string listingUrl)
    {
        if (!Uri.TryCreate(listingUrl, UriKind.Absolute, out var uri) ||
            uri.Scheme != Uri.UriSchemeHttps)
        {
            return false;
        }

        var isArabamHost =
            uri.Host.Equals("arabam.com", StringComparison.OrdinalIgnoreCase) ||
            uri.Host.Equals("www.arabam.com", StringComparison.OrdinalIgnoreCase);

        return isArabamHost &&
               uri.AbsolutePath.StartsWith("/ilan/", StringComparison.OrdinalIgnoreCase);
    }
}
