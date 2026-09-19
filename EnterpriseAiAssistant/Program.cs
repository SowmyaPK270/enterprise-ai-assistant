using EnterpriseAiAssistant.Application;
using EnterpriseAiAssistant.Infrastructure;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.Identity.Web;
using Microsoft.Identity.Web.UI;

var builder = WebApplication.CreateBuilder(args);

// --------------------------------------------------
// Blazor Web App
// --------------------------------------------------

builder.Services
    .AddRazorComponents()
    .AddInteractiveServerComponents();

// --------------------------------------------------
// Microsoft Entra ID Authentication
// --------------------------------------------------

builder.Services
    .AddAuthentication(OpenIdConnectDefaults.AuthenticationScheme)
    .AddMicrosoftIdentityWebApp(
        builder.Configuration.GetSection("AzureAd"));

builder.Services
    .AddControllersWithViews()
    .AddMicrosoftIdentityUI();

builder.Services.AddCascadingAuthenticationState();

// --------------------------------------------------
// Authorization
// --------------------------------------------------

builder.Services.AddAuthorization();

// --------------------------------------------------
// Application Layer
// --------------------------------------------------

builder.Services.AddApplication();

// --------------------------------------------------
// Infrastructure Layer
// --------------------------------------------------

builder.Services.AddInfrastructure(
    builder.Configuration);

var app = builder.Build();

// --------------------------------------------------
// HTTP Pipeline
// --------------------------------------------------

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

app.MapRazorComponents<
    EnterpriseAiAssistant.Web.Components.App>()
    .AddInteractiveServerRenderMode();

app.Run();