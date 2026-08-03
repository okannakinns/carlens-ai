using System.Reflection;
using Carlens.AiWorker;
using Carlens.Api.Controllers;
using Carlens.Application.Features.Listings.Commands;
using Carlens.Contracts.Events;
using Carlens.Domain.Entities;
using Carlens.Infrastructure.Persistence;
using Carlens.Web.Services;
using NetArchTest.Rules;

namespace Carlens.ArchitectureTests;

public sealed class LayerDependencyTests
{
    [Fact]
    public void Domain_must_not_depend_on_any_other_Carlens_layer()
    {
        AssertHasNoDependencyOn(
            typeof(CarListing).Assembly,
            "Carlens.Domain",
            "Carlens.Application",
            "Carlens.Contracts",
            "Carlens.Infrastructure",
            "Carlens.Api",
            "Carlens.AiWorker",
            "Carlens.Web");
    }

    [Fact]
    public void Contracts_must_not_depend_on_any_other_Carlens_layer()
    {
        AssertHasNoDependencyOn(
            typeof(AnalyzeListingRequestedEvent).Assembly,
            "Carlens.Contracts",
            "Carlens.Domain",
            "Carlens.Application",
            "Carlens.Infrastructure",
            "Carlens.Api",
            "Carlens.AiWorker",
            "Carlens.Web");
    }

    [Fact]
    public void Application_must_depend_only_on_Domain_and_Contracts()
    {
        AssertHasNoDependencyOn(
            typeof(CreateListingAnalysisCommandHandler).Assembly,
            "Carlens.Application",
            "Carlens.Infrastructure",
            "Carlens.Api",
            "Carlens.AiWorker",
            "Carlens.Web");
    }

    [Fact]
    public void Infrastructure_must_not_depend_on_host_projects()
    {
        AssertHasNoDependencyOn(
            typeof(CarlensDbContext).Assembly,
            "Carlens.Infrastructure",
            "Carlens.Api",
            "Carlens.AiWorker",
            "Carlens.Web");
    }

    [Fact]
    public void Api_and_worker_must_not_depend_on_each_other_or_Web()
    {
        AssertHasNoDependencyOn(
            typeof(ListingAnalysesController).Assembly,
            "Carlens.Api",
            "Carlens.AiWorker",
            "Carlens.Web");

        AssertHasNoDependencyOn(
            typeof(Worker).Assembly,
            "Carlens.AiWorker",
            "Carlens.Api",
            "Carlens.Web");
    }

    [Fact]
    public void Web_must_use_only_Contracts_from_the_Carlens_layers()
    {
        AssertHasNoDependencyOn(
            typeof(ListingAnalysisApiClient).Assembly,
            "Carlens.Web",
            "Carlens.Domain",
            "Carlens.Application",
            "Carlens.Infrastructure",
            "Carlens.Api",
            "Carlens.AiWorker");
    }

    private static void AssertHasNoDependencyOn(
        Assembly assembly,
        string layerName,
        params string[] forbiddenNamespaces)
    {
        var result = Types
            .InAssembly(assembly)
            .ShouldNot()
            .HaveDependencyOnAny(forbiddenNamespaces)
            .GetResult();

        var failingTypes = result.FailingTypes is null
            ? "Unknown type"
            : string.Join(", ", result.FailingTypes.Select(type => type.FullName));

        Assert.True(
            result.IsSuccessful,
            $"{layerName} contains a forbidden dependency: {failingTypes}");
    }
}
