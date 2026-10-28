using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.EntityFrameworkCore;
using PTK.SIGMA.Data.Context;

namespace PTK.SIGMA.Api;

public sealed class LocalUserClaimsTransformation : IClaimsTransformation
{
    private const string RoleSourceClaim = "PTK.SIGMA.RoleSource";

    private readonly AppDbContext _dbContext;

    public LocalUserClaimsTransformation(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<ClaimsPrincipal> TransformAsync(ClaimsPrincipal principal)
    {
        if (principal.Identity is not ClaimsIdentity identity ||
            !identity.IsAuthenticated)
        {
            return principal;
        }

        // Local login already resolved its role from the database.
        if (principal.HasClaim(RoleSourceClaim, "LocalDatabase"))
        {
            return principal;
        }

        var email =
            principal.FindFirstValue(ClaimTypes.Email) ??
            principal.FindFirstValue("email");

        if (string.IsNullOrWhiteSpace(email))
        {
            return principal;
        }

        var user = await _dbContext.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(
                u => u.Email == email,
                CancellationToken.None);

        if (user is null)
        {
            // Keep the authentication valid but do not grant an application role.
            // Controllers protected by [Authorize(Roles = "Admin")] will therefore
            // not become accessible without a mapped local application role.
            return principal;
        }

        var existingRoles = identity
            .FindAll(ClaimTypes.Role)
            .ToList();

        foreach (var roleClaim in existingRoles)
        {
            identity.RemoveClaim(roleClaim);
        }

        identity.AddClaim(
            new Claim(
                ClaimTypes.Role,
                user.Role.ToString()));

        var existingName = identity.FindFirst(ClaimTypes.Name);
        if (existingName is null || string.IsNullOrWhiteSpace(existingName.Value))
        {
            identity.AddClaim(
                new Claim(
                    ClaimTypes.Name,
                    string.IsNullOrWhiteSpace(user.Username)
                        ? user.Email
                        : user.Username));
        }

        if (!identity.HasClaim(c => c.Type == "FullName"))
        {
            identity.AddClaim(
                new Claim(
                    "FullName",
                    user.NamaLengkap ?? string.Empty));
        }

        identity.AddClaim(
            new Claim(
                RoleSourceClaim,
                "LocalDatabase"));

        return principal;
    }
}
