using Carlens.Contracts.Events;
using System;
using System.Collections.Generic;
using System.Text;

namespace Carlens.Application.Interfaces
{
    public interface IAnalysisRequestPublisher
    {
        Task PublishAsync(AnalyzeListingRequestedEvent analysisRequestedEvent, CancellationToken cancellationToken = default);
    }
}
