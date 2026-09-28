using EnterpriseAiAssistant.Application.Chat.Interfaces;
using EnterpriseAiAssistant.Application.Chat.Models;
using EnterpriseAiAssistant.Domain.Chat;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;
using System.Security.Claims;

namespace EnterpriseAiAssistant.Web.Components.Pages.Chat;

public partial class Chat : ComponentBase
{
    private const string DefaultTitle = "New conversation";

    private const string FooterDelimiter = "\n\n---";

    [Inject]
    protected IChatService ChatService { get; set; } = default!;

    [Inject]
    protected ILogger<Chat> Logger { get; set; } = default!;

    [Inject]
    protected IJSRuntime JS { get; set; } = default!;

    [Inject]
    protected IConfiguration Configuration { get; set; } = default!;

    [Inject]
    protected NavigationManager Navigation { get; set; } = default!;

    [CascadingParameter]
    private Task<AuthenticationState>? AuthenticationStateTask { get; set; }

    protected string UserInput { get; set; } = string.Empty;

    protected bool IsLoading { get; set; }

    protected string StreamingReply { get; set; } = string.Empty;

    protected List<ChatSession> ChatSessions { get; set; } = [];

    protected ChatSession? CurrentSession { get; set; }

    protected IReadOnlyCollection<ChatMessage> CurrentMessages =>
        CurrentSession?.Messages ?? Array.Empty<ChatMessage>();

    protected bool HasMessages =>
        CurrentMessages.Count > 0 || IsLoading;

    protected bool CanSendMessage =>
        !IsLoading &&
        !string.IsNullOrWhiteSpace(UserInput) &&
        CurrentSession is not null;

    private Guid _currentUserId;
    private CancellationTokenSource? _sendCts;

    private ElementReference _messagesContainerRef;
    private ElementReference _inputElement;
    private bool _shouldFocusInput;

    protected override async Task OnInitializedAsync()
    {
        _currentUserId = await ResolveCurrentUserIdAsync();
        if (_currentUserId == Guid.Empty)
        {
            return;
        }

        var sessions = await ChatService.GetSessionsAsync(_currentUserId);

        ChatSessions = sessions.ToList();

        CurrentSession = ChatSessions.Count > 0
            ? ChatSessions[0]
            : await CreateAndTrackNewSessionAsync();
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        try
        {
            if (HasMessages)
            {
                await JS.InvokeVoidAsync(
                    "chatUI.scrollToBottom",
                    _messagesContainerRef);
            }

            if (_shouldFocusInput && !IsLoading)
            {
                _shouldFocusInput = false;

                await JS.InvokeVoidAsync(
                    "chatUI.focusInput",
                    _inputElement);
            }
        }
        catch (JSDisconnectedException)
        {
        }
        catch (JSException ex)
        {
            Logger.LogWarning(ex, "Chat UI JavaScript is unavailable.");
        }
    }

    private async Task<Guid> ResolveCurrentUserIdAsync()
    {
        if (AuthenticationStateTask is null)
        {
            throw new InvalidOperationException(
                "AuthenticationStateTask was not supplied. Make sure " +
                "AddCascadingAuthenticationState() is registered and this " +
                "page renders under it.");
        }

        var authState = await AuthenticationStateTask;
        var principal = authState.User;

        var authEnabled = Configuration.GetValue<bool>("Authentication:Enabled", defaultValue: true);
        if (principal.Identity?.IsAuthenticated != true)
        {
            if (!authEnabled)
            {
                var guestUser = await ChatService.EnsureUserAsync(
                    $"guest-{Guid.NewGuid()}",
                    "Guest");
                return guestUser.Id;
            }
            else
            {
                Navigation.NavigateTo(
                    "/MicrosoftIdentity/Account/SignIn?returnUrl=%2Fchat",
                    forceLoad: true);
                return Guid.Empty; 
            }
        }

        var externalId =
            principal.FindFirst("oid")?.Value ??
            principal.FindFirst(ClaimTypes.NameIdentifier)?.Value ??
            principal.Identity?.Name;

        if (string.IsNullOrWhiteSpace(externalId))
        {
            throw new InvalidOperationException(
                "Could not resolve an identity for the signed-in user.");
        }

        var displayName =
            principal.FindFirst("name")?.Value ??
            principal.Identity?.Name ??
            "User";

        var user = await ChatService.EnsureUserAsync(externalId, displayName);

        return user.Id;
    }

    protected async Task SendMessageAsync()
    {
        if (!CanSendMessage || CurrentSession is null)
        {
            return;
        }

        var userMessage = UserInput.Trim();
        var session = CurrentSession;

        UserInput = string.Empty;
        IsLoading = true;
        StreamingReply = string.Empty;

        _sendCts = new CancellationTokenSource();

        session.AddMessage(
            ChatMessage.Create(ChatRole.User, userMessage));

        StateHasChanged();

        try
        {
            var request = new ChatRequest(
                session.Id,
                _currentUserId,
                userMessage);

            await foreach (var chunk in ChatService.StreamMessageAsync(
                request,
                _sendCts.Token))
            {
                StreamingReply += chunk;
                StateHasChanged();
            }

            if (StreamingReply.Length > 0)
            {
                session.AddMessage(
                    ChatMessage.Create(ChatRole.Assistant, StreamingReply));
            }

            if (session.Title == DefaultTitle)
            {
                var title = userMessage.Length > 30
                    ? userMessage[..30] + "..."
                    : userMessage;

                session.Rename(title);
            }
        }
        catch (OperationCanceledException)
        {
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Chat request failed.");

            session.AddMessage(
                ChatMessage.Create(
                    ChatRole.Assistant,
                    $"Error: {ex.Message}"));
        }
        finally
        {
            StreamingReply = string.Empty;
            IsLoading = false;
            _sendCts.Dispose();
            _sendCts = null;
            _shouldFocusInput = true;
        }
    }

    protected async Task StartNewChat()
    {
        CurrentSession = await CreateAndTrackNewSessionAsync();
        UserInput = string.Empty;
        _shouldFocusInput = true;
    }

    protected void SelectSession(ChatSession session)
    {
        CurrentSession = session;
        UserInput = string.Empty;
        _shouldFocusInput = true;
    }

    protected async Task HandleKeyDown(
         KeyboardEventArgs e)
    {
        if (e.Key == "Enter" && !e.ShiftKey)
        {
            await SendMessageAsync();
        }
    }

    private async Task<ChatSession> CreateAndTrackNewSessionAsync()
    {
        var session = await ChatService.CreateSessionAsync(_currentUserId);

        ChatSessions.Insert(0, session);

        return session;
    }

    private static (string Main, string? Footer) SplitMessageContent(string content)
    {
        var index = content.IndexOf(FooterDelimiter, StringComparison.Ordinal);

        return index < 0
            ? (content, null)
            : (content[..index], content[(index + FooterDelimiter.Length)..].TrimStart('\n'));
    }
}