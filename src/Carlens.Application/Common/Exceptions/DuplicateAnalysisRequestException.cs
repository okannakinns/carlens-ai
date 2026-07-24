namespace Carlens.Application.Common.Exceptions;

public sealed class DuplicateAnalysisRequestException : Exception
{
    public DuplicateAnalysisRequestException(string message)
        : base(message)
    {
    }
}
