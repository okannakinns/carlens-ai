using Carlens.Contracts.Responses;
using Carlens.Web.Security;
using Microsoft.AspNetCore.Http;

namespace Carlens.Tests;

public sealed class SessionAnalysisAccessStoreTests
{
    [Fact]
    public void Grant_AllowsOnlyGrantedAnalysisAndImages()
    {
        var analysisId = Guid.NewGuid();
        var imageId = Guid.NewGuid();
        var session = new TestSession();
        var store = new SessionAnalysisAccessStore();
        var response = CreateResponse(
            analysisId,
            $"/api/listing-images/{imageId}");

        store.Grant(session, response);

        Assert.True(store.CanAccessAnalysis(session, analysisId));
        Assert.True(store.CanAccessImage(session, imageId));
        Assert.False(store.CanAccessAnalysis(session, Guid.NewGuid()));
        Assert.False(store.CanAccessImage(session, Guid.NewGuid()));
    }

    private static ListingAnalysisResponse CreateResponse(
        Guid analysisId,
        string imageUrl)
    {
        return new ListingAnalysisResponse(
            analysisId,
            "Pending",
            "Queued",
            new ListingSummaryResponse(
                Guid.NewGuid(),
                "Url",
                "https://example.test/listing",
                null,
                "Test vehicle",
                "Test",
                null,
                "Model",
                2020,
                1_000_000m,
                50_000,
                "Gasoline",
                "Automatic",
                "Individual",
                "Istanbul",
                [imageUrl],
                1,
                0),
            null,
            null,
            DateTime.UtcNow,
            null,
            new AnalysisUsageResponse(0, 0, 0, null));
    }

    private sealed class TestSession : ISession
    {
        private readonly Dictionary<string, byte[]> _values = [];

        public bool IsAvailable => true;
        public string Id { get; } = Guid.NewGuid().ToString();
        public IEnumerable<string> Keys => _values.Keys;

        public void Clear()
        {
            _values.Clear();
        }

        public Task CommitAsync(CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }

        public Task LoadAsync(CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }

        public void Remove(string key)
        {
            _values.Remove(key);
        }

        public void Set(string key, byte[] value)
        {
            _values[key] = value;
        }

        public bool TryGetValue(string key, out byte[] value)
        {
            return _values.TryGetValue(key, out value!);
        }
    }
}
