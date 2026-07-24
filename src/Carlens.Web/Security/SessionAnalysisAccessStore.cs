using System.Text.Json;
using Carlens.Contracts.Responses;

namespace Carlens.Web.Security;

public sealed class SessionAnalysisAccessStore : IAnalysisAccessStore
{
    private const string AnalysisIdsKey = "security:analysis-ids";
    private const string ImageIdsKey = "security:image-ids";
    private const int MaximumAnalysisCount = 50;
    private const int MaximumImageCount = 300;

    public void Grant(ISession session, ListingAnalysisResponse analysis)
    {
        Add(session, AnalysisIdsKey, analysis.AnalysisId, MaximumAnalysisCount);

        foreach (var imageUrl in analysis.Listing.ImageUrls)
        {
            if (TryGetImageId(imageUrl, out var imageId))
            {
                Add(session, ImageIdsKey, imageId, MaximumImageCount);
            }
        }
    }

    public bool CanAccessAnalysis(ISession session, Guid analysisId)
    {
        return Read(session, AnalysisIdsKey).Contains(analysisId);
    }

    public bool CanAccessImage(ISession session, Guid imageId)
    {
        return Read(session, ImageIdsKey).Contains(imageId);
    }

    private static void Add(
        ISession session,
        string key,
        Guid value,
        int maximumCount)
    {
        var values = Read(session, key);
        values.Remove(value);
        values.Add(value);

        if (values.Count > maximumCount)
        {
            values.RemoveRange(0, values.Count - maximumCount);
        }

        session.SetString(key, JsonSerializer.Serialize(values));
    }

    private static List<Guid> Read(ISession session, string key)
    {
        var json = session.GetString(key);

        if (string.IsNullOrWhiteSpace(json))
        {
            return [];
        }

        try
        {
            return JsonSerializer.Deserialize<List<Guid>>(json) ?? [];
        }
        catch (JsonException)
        {
            session.Remove(key);
            return [];
        }
    }

    private static bool TryGetImageId(string imageUrl, out Guid imageId)
    {
        imageId = Guid.Empty;

        if (!Uri.TryCreate(imageUrl, UriKind.RelativeOrAbsolute, out var uri))
        {
            return false;
        }

        var path = uri.IsAbsoluteUri ? uri.AbsolutePath : uri.OriginalString;
        var fileName = Path.GetFileName(path.Split('?', '#')[0].TrimEnd('/'));
        return Guid.TryParse(fileName, out imageId);
    }
}
