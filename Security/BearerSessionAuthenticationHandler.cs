using System.Security.Claims;
using System.Text.Encodings.Web;
using DogWalkingApp.Api.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace DogWalkingApp.Api.Security;

public sealed class BearerSessionAuthenticationHandler
    : AuthenticationHandler<AuthenticationSchemeOptions>
{
    public const string SchemeName = "BearerSession";

    private readonly AppDbContext _context;

    public BearerSessionAuthenticationHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder,
        AppDbContext context)
        : base(options, logger, encoder)
    {
        _context = context;
    }

    protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (!Request.Headers.TryGetValue("Authorization", out var header))
        {
            return AuthenticateResult.NoResult();
        }

        var authorization = header.ToString();
        if (!authorization.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
        {
            return AuthenticateResult.NoResult();
        }

        var token = authorization["Bearer ".Length..].Trim();
        if (token.Length == 0)
        {
            return AuthenticateResult.Fail("Bearer token is empty.");
        }

        var tokenHash = SessionToken.Hash(token);
        var now = DateTime.UtcNow;
        var session = await _context.UserSessions
            .Include(item => item.User)
            .FirstOrDefaultAsync(
                item => item.TokenHash == tokenHash &&
                        item.RevokedAt == null &&
                        item.ExpiresAt > now,
                Context.RequestAborted);

        if (session?.User is null)
        {
            return AuthenticateResult.Fail("Bearer token is invalid or expired.");
        }

        var user = session.User;
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Name, user.Name),
            new Claim(ClaimTypes.Email, user.Email),
            new Claim(ClaimTypes.Role, user.Role)
        };
        var identity = new ClaimsIdentity(claims, SchemeName);
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, SchemeName);
        return AuthenticateResult.Success(ticket);
    }
}