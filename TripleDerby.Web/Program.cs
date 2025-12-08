using TripleDerby.Web.Components;
using Microsoft.FluentUI.AspNetCore.Components;
using TripleDerby.ServiceDefaults;
using TripleDerby.Web.ApiClients;
using TripleDerby.Web.ApiClients.Abstractions;

var builder = WebApplication.CreateBuilder(args);

// Add service defaults & Aspire client integrations.
builder.AddServiceDefaults();
builder.AddRedisOutputCache("cache");

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddFluentUIComponents();

builder.Services.AddHttpClient<IBreedingApiClient, BreedingApiClient>(client =>
{
    client.BaseAddress = new("https+http://api");
}); 
builder.Services.AddHttpClient<IHorseApiClient, HorseApiClient>(client =>
{
    client.BaseAddress = new("https+http://api");
});
builder.Services.AddHttpClient<IStatsApiClient, StatsApiClient>(client =>
{
    client.BaseAddress = new("https+http://api");
});
builder.Services.AddHttpClient<IUserApiClient, UserApiClient>(client =>
{
    client.BaseAddress = new("https+http://api");
}); 

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
