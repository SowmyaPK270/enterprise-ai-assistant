using System;
using System.Collections.Generic;
using System.Text;

namespace EnterpriseAiAssistant.Application.Abstractions.AI;

public interface IAIClient
{
    Task<AIResponse> CompleteAsync(
        AIRequest request,
        CancellationToken cancellationToken = default);
}