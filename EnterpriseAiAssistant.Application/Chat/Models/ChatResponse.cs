using System;
using System.Collections.Generic;
using System.Text;

//Represents the response returned to the UI.
namespace EnterpriseAiAssistant.Application.Chat.Models;

public sealed record ChatResponse(
    string Message);
