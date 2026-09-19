using EnterpriseAiAssistant.Application.Chat.Interfaces;
using EnterpriseAiAssistant.Application.Chat.Models;
using EnterpriseAiAssistant.Domain.Chat;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;

namespace EnterpriseAiAssistant.Web.Components.Pages.Chat;

public partial class Chat : ComponentBase
{
    [Inject]
    protected IChatService ChatService { get; set; } = default!;
    [Inject]
    protected ILogger<Chat> Logger { get; set; } = default!;

    protected string UserInput { get; set; } = string.Empty;

    protected bool IsLoading { get; set; }

    protected List<ChatSession> ChatSessions { get; set; } = [];

    protected ChatSession CurrentSession { get; set; } =
        ChatSession.Create();

    protected IReadOnlyCollection<ChatMessage> CurrentMessages =>
        CurrentSession.Messages;

    protected bool HasMessages =>
        CurrentMessages.Count > 0;

    protected bool CanSendMessage =>
        !IsLoading &&
        !string.IsNullOrWhiteSpace(UserInput);


    protected override void OnInitialized()
    {
        ChatSessions.Add(CurrentSession);
    }


    protected async Task SendMessageAsync()
    {
        if (!CanSendMessage)
            return;

        var userMessage = UserInput.Trim();

        UserInput = string.Empty;

        IsLoading = true;

        try
        {
            // Create the user's domain message.
            var userChatMessage =
                ChatMessage.Create(
                    ChatRole.User,
                    userMessage);

            // Add the user's message to the session.
            CurrentSession.AddMessage(userChatMessage);

            // Send the current conversation to the application layer.
            var request = new ChatRequest(
                userMessage,
                CurrentSession.Messages.ToList());

            var response =
                await ChatService.SendMessageAsync(request);

            // Add the AI response to the session.
            var assistantChatMessage =
                ChatMessage.Create(
                    ChatRole.Assistant,
                    response.Message);

            CurrentSession.AddMessage(assistantChatMessage);

            // Create the session title from the first user message.
            if (CurrentSession.Title == "New conversation")
            {
                var title = userMessage.Length > 30
                    ? userMessage[..30] + "..."
                    : userMessage;

                CurrentSession.Rename(title);
            }
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Chat request failed.");

            CurrentSession.AddMessage(
                ChatMessage.Create(
                    ChatRole.Assistant,
                    $"Error: {ex.Message}"));
        }
        finally
        {
            IsLoading = false;
        }
    }


    protected void StartNewChat()
    {
        CurrentSession = ChatSession.Create();

        ChatSessions.Insert(0, CurrentSession);

        UserInput = string.Empty;
    }


    protected void SelectSession(ChatSession session)
    {
        CurrentSession = session;

        UserInput = string.Empty;
    }


    protected async Task HandleKeyDown(
        KeyboardEventArgs e)
    {
        if (e.Key == "Enter" && !e.ShiftKey)
        {
            await SendMessageAsync();
        }
    }
}