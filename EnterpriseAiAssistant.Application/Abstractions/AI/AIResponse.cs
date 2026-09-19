using System;
using System.Collections.Generic;
using System.Text;

//Represents the response returned by an AI provider.
namespace EnterpriseAiAssistant.Application.Abstractions.AI;

public sealed record AIResponse(
    string Content);
