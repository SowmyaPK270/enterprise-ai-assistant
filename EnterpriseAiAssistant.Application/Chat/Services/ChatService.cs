using EnterpriseAiAssistant.Application.Abstractions.AI;
using EnterpriseAiAssistant.Application.Chat.Interfaces;
using EnterpriseAiAssistant.Application.Chat.Models;
using EnterpriseAiAssistant.Domain.Chat;
using System;
using System.Collections.Generic;
using System.Text;

//Sits inbetween ChatRequest and AIRequest
//application can receive a chat request and return a chat response.
//main application use-case service.
namespace EnterpriseAiAssistant.Application.Chat.Services;

public sealed class ChatService : IChatService
{
    private readonly IAIClient _aiClient;

    public ChatService(IAIClient aiClient)
    {
        _aiClient = aiClient;
    }

    public async Task<ChatResponse> SendMessageAsync(
        ChatRequest request,
        CancellationToken cancellationToken = default)
    {
        if (request is null)
        {
            throw new ArgumentNullException(nameof(request));
        }

        if (string.IsNullOrWhiteSpace(request.Message))
        {
            throw new ArgumentException(
                "Message cannot be empty.",
                nameof(request));
        }

        var messages = new List<ChatMessage>();

        if (request.History is not null)
        {
            messages.AddRange(request.History);
        }

        messages.Add(
            ChatMessage.Create(
                ChatRole.User,
                request.Message));

        var aiRequest = new AIRequest(messages);

        var aiResponse = await _aiClient.CompleteAsync(
            aiRequest,
            cancellationToken);

        return new ChatResponse(aiResponse.Content);
    }
}



/*UI request
   ↓
ChatService
   ↓
Build AIRequest
   ↓
Call IAIClient
   ↓
Convert AIResponse to ChatResponse
   ↓
Return to UI..




Why is this file not in the Web layer?

Because sending a chat message is an application action




The Web layer should handle:

Button clicks

Text input

Displaying messages

Loading indicators

The Application layer should handle:

Preparing the request

Calling the AI abstraction

Applying business workflow

Returning the result*/