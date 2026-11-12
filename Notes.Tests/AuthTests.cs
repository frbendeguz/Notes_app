using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using Notes.Api;
using Notes.Api.Data;
using Notes.Api.Dtos;
using Notes.Api.Services;

namespace Notes.Tests;

/// <summary>Registration, login, JWT and the ProblemDetails error format (task 02; design 3, 5, 6).</summary>
public sealed class AuthTests(TestApiFactory factory) : IClassFixture<TestApiFactory>
{
    private const string ProblemJson = "application/problem+json";

    // ---------- Registration ----------

    [Fact]
    public async Task Register_ValidData_Returns201WithTokenThatWorksOnMe()
    {
        var userName = AuthHelper.UniqueUserName();
        var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/auth/register", new AuthRequest(userName, "Jelszo123"));

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var auth = await response.Content.ReadFromJsonAsync<AuthResponse>();
        Assert.False(string.IsNullOrEmpty(auth!.Token));
        Assert.Equal(userName, auth.UserName);

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", auth.Token);
        var me = await client.GetAsync("/api/auth/me");
        Assert.Equal(HttpStatusCode.OK, me.StatusCode);
        var meBody = await me.Content.ReadFromJsonAsync<MeResponse>();
        Assert.Equal(userName, meBody!.UserName);
        Assert.Equal(await GetUserIdAsync(userName), meBody.Id);
    }

    [Fact]
    public async Task Register_UserNameWithSurroundingSpaces_StoredTrimmedWithOriginalCasing()
    {
        var userName = "Ab" + AuthHelper.UniqueUserName();
        var response = await Register("  " + userName + "  ", "Jelszo123");

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.Equal(userName, (await response.Content.ReadFromJsonAsync<AuthResponse>())!.UserName);
        await using var db = NewContext();
        var user = await db.Users.SingleAsync(u => u.NormalizedUserName == Text.Normalize(userName));
        Assert.Equal(userName, user.UserName);
    }

    [Theory]
    [InlineData(false)] // same name, different letter case
    [InlineData(true)]  // same name, surrounded by spaces
    public async Task Register_UserNameTaken_Returns409AndNoSecondUser(bool withSpaces)
    {
        var userName = AuthHelper.UniqueUserName();
        Assert.Equal(HttpStatusCode.Created, (await Register(userName, "Jelszo123")).StatusCode);

        var second = withSpaces ? " " + userName + " " : userName.ToUpperInvariant();
        var response = await Register(second, "Masik1234");

        await AssertProblemAsync(response, HttpStatusCode.Conflict, AuthService.UserNameTakenMessage);
        await using var db = NewContext();
        Assert.Equal(1, await db.Users.CountAsync(u => u.NormalizedUserName == Text.Normalize(userName)));
    }

    [Theory]
    [InlineData(Limits.UserNameMin)]
    [InlineData(Limits.UserNameMax)]
    public async Task Register_UserNameBoundaryLength_Returns201(int length)
    {
        var response = await Register(UniqueOfLength(length), "Jelszo123");

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    [Theory]
    [InlineData(Limits.UserNameMin - 1)]
    [InlineData(Limits.UserNameMax + 1)]
    public async Task Register_UserNameWrongLength_Returns400WithErrorsAndUserNotCreated(int length)
    {
        var userName = UniqueOfLength(length);

        var response = await Register(userName, "Jelszo123");

        await AssertValidationErrorAsync(response, "userName", AuthService.UserNameLength);
        await AssertUserNotCreatedAsync(userName);
    }

    [Theory]
    [InlineData(" ")] // inner space
    [InlineData("@")]
    [InlineData("á")]
    [InlineData("!")]
    public async Task Register_UserNameForbiddenCharacter_Returns400WithErrorsAndUserNotCreated(string character)
    {
        var userName = AuthHelper.UniqueUserName() + character + "x";

        var response = await Register(userName, "Jelszo123");

        await AssertValidationErrorAsync(response, "userName", AuthService.UserNameCharacters);
        await AssertUserNotCreatedAsync(userName);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task Register_UserNameMissing_Returns400WithErrorsAndNoToken(string? userName)
    {
        var response = await Register(userName, "Jelszo123");

        await AssertValidationErrorAsync(response, "userName", AuthService.UserNameRequired);
        Assert.DoesNotContain("token", await response.Content.ReadAsStringAsync());
    }

    [Theory]
    [InlineData("{bad json")]
    [InlineData("")]
    public async Task Register_MalformedBody_Returns400ProblemDetailsAndUserNotCreated(string body)
    {
        var before = await CountUsersAsync();

        var response = await factory.CreateClient().PostAsync("/api/auth/register",
            new StringContent(body, Encoding.UTF8, "application/json"));

        await AssertProblemAsync(response, HttpStatusCode.BadRequest, "A kérés formátuma érvénytelen.");
        Assert.Equal(before, await CountUsersAsync());
    }

    // ---------- Password rules (V5) ----------

    [Theory]
    [InlineData(Limits.PasswordMin)]
    [InlineData(Limits.PasswordMax)]
    public async Task Register_PasswordBoundaryLength_Returns201(int length)
    {
        var response = await Register(AuthHelper.UniqueUserName(), PasswordOfLength(length));

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    [Theory]
    [InlineData("Árvíz123")] // accented upper-case letter counts as upper case
    [InlineData("Abcdefg1")] // no special character: allowed
    [InlineData("Ab1 !?#ő")] // special characters and space are allowed
    public async Task Register_PasswordValid_Returns201(string password)
    {
        var response = await Register(AuthHelper.UniqueUserName(), password);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    [Theory]
    [InlineData(Limits.PasswordMin - 1)]
    [InlineData(Limits.PasswordMax + 1)]
    public async Task Register_PasswordWrongLength_Returns400AndUserNotCreated(int length)
    {
        var userName = AuthHelper.UniqueUserName();

        var response = await Register(userName, PasswordOfLength(length));

        var messages = await AssertValidationErrorAsync(response, "password", AuthService.PasswordLength);
        Assert.Single(messages);
        await AssertUserNotCreatedAsync(userName);
    }

    [Theory]
    [InlineData("ABCDEFG1", AuthService.PasswordLower)]
    [InlineData("abcdefg1", AuthService.PasswordUpper)]
    [InlineData("Abcdefgh", AuthService.PasswordDigit)]
    public async Task Register_PasswordMissingCharacterClass_Returns400AndUserNotCreated(string password, string expected)
    {
        var userName = AuthHelper.UniqueUserName();

        var response = await Register(userName, password);

        var messages = await AssertValidationErrorAsync(response, "password", expected);
        Assert.Single(messages);
        await AssertUserNotCreatedAsync(userName);
    }

    [Fact]
    public async Task Register_PasswordBreaksMultipleRules_EachRuleSeparateMessage()
    {
        var userName = AuthHelper.UniqueUserName();

        // "abc": too short, no upper-case letter, no digit.
        var response = await Register(userName, "abc");

        var messages = await AssertValidationErrorAsync(response, "password", AuthService.PasswordLength);
        Assert.Equal(
            new[] { AuthService.PasswordLength, AuthService.PasswordUpper, AuthService.PasswordDigit }.Order(),
            messages.Order());
        await AssertUserNotCreatedAsync(userName);
    }

    [Fact]
    public async Task Register_UserNameAndPasswordInvalid_BothFieldsInErrors()
    {
        var response = await Register("a@", "abc");

        await AssertValidationErrorAsync(response, "userName", AuthService.UserNameLength);
        await AssertValidationErrorAsync(response, "password", AuthService.PasswordUpper);
    }

    [Fact]
    public async Task Register_PasswordStoredHashed_HashDiffersFromPassword()
    {
        var userName = AuthHelper.UniqueUserName();
        const string password = "Jelszo123";
        Assert.Equal(HttpStatusCode.Created, (await Register(userName, password)).StatusCode);

        await using var db = NewContext();
        var user = await db.Users.SingleAsync(u => u.NormalizedUserName == Text.Normalize(userName));
        Assert.False(string.IsNullOrEmpty(user.PasswordHash));
        Assert.NotEqual(password, user.PasswordHash);
        Assert.DoesNotContain(password, user.PasswordHash);
    }

    // ---------- Login ----------

    // AC1
    [Fact]
    public async Task Login_ValidCredentials_Returns200WithToken()
    {
        var user = await AuthHelper.CreateUserAsync(factory);

        var response = await Login(user.UserName, user.Password);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var auth = await response.Content.ReadFromJsonAsync<AuthResponse>();
        Assert.False(string.IsNullOrEmpty(auth!.Token));
        Assert.Equal(user.UserName, auth.UserName);
    }

    [Fact]
    public async Task Login_UserNameDifferentCaseAndSpaces_Returns200WithStoredUserName()
    {
        var user = await AuthHelper.CreateUserAsync(factory, "Ab" + AuthHelper.UniqueUserName());

        var response = await Login(" " + user.UserName.ToUpperInvariant() + " ", user.Password);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(user.UserName, (await response.Content.ReadFromJsonAsync<AuthResponse>())!.UserName);
    }

    // AC1
    [Fact]
    public async Task Login_WrongPassword_Returns401WithoutToken()
    {
        var user = await AuthHelper.CreateUserAsync(factory);

        var response = await Login(user.UserName, user.Password + "x");

        var body = await AssertProblemAsync(response, HttpStatusCode.Unauthorized, AuthService.LoginFailedMessage);
        Assert.False(body.TryGetProperty("token", out _));
    }

    // AC1
    [Fact]
    public async Task Login_UnknownUser_Returns401WithSameMessageAsWrongPassword()
    {
        var user = await AuthHelper.CreateUserAsync(factory);
        var wrongPassword = await Login(user.UserName, "Rossz12345");

        var unknownUser = await Login(AuthHelper.UniqueUserName(), "Rossz12345");

        var wrongBody = await AssertProblemAsync(wrongPassword, HttpStatusCode.Unauthorized, AuthService.LoginFailedMessage);
        var unknownBody = await AssertProblemAsync(unknownUser, HttpStatusCode.Unauthorized, AuthService.LoginFailedMessage);
        Assert.Equal(wrongBody.GetProperty("detail").GetString(), unknownBody.GetProperty("detail").GetString());
        Assert.False(unknownBody.TryGetProperty("token", out _));
    }

    [Theory]
    [InlineData(null, "Jelszo123")]
    [InlineData("", "Jelszo123")]
    [InlineData("   ", "Jelszo123")]
    [InlineData("valaki", null)]
    [InlineData("valaki", "")]
    public async Task Login_MissingField_Returns401WithSameMessageAndNoToken(string? userName, string? password)
    {
        var response = await Login(userName, password);

        var body = await AssertProblemAsync(response, HttpStatusCode.Unauthorized, AuthService.LoginFailedMessage);
        Assert.False(body.TryGetProperty("token", out _));
    }

    // ---------- Token on /me ----------

    [Fact]
    public async Task Me_NoToken_Returns401ProblemDetails()
    {
        var response = await factory.CreateClient().GetAsync("/api/auth/me");

        await AssertProblemAsync(response, HttpStatusCode.Unauthorized, null);
    }

    [Theory]
    [InlineData("garbage")]
    [InlineData("wrong-key")]
    [InlineData("tampered")]
    public async Task Me_InvalidToken_Returns401(string kind)
    {
        var user = await AuthHelper.CreateUserAsync(factory);
        var token = kind switch
        {
            "garbage" => "nem.egy.token",
            "wrong-key" => CreateToken(Guid.NewGuid(), user.UserName, DateTime.UtcNow.AddMinutes(30),
                                       "another-key-that-is-long-enough-0123456789"),
            _ => TamperSignature(user.Token),
        };

        var response = await GetMe(token);

        await AssertProblemAsync(response, HttpStatusCode.Unauthorized, null);
    }

    [Fact]
    public async Task Me_ExpiredToken_Returns401()
    {
        var user = await AuthHelper.CreateUserAsync(factory);
        var token = CreateToken(await GetUserIdAsync(user.UserName), user.UserName, DateTime.UtcNow.AddMinutes(-1),
                                TestApiFactory.TestJwtKey);

        var response = await GetMe(token);

        await AssertProblemAsync(response, HttpStatusCode.Unauthorized, null);
    }

    [Fact]
    public async Task Login_Token_ExpiresAfterConfiguredMinutesAndHasNoRole()
    {
        var user = await AuthHelper.CreateUserAsync(factory);
        var before = DateTime.UtcNow;

        var auth = await (await Login(user.UserName, user.Password)).Content.ReadFromJsonAsync<AuthResponse>();

        var token = new JsonWebToken(auth!.Token);
        var minutes = factory.Services.GetRequiredService<IOptions<JwtOptions>>().Value.ExpiresMinutes;
        Assert.Equal(60, minutes); // V9
        Assert.InRange(token.ValidTo, before.AddMinutes(minutes).AddSeconds(-2), DateTime.UtcNow.AddMinutes(minutes).AddSeconds(2));
        Assert.Equal((await GetUserIdAsync(user.UserName)).ToString(), token.Subject);
        Assert.DoesNotContain(token.Claims, c => c.Type is "role" or ClaimTypes.Role);
    }

    // ---------- Startup ----------

    [Theory]
    [InlineData("")]
    [InlineData("too-short-key")]
    public void Startup_JwtKeyMissingOrTooShort_Throws(string key)
    {
        using var noKey = factory.WithWebHostBuilder(b => b.UseSetting("Jwt:Key", key));

        var ex = Assert.ThrowsAny<Exception>(() => noKey.CreateClient());

        Assert.Contains("Jwt:Key", ex.ToString());
    }

    // ---------- Helpers ----------

    private Task<HttpResponseMessage> Register(string? userName, string? password) =>
        factory.CreateClient().PostAsJsonAsync("/api/auth/register", new AuthRequest(userName, password));

    private Task<HttpResponseMessage> Login(string? userName, string? password) =>
        factory.CreateClient().PostAsJsonAsync("/api/auth/login", new AuthRequest(userName, password));

    private Task<HttpResponseMessage> GetMe(string token)
    {
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return client.GetAsync("/api/auth/me");
    }

    /// <summary>Checks status, ProblemDetails content type and (if given) the Hungarian detail.</summary>
    private static async Task<JsonElement> AssertProblemAsync(HttpResponseMessage response, HttpStatusCode status, string? detail)
    {
        Assert.Equal(status, response.StatusCode);
        Assert.Equal(ProblemJson, response.Content.Headers.ContentType?.MediaType);
        // Read as string: a response can be asserted more than once (ReadFromJsonAsync consumes the stream).
        var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync()).RootElement;
        Assert.Equal((int)status, body.GetProperty("status").GetInt32());
        var actualDetail = body.GetProperty("detail").GetString();
        Assert.False(string.IsNullOrEmpty(actualDetail));
        if (detail is not null) Assert.Equal(detail, actualDetail);
        return body;
    }

    /// <summary>400 ProblemDetails whose errors[field] contains the expected message; returns all messages of the field.</summary>
    private static async Task<string[]> AssertValidationErrorAsync(HttpResponseMessage response, string field, string expected)
    {
        var body = await AssertProblemAsync(response, HttpStatusCode.BadRequest, "A megadott adatok érvénytelenek.");
        var messages = body.GetProperty("errors").GetProperty(field).EnumerateArray().Select(e => e.GetString()!).ToArray();
        Assert.Contains(expected, messages);
        return messages;
    }

    private async Task AssertUserNotCreatedAsync(string userName)
    {
        await using var db = NewContext();
        Assert.False(await db.Users.AnyAsync(u => u.NormalizedUserName == Text.Normalize(userName)));
    }

    private async Task<int> CountUsersAsync()
    {
        await using var db = NewContext();
        return await db.Users.CountAsync();
    }

    private async Task<Guid> GetUserIdAsync(string userName)
    {
        await using var db = NewContext();
        return await db.Users.Where(u => u.NormalizedUserName == Text.Normalize(userName)).Select(u => u.Id).SingleAsync();
    }

    private AppDbContext NewContext() =>
        factory.Services.CreateScope().ServiceProvider.GetRequiredService<AppDbContext>();

    /// <summary>Signs a token like the API does, but with a chosen key and expiry (for invalid / expired cases).</summary>
    private string CreateToken(Guid userId, string userName, DateTime expires, string key)
    {
        var jwt = factory.Services.GetRequiredService<IOptions<JwtOptions>>().Value;
        return new JsonWebTokenHandler().CreateToken(new SecurityTokenDescriptor
        {
            Issuer = jwt.Issuer,
            Audience = jwt.Audience,
            IssuedAt = expires.AddMinutes(-jwt.ExpiresMinutes),
            NotBefore = expires.AddMinutes(-jwt.ExpiresMinutes),
            Expires = expires,
            Subject = new ClaimsIdentity([
                new Claim(JwtRegisteredClaimNames.Sub, userId.ToString()),
                new Claim(JwtRegisteredClaimNames.Name, userName),
            ]),
            SigningCredentials = new SigningCredentials(
                new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key)), SecurityAlgorithms.HmacSha256),
        });
    }

    /// <summary>Changes the first character of the signature (all 6 bits of it are significant).</summary>
    private static string TamperSignature(string token)
    {
        var at = token.LastIndexOf('.') + 1;
        return token[..at] + (token[at] == 'A' ? 'B' : 'A') + token[(at + 1)..];
    }

    /// <summary>Unique user name of exactly the given length (hex digits of two Guids).</summary>
    private static string UniqueOfLength(int length) =>
        ("u" + Guid.NewGuid().ToString("N") + Guid.NewGuid().ToString("N"))[..length];

    /// <summary>Password of exactly the given length that satisfies the character-class rules.</summary>
    private static string PasswordOfLength(int length) => ("Aa1" + new string('x', Math.Max(0, length - 3)))[..length];
}
