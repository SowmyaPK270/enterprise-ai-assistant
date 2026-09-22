using EnterpriseAiAssistant.Application.Abstractions.Evaluation;
using EnterpriseAiAssistant.Application.Abstractions.Guardrails;
using EnterpriseAiAssistant.Application.Chat.Evaluation;
using EnterpriseAiAssistant.Application.Chat.Guardrails;
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
        services.AddScoped<IChatService, ChatService>();
        services.AddScoped<IGuardrailService, GuardrailService>();
        services.AddScoped<IChatEvaluationService, ChatEvaluationService>();

        return services;
    }
}