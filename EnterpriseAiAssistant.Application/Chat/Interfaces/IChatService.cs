using EnterpriseAiAssistant.Application.Chat.Models;

//Contract for the application's chat use case
//Tt is better for Web to depend on an abstraction
//UI depend on a concrete implementation if used ChatServuice direcly //[Inject]protected ChatService ChatService { get; set; } = default!;  - We dont need this
namespace EnterpriseAiAssistant.Application.Chat.Interfaces;

public interface IChatService
{
    Task<ChatResponse> SendMessageAsync(
        ChatRequest request,
        CancellationToken cancellationToken = default);
}