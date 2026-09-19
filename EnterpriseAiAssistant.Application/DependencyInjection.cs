using EnterpriseAiAssistant.Application.Chat.Interfaces;
using EnterpriseAiAssistant.Application.Chat.Services;
using Microsoft.Extensions.DependencyInjection;

namespace EnterpriseAiAssistant.Application;

//Registers Application services with .NET dependency injection
//Instead of placing many registrations inside Program.cs, you keep them organized
public static class DependencyInjection
{
    public static IServiceCollection AddApplication(
        this IServiceCollection services)
    {
        //services.AddScoped<ChatService>();
        services.AddScoped<IChatService, ChatService>();

        return services;
    }
}