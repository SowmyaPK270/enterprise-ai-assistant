using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;

using EnterpriseAiAssistant.Application.Abstractions.AI;
using EnterpriseAiAssistant.Domain.Chat;
using OpenAI.Chat;

// Converts messages and calls SDK
// AzureOpenAIService is the actual bridge between your application-level AI abstraction and the Azure OpenAI SDK.
namespace EnterpriseAiAssistant.Infrastructure.AI.AzureOpenAI;

public sealed class AzureOpenAIService : IAIClient
{
    private readonly AzureOpenAIClient _client;

    public AzureOpenAIService(
        AzureOpenAIClient client)
    {
        _client = client;
    }

    public async Task<AIResponse> CompleteAsync(
        AIRequest request,
        CancellationToken cancellationToken = default)
    {
        var messages = new List<OpenAI.Chat.ChatMessage>();

        foreach (var message in request.Messages)
        {
            switch (message.Role)
            {
                case ChatRole.System:
                    messages.Add(
                        new SystemChatMessage(message.Content));
                    break;

                case ChatRole.User:
                    messages.Add(
                        new UserChatMessage(message.Content));
                    break;

                case ChatRole.Assistant:
                    messages.Add(
                        new AssistantChatMessage(message.Content));
                    break;

                default:
                    throw new InvalidOperationException(
                        $"Unsupported chat role: {message.Role}");
            }
        }

        //This line is where your application actually calls the Azure OpenAI model 
        //Chat Completions API.
        var completion = await _client.ChatClient.CompleteChatAsync(messages);  

        var content = completion.Value.Content
            .FirstOrDefault()?.Text;

        if (string.IsNullOrWhiteSpace(content))
        {
            throw new InvalidOperationException(
                "Azure OpenAI returned an empty response.");
        }

        return new AIResponse(content);
    }
}



/*Now it has SDK-compatible messages

Before the conversion:

Your Application/Domain objects

ChatMessage
ChatMessage
ChatMessage

After the conversion:

OpenAI SDK objects

SystemChatMessage
UserChatMessage
AssistantChatMessage

This is one of the main jobs of this class.*/



/*
 
var completion = await _client.ChatClient
    .CompleteChatAsync(messages);


_client
   ↓
AzureOpenAIClient                 ← your wrapper
   ↓
ChatClient                        ← OpenAI SDK
   ↓
CompleteChatAsync()
   ↓
Azure OpenAI*/




/*In one sentence:

AzureOpenAIService takes your application's AIRequest, converts your Domain ChatMessage objects into Azure/OpenAI SDK message objects, calls Azure OpenAI through ChatClient, extracts the generated text, and converts it into your application's AIResponse.*/


















