using BlazorPwaIdentity.Server;
using BlazorPwaIdentity.Server.Api;
using BlazorPwaIdentity.Server.Components;
using BlazorPwaIdentity.Server.Components.Account;
using BlazorPwaIdentity.Server.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.FluentUI.AspNetCore.Components;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddAuthentication(IdentityConstants.ApplicationScheme)
    .AddIdentityCookies();

builder.Services.AddAuthorizationBuilder();

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(connectionString));
builder.Services.AddDatabaseDeveloperPageExceptionFilter();

builder.Services.AddIdentityCore<ApplicationUser>(options =>
{
    //options.SignIn.RequireConfirmedAccount = false;
    options.Password.RequiredLength = 0;
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequireDigit = false;
    options.Password.RequireLowercase = false;
    options.Password.RequireUppercase = false;
})
    .AddRoles<IdentityRole>()
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddApiEndpoints();

builder.Services.AddCors(
    options => options.AddPolicy(
        "wasm",
        policy => policy.WithOrigins(["https://localhost:7103"])
            .AllowAnyMethod()
            .AllowAnyHeader()
            .AllowCredentials()));

builder.Services.AddSingleton<IEmailSender<ApplicationUser>, IdentityNoOpEmailSender>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    //db.Database.EnsureDeleted();
    if (db.Database.EnsureCreated())
    {
        SeedData.Execute(db);
    }
}

app.MapIdentityApi<ApplicationUser>();
app.MapPost("/logout", async (SignInManager<ApplicationUser> signInManager, [FromBody] object empty) =>
{
    if (empty is not null)
    {
        await signInManager.SignOutAsync();

        return Results.Ok();
    }

    return Results.Unauthorized();
}).RequireAuthorization();

app.UseCors("wasm");

app.UseAuthentication();
app.UseAuthorization();

app.MapForecastEndpoints();

app.Run();
