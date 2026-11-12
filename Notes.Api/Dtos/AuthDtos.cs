namespace Notes.Api.Dtos;

/// <summary>Body of register and login. Fields are nullable: the service validates (design 5).</summary>
public sealed record AuthRequest(string? UserName, string? Password);

public sealed record AuthResponse(string Token, string UserName);

public sealed record MeResponse(Guid Id, string UserName);
