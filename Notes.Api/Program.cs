using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using Notes.Api.Data;
using Notes.Api.Data.Entities;
using Notes.Api.Errors;
using Notes.Api.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("Default")));

// Errors: services throw, ApiExceptionHandler writes ProblemDetails (design 5).
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<ApiExceptionHandler>();

builder.Services.AddControllers()
    .ConfigureApiBehaviorOptions(options =>
    {
        // DTO fields are nullable and validated by the services. Model binding can still fail
        // (e.g. malformed JSON): answer in the same ProblemDetails shape, with a Hungarian message.
        options.InvalidModelStateResponseFactory = context =>
        {
            var errors = context.ModelState
                .Where(e => e.Value?.Errors.Count > 0)
                .ToDictionary(e => string.IsNullOrEmpty(e.Key) ? "body" : e.Key,
                              _ => new[] { "A kérés formátuma érvénytelen." });
            return new BadRequestObjectResult(new ValidationProblemDetails(errors)
            {
                Status = StatusCodes.Status400BadRequest,
                Detail = "A kérés formátuma érvénytelen.",
            });
        };
    });
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

// JWT settings: validated at startup, so a missing or short Jwt:Key stops the application (design 3).
builder.Services.AddOptions<JwtOptions>()
    .BindConfiguration(JwtOptions.SectionName)
    .ValidateOnStart();
builder.Services.AddSingleton<IValidateOptions<JwtOptions>, JwtOptionsValidator>();

builder.Services.AddScoped<IPasswordHasher<User>, PasswordHasher<User>>();
builder.Services.AddScoped<AuthService>();

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer();

// Deferred options binding: the Jwt values are NOT read here from builder.Configuration, but when the
// JwtBearerOptions are first resolved, from IOptions<JwtOptions>. This way configuration added later by the
// test host (TestApiFactory: UseSetting("Jwt:Key", ...)) is also seen, and the key lives in one place.
builder.Services.AddOptions<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme)
    .Configure<IOptions<JwtOptions>>((options, jwtOptions) =>
    {
        var jwt = jwtOptions.Value;
        // Keep the JWT claim names ("sub", "name") instead of mapping them to the long .NET claim types.
        options.MapInboundClaims = false;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = jwt.Issuer,
            ValidateAudience = true,
            ValidAudience = jwt.Audience,
            ValidateLifetime = true,
            ClockSkew = TimeSpan.Zero, // the token is valid exactly ExpiresMinutes (V9), no extra tolerance
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.Key)),
            ValidAlgorithms = [SecurityAlgorithms.HmacSha256],
            NameClaimType = JwtRegisteredClaimNames.Name,
        };
        options.Events = new JwtBearerEvents
        {
            // Missing/invalid/expired token: 401 as ProblemDetails instead of an empty body.
            OnChallenge = async context =>
            {
                context.HandleResponse();
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                await WriteProblemAsync(context.HttpContext, StatusCodes.Status401Unauthorized,
                    "Bejelentkezés szükséges, vagy a munkamenet lejárt.");
            },
            OnForbidden = context =>
            {
                context.Response.StatusCode = StatusCodes.Status403Forbidden;
                return WriteProblemAsync(context.HttpContext, StatusCodes.Status403Forbidden,
                    "Nincs jogosultságod ehhez a művelethez.");
            },
        };
    });

// Every endpoint requires a logged-in user unless it is marked [AllowAnonymous] (auth endpoints).
builder.Services.AddAuthorizationBuilder()
    .SetFallbackPolicy(new AuthorizationPolicyBuilder().RequireAuthenticatedUser().Build());

var app = builder.Build();

app.UseExceptionHandler();
app.UseStatusCodePages(); // bodiless error responses (e.g. 404 unknown route) also become ProblemDetails

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi().AllowAnonymous();
}
else
{
    // V14: in development the Vite proxy calls the backend over HTTP, a redirect would break it.
    app.UseHttpsRedirection();
}

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();

static Task WriteProblemAsync(HttpContext httpContext, int status, string detail) =>
    httpContext.RequestServices.GetRequiredService<IProblemDetailsService>().WriteAsync(new ProblemDetailsContext
    {
        HttpContext = httpContext,
        ProblemDetails = new ProblemDetails { Status = status, Detail = detail },
    }).AsTask();

// Makes the implicit Program class visible to WebApplicationFactory<Program> in Notes.Tests.
public partial class Program { }
