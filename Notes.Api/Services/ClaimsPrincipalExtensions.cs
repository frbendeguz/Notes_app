using System.Security.Claims;
using Microsoft.IdentityModel.JsonWebTokens;

namespace Notes.Api.Services;

/// <summary>
/// Current user from the validated token. Inbound claim mapping is off (Program.cs), so the claims keep their
/// JWT names: "sub" = user id, "name" = user name (design 3).
/// </summary>
public static class ClaimsPrincipalExtensions
{
    public static Guid GetUserId(this ClaimsPrincipal user) =>
        Guid.TryParse(user.FindFirstValue(JwtRegisteredClaimNames.Sub), out var id)
            ? id
            : throw new InvalidOperationException("The authenticated user has no valid 'sub' claim.");

    public static string GetUserName(this ClaimsPrincipal user) =>
        user.FindFirstValue(JwtRegisteredClaimNames.Name)
            ?? throw new InvalidOperationException("The authenticated user has no 'name' claim.");
}
