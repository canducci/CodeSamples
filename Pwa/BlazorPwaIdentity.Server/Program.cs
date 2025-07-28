using BlazorPwaIdentity.Server;
using BlazorPwaIdentity.Server.Api;
using BlazorPwaIdentity.Server.Components.Account;
using BlazorPwaIdentity.Server.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

//https://github.com/dotnet/blazor-samples/tree/main/9.0/BlazorWebAssemblyStandaloneWithIdentity

builder.Services.AddOutputCache();

builder.Services.AddAuthentication(IdentityConstants.ApplicationScheme)
    .AddIdentityCookies(c => { });

builder.Services.ConfigureApplicationCookie(options =>
{    
    options.Cookie.SameSite = SameSiteMode.None;
    //options.Cookie.SameSite = SameSiteMode.Lax;
    options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
    //options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
});

builder.Services.AddAuthorizationBuilder();

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseInMemoryDatabase("BlazorPwaIdentity"));
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
        policy => policy.WithOrigins([builder.Configuration["frontend"] ?? "https://localhost:7103"])
            .AllowAnyMethod()
            .AllowAnyHeader()
            .AllowCredentials()));

builder.Services.AddSingleton<IEmailSender<ApplicationUser>, IdentityNoOpEmailSender>();

var app = builder.Build();

// Configure the HTTP request pipeline.
//if (app.Environment.IsDevelopment())
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    //db.Database.EnsureDeleted(); //Using InMemory Db
    //if (db.Database.EnsureCreated())
    //{
        SeedData.Execute(db);
    //}
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

app.UseOutputCache();

app.MapForecastEndpoints();

app.Run();
