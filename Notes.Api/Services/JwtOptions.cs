using System.Text;
using Microsoft.Extensions.Options;

namespace Notes.Api.Services;

/// <summary>
/// The "Jwt" configuration section. Issuer, Audience and ExpiresMinutes come from appsettings.json;
/// Key is a secret (user-secrets in development, environment variable Jwt__Key otherwise), never in the repo.
/// </summary>
public sealed class JwtOptions
{
    public const string SectionName = "Jwt";
    public const int KeyMinBytes = 32;

    public string Issuer { get; set; } = "";
    public string Audience { get; set; } = "";
    public int ExpiresMinutes { get; set; }
    public string Key { get; set; } = "";
}

/// <summary>Validated at startup (ValidateOnStart): a missing or too short key stops the application.</summary>
public sealed class JwtOptionsValidator : IValidateOptions<JwtOptions>
{
    public ValidateOptionsResult Validate(string? name, JwtOptions options)
    {
        var failures = new List<string>();
        if (string.IsNullOrWhiteSpace(options.Issuer)) failures.Add("Jwt:Issuer is missing.");
        if (string.IsNullOrWhiteSpace(options.Audience)) failures.Add("Jwt:Audience is missing.");
        if (options.ExpiresMinutes <= 0) failures.Add("Jwt:ExpiresMinutes must be positive.");
        if (Encoding.UTF8.GetByteCount(options.Key ?? "") < JwtOptions.KeyMinBytes)
        {
            failures.Add($"Jwt:Key is missing or shorter than {JwtOptions.KeyMinBytes} bytes. " +
                         "Set it with: dotnet user-secrets set \"Jwt:Key\" \"<value>\" --project Notes.Api");
        }
        return failures.Count == 0 ? ValidateOptionsResult.Success : ValidateOptionsResult.Fail(failures);
    }
}
