using System.Linq;
using System.Runtime.CompilerServices;

using EnterpriseAiAssistant.Application.Abstractions.AI;
using EnterpriseAiAssistant.Domain.Chat;
using OpenAI.Chat;

// Converts domain messages to SDK messages and calls the Azure OpenAI SDK.
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
        var messages = BuildMessages(request);

        var completion = await _client.ChatClient.CompleteChatAsync(
            messages,
            cancellationToken: cancellationToken);

        var content = completion.Value.Content
            .FirstOrDefault()?.Text;

        if (string.IsNullOrWhiteSpace(content))
        {
            throw new InvalidOperationException(
                "Azure OpenAI returned an empty response.");
        }

        return new AIResponse(content);
    }

    public async IAsyncEnumerable<string> StreamCompleteAsync(
        AIRequest request,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var messages = BuildMessages(request);

        var updates = _client.ChatClient.CompleteChatStreamingAsync(
            messages,
            cancellationToken: cancellationToken);

        await foreach (var update in updates)
        {
            foreach (var part in update.ContentUpdate)
            {
                if (!string.IsNullOrEmpty(part.Text))
                {
                    yield return part.Text;
                }
            }
        }
    }

    private static List<OpenAI.Chat.ChatMessage> BuildMessages(AIRequest request)
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

        return messages;
    }
}
