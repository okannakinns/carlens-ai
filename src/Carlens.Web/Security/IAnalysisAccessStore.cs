using Carlens.Contracts.Responses;

namespace Carlens.Web.Security;

public interface IAnalysisAccessStore
{
    void Grant(ISession session, ListingAnalysisResponse analysis);
    bool CanAccessAnalysis(ISession session, Guid analysisId);
    bool CanAccessImage(ISession session, Guid imageId);
}
