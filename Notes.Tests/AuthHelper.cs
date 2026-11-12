using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Notes.Api.Dtos;

namespace Notes.Tests;

/// <summary>A registered test user and an HttpClient that sends its Bearer token.</summary>
public sealed record AuthenticatedUser(HttpClient Client, string UserName, string Password, string Token);

public static class AuthHelper
{
    public const string DefaultPassword = "Jelszo123";

    /// <summary>Unique user name, so tests sharing the fixture database don't collide.</summary>
    public static string UniqueUserName() => "u" + Guid.NewGuid().ToString("N")[..12];

    /// <summary>Registers a new unique user and returns a client with its Bearer token set.</summary>
    public static async Task<AuthenticatedUser> CreateUserAsync(TestApiFactory factory, string? userName = null)
    {
        userName ??= UniqueUserName();
        var client = factory.CreateClient();
        var response = await client.PostAsJsonAsync("/api/auth/register", new AuthRequest(userName, DefaultPassword));
        if (response.StatusCode != HttpStatusCode.Created)
        {
            throw new InvalidOperationException(
                $"Test registration failed: {(int)response.StatusCode} {await response.Content.ReadAsStringAsync()}");
        }
        var auth = (await response.Content.ReadFromJsonAsync<AuthResponse>())!;
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", auth.Token);
        return new AuthenticatedUser(client, auth.UserName, DefaultPassword, auth.Token);
    }
}
