using TripleDerby.Web.Components;
using Microsoft.FluentUI.AspNetCore.Components;
using TripleDerby.ServiceDefaults;
using TripleDerby.Web.ApiClients;
using TripleDerby.Web.ApiClients.Abstractions;
using TripleDerby.Web.Resilience;

var builder = WebApplication.CreateBuilder(args);

// Add service defaults & Aspire client integrations.
builder.AddServiceDefaults();
builder.AddRedisOutputCache("cache");

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddFluentUIComponents();

// Configure API clients with retry and circuit breaker resilience policies
builder.Services.AddApiClientWithResilience<IBreedingApiClient, BreedingApiClient>(client =>
    client.BaseAddress = new("https+http://api"));

builder.Services.AddApiClientWithResilience<IHorseApiClient, HorseApiClient>(client =>
    client.BaseAddress = new("https+http://api"));

builder.Services.AddApiClientWithResilience<IStatsApiClient, StatsApiClient>(client =>
    client.BaseAddress = new("https+http://api"));

builder.Services.AddApiClientWithResilience<IUserApiClient, UserApiClient>(client =>
    client.BaseAddress = new("https+http://api"));

builder.Services.AddApiClientWithResilience<IRaceApiClient, RaceApiClient>(client =>
    client.BaseAddress = new("https+http://api"));

builder.Services.AddApiClientWithResilience<IRaceRunApiClient, RaceRunApiClient>(client =>
    client.BaseAddress = new("https+http://api"));

builder.Services.AddApiClientWithResilience<ITrackApiClient, TrackApiClient>(client =>
    client.BaseAddress = new("https+http://api"));

builder.Services.AddApiClientWithResilience<ITrainingsApiClient, TrainingsApiClient>(client =>
    client.BaseAddress = new("https+http://api"));

builder.Services.AddApiClientWithResilience<IFeedingsApiClient, FeedingsApiClient>(client =>
    client.BaseAddress = new("https+http://api"));

builder.Services.AddApiClientWithResilience<IMessagesApiClient, MessagesApiClient>(client =>
    client.BaseAddress = new("https+http://api"));

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseAntiforgery();

app.UseOutputCache();

app.MapStaticAssets();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.MapDefaultEndpoints();

app.Run();
