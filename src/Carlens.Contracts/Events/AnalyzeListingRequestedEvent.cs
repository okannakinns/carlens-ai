namespace Carlens.Contracts.Events;

public sealed record AnalyzeListingRequestedEvent(
    Guid AnalysisId,
    Guid CarListingId,
    DateTime RequestedAtUtc);
