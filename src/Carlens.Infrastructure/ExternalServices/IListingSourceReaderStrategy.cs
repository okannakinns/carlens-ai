using Carlens.Application.Interfaces;

namespace Carlens.Infrastructure.ExternalServices;

public interface IPrimaryListingSourceReader : IListingSourceReader;

public interface IFallbackListingSourceReader : IListingSourceReader;
