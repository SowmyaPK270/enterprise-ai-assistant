 using EnterpriseAiAssistant.Domain.Chat;
using System;
using System.Collections.Generic;
using System.Text;
using static System.Net.Mime.MediaTypeNames;

//Request sent from your Application layer to the AI provider
//Represents what the application is asking the AI provider to process
namespace EnterpriseAiAssistant.Application.Abstractions.AI;

public sealed record AIRequest(
    IReadOnlyList<ChatMessage> Messages);