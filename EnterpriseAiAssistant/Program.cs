using EnterpriseAiAssistant.Application;
using EnterpriseAiAssistant.Application.Ingestion;
using EnterpriseAiAssistant.Ingestion;
using EnterpriseAiAssistant.Infrastructure;
using EnterpriseAiAssistant.Infrastructure.Persistence;
using EnterpriseAiAssistant.Plugins;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.Identity.Web;
using Microsoft.Identity.Web.UI;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);
builder.Services
    .AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services
    .AddAuthentication(OpenIdConnectDefaults.AuthenticationScheme)
    .AddMicrosoftIdentityWebApp(
        builder.Configuration.GetSection("AzureAd"));

builder.Services
    .AddControllersWithViews()
    .AddMicrosoftIdentityUI();

builder.Services.AddCascadingAuthenticationState();
builder.Services.AddAuthorization();

builder.Services.AddApplication();
builder.Services.AddInfrastructure(
    builder.Configuration);

// Semantic Kernel orchestration + multi-source RAG plugins.
// Must be registered after AddInfrastructure() so SemanticKernelAIClient
// wins the IAIClient registration (see Plugins.DependencyInjection).
builder.Services.AddSemanticKernelPlugins();

// Background ingestion of SQL Server JOB data (into Cosmos DB
// and Azure AI Search) and of uploaded documents (into Azure AI Search).
builder.Services.AddIngestion();

var app = builder.Build();


using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ConversationDbContext>();
    await db.Database.MigrateAsync();
}

// The JOB source-of-truth database and the Azure AI Search index /
// Cosmos DB container are created and seeded by InitialSyncHostedService
// in the background (see EnterpriseAiAssistant.Ingestion), so they don't
// delay the app becoming ready to serve chat requests.

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseStaticFiles();

app.UseAuthentication();
app.UseAuthorization();

app.UseAntiforgery();

app.MapControllers();

// Ingestion endpoints: Observe progress and
// upload a document (e.g. a maintenance manual) for RAG without 
// a dedicated admin UI yet.
app.MapGet("/api/ingestion/status", async (
    IIngestionStatusStore statusStore,
    CancellationToken cancellationToken) =>
{
    var recent = await statusStore.GetRecentAsync(cancellationToken: cancellationToken);
    return Results.Ok(recent);
})
.RequireAuthorization();

app.MapPost("/api/ingestion/documents", async (
    IFormFile file,
    IngestionQueue queue) =>
{
    if (file.Length == 0)
    {
        return Results.BadRequest("The uploaded file is empty.");
    }

    await using var stream = file.OpenReadStream();
    using var memoryStream = new MemoryStream();
    await stream.CopyToAsync(memoryStream);

    queue.Enqueue(IngestionWorkItem.ForDocument(
        file.FileName, file.ContentType, memoryStream.ToArray()));

    return Results.Accepted(value: new { file.FileName, Status = "Queued" });
})
.RequireAuthorization()
.DisableAntiforgery();

app.MapRazorComponents<
    EnterpriseAiAssistant.Web.Components.App>()
    .AddInteractiveServerRenderMode();

app.Run();