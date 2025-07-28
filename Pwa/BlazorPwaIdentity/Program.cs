using BlazorPwaIdentity;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.FluentUI.AspNetCore.Components;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services.AddTransient<CookieHandler>();
builder.Services.AddAuthorizationCore();

builder.Services.AddScoped<AuthenticationStateProvider, CookieAuthenticationStateProvider>();
builder.Services.AddScoped(
    sp => (IAccountManagement)sp.GetRequiredService<AuthenticationStateProvider>());

builder.Services.AddScoped(sp =>
    new HttpClient { BaseAddress = new Uri("https://localhost:7074") });

builder.Services.AddHttpClient(
    "Auth",
    opt => opt.BaseAddress = new Uri("https://localhost:7074"))
    .AddHttpMessageHandler<CookieHandler>();

builder.Services.AddFluentUIComponents();

await builder.Build().RunAsync();
