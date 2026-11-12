using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Identity;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using Notes.Api.Data;
using Notes.Api.Data.Entities;
using Notes.Api.Dtos;
using Notes.Api.Errors;

namespace Notes.Api.Services;

/// <summary>Registration, login and JWT issuing (design 3, 6; V5, V6, V9, V11).</summary>
public sealed class AuthService(AppDbContext db, IPasswordHasher<User> passwordHasher, IOptions<JwtOptions> jwtOptions)
{
    public const string LoginFailedMessage = "Hibás felhasználónév vagy jelszó.";
    public const string UserNameTakenMessage = "Ez a felhasználónév már foglalt.";

    public const string UserNameRequired = "A felhasználónév megadása kötelező.";
    public static readonly string UserNameLength =
        $"A felhasználónév {Limits.UserNameMin}–{Limits.UserNameMax} karakter hosszú lehet.";
    public const string UserNameCharacters =
        "A felhasználónév csak angol betűt, számjegyet és a _ . - karaktereket tartalmazhatja.";

    public const string PasswordRequired = "A jelszó megadása kötelező.";
    public static readonly string PasswordLength =
        $"A jelszó {Limits.PasswordMin}–{Limits.PasswordMax} karakter hosszú lehet.";
    public const string PasswordLower = "A jelszónak legalább egy kisbetűt kell tartalmaznia.";
    public const string PasswordUpper = "A jelszónak legalább egy nagybetűt kell tartalmaznia.";
    public const string PasswordDigit = "A jelszónak legalább egy számjegyet kell tartalmaznia.";

    public async Task<AuthResponse> RegisterAsync(AuthRequest request, CancellationToken ct)
    {
        // Validation runs before any database change (design 6).
        var errors = new Dictionary<string, string[]>();
        var userNameErrors = ValidateUserName(request.UserName);
        if (userNameErrors.Count > 0) errors["userName"] = [.. userNameErrors];
        var passwordErrors = ValidatePassword(request.Password);
        if (passwordErrors.Count > 0) errors["password"] = [.. passwordErrors];
        if (errors.Count > 0) throw new ValidationFailedException(errors);

        var userName = request.UserName!.Trim();
        var normalized = Text.Normalize(userName);
        if (await db.Users.AnyAsync(u => u.NormalizedUserName == normalized, ct))
        {
            throw new ConflictException(UserNameTakenMessage);
        }

        var user = new User
        {
            Id = Guid.NewGuid(),
            UserName = userName,
            NormalizedUserName = normalized,
            CreatedAt = DateTime.UtcNow,
        };
        user.PasswordHash = passwordHasher.HashPassword(user, request.Password!);
        db.Users.Add(user);

        try
        {
            await db.SaveChangesAsync(ct);
        }
        catch (DbUpdateException ex) when (ex.InnerException is SqliteException { SqliteExtendedErrorCode: 2067 })
        {
            // Unique index hit by a concurrent registration of the same name (SQLITE_CONSTRAINT_UNIQUE).
            throw new ConflictException(UserNameTakenMessage);
        }

        return new AuthResponse(CreateToken(user), user.UserName);
    }

    public async Task<AuthResponse> LoginAsync(AuthRequest request, CancellationToken ct)
    {
        // Missing/empty fields and unknown users get the same 401 as a wrong password (developer decision).
        if (string.IsNullOrWhiteSpace(request.UserName) || string.IsNullOrEmpty(request.Password))
        {
            throw new UnauthorizedException(LoginFailedMessage);
        }

        var normalized = Text.Normalize(request.UserName);
        var user = await db.Users.SingleOrDefaultAsync(u => u.NormalizedUserName == normalized, ct);
        if (user is null
            || passwordHasher.VerifyHashedPassword(user, user.PasswordHash, request.Password)
                == PasswordVerificationResult.Failed)
        {
            throw new UnauthorizedException(LoginFailedMessage);
        }

        return new AuthResponse(CreateToken(user), user.UserName);
    }

    private static List<string> ValidateUserName(string? value)
    {
        var errors = new List<string>();
        var userName = value?.Trim();
        if (string.IsNullOrEmpty(userName))
        {
            errors.Add(UserNameRequired);
            return errors;
        }
        if (userName.Length < Limits.UserNameMin || userName.Length > Limits.UserNameMax)
        {
            errors.Add(UserNameLength);
        }
        if (!userName.All(c => char.IsAsciiLetterOrDigit(c) || c is '_' or '.' or '-'))
        {
            errors.Add(UserNameCharacters);
        }
        return errors;
    }

    /// <summary>Every rule that fails gives its own message (design 6). The password is not trimmed.</summary>
    private static List<string> ValidatePassword(string? password)
    {
        var errors = new List<string>();
        if (string.IsNullOrEmpty(password))
        {
            errors.Add(PasswordRequired);
            return errors;
        }
        if (password.Length < Limits.PasswordMin || password.Length > Limits.PasswordMax)
        {
            errors.Add(PasswordLength);
        }
        if (!password.Any(char.IsLower)) errors.Add(PasswordLower);
        if (!password.Any(char.IsUpper)) errors.Add(PasswordUpper);
        if (!password.Any(char.IsAsciiDigit)) errors.Add(PasswordDigit);
        return errors;
    }

    /// <summary>HS256 token with "sub" (user id) and "name" claims only – no role (design 3).</summary>
    private string CreateToken(User user)
    {
        var options = jwtOptions.Value;
        var descriptor = new SecurityTokenDescriptor
        {
            Issuer = options.Issuer,
            Audience = options.Audience,
            Expires = DateTime.UtcNow.AddMinutes(options.ExpiresMinutes),
            Subject = new ClaimsIdentity(
            [
                new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
                new Claim(JwtRegisteredClaimNames.Name, user.UserName),
            ]),
            SigningCredentials = new SigningCredentials(
                new SymmetricSecurityKey(Encoding.UTF8.GetBytes(options.Key)), SecurityAlgorithms.HmacSha256),
        };
        return new JsonWebTokenHandler().CreateToken(descriptor);
    }
}
