using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.Metadata;
using Microsoft.Extensions.Options;
using MongoDB.Driver;
using System.Collections.ObjectModel;
using System.Security.Claims;
using System.Text.Encodings.Web;

namespace EventSourceApi;

internal sealed class ApiKeyAuthenticationSchemeHandler(IOptionsMonitor<AuthenticationSchemeOptions> options, ILoggerFactory logger, UrlEncoder encoder)
    : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
{
    internal const string ApiKeyScheme = "ApiKey";

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (!Context.Request.Headers.TryGetValue("X-Api-Key", out var apiKey))
        {
            return Task.FromResult(AuthenticateResult.NoResult());
        }

        if (apiKey != "my-api-key")
        {
            return Task.FromResult(AuthenticateResult.Fail("Invalid API key"));
        }

        var identity = new ClaimsIdentity(
            [
                new Claim(ClaimTypes.Name, "Api-Key-User"),
                new Claim(ClaimTypes.Role, "Admin"),
            ],
            Scheme.Name);
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, Scheme.Name);
        var result = AuthenticateResult.Success(ticket);
        return Task.FromResult(result);
    }
}

public class RoleResourceAuthorization : AuthorizationHandler<RoleResourceRequirement>
{
    ReadOnlyCollection<(string resource, string method, string role)> _roleResources =
       [
            ("orders","GET", "Admin"),
            ("orders/{orderId:guid}","GET", "Admin"),
            ("orders/{orderId:guid}","PUT", "SuperAdmin"),
            ("orders/{orderId:guid}","PUT", "Admin"),
            ("orders","POST", "Admin"),
            ("orders","POST", "SuperAdmin"),
            ("orders/{orderId:guid}","DELETE", "SuperAdmin"),

            ("suppliers","GET", "Admin"),
            ("suppliers/{supplierId:guid}","GET", "Admin"),
            ("suppliers/{supplierId:guid}","PUT", "SuperAdmin"),
            ("suppliers/{supplierId:guid}","PUT", "Admin"),
            ("suppliers","POST", "Admin"),
            ("suppliers","POST", "SuperAdmin"),
            ("suppliers/{supplierId:guid}","DELETE", "SuperAdmin"),
        ];

    const bool AllowByDefault = false;

    protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, RoleResourceRequirement requirement)
    {
        var resource = context.Resource as HttpContext;
        var principal = context.User;

        var method = resource?.Request.Method;
        var endpt = resource?.GetEndpoint();
        var routept = (endpt?.Metadata.FirstOrDefault(x => x is IRouteDiagnosticsMetadata) as IRouteDiagnosticsMetadata)?.Route ?? "/";
        routept = string.Join('/', routept.Split('/', StringSplitOptions.RemoveEmptyEntries));

        var roles = _roleResources.Where(x => x.resource.Equals(routept, StringComparison.InvariantCultureIgnoreCase) && x.method == method).ToArray();

        if (roles.Length == 0 && AllowByDefault)
        {
            context.Succeed(requirement);
            return Task.CompletedTask;
        }

        for (int i = 0; i < roles.Length; i++)
        {
            if (principal.IsInRole(roles[i].role))
            {
                context.Succeed(requirement);
                return Task.CompletedTask;
            }
        }

        context.Fail(new AuthorizationFailureReason(this, "User are not in role needed to access this resource"));
        return Task.CompletedTask;
    }
}

public class RoleResourceRequirement : IAuthorizationRequirement;
