using System;
using System.Collections.Generic;
using System.Text;

//Represents the request coming from the UI.
//represents what the user is asking the application to do
namespace EnterpriseAiAssistant.Application.Chat.Models;

public sealed record ChatRequest(
    string Message,
    IReadOnlyList<Domain.Chat.ChatMessage> History);