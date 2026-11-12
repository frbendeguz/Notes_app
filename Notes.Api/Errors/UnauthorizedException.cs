namespace Notes.Api.Errors;

/// <summary>401: failed login (wrong password or unknown user – same message, design 3).</summary>
public sealed class UnauthorizedException(string message) : Exception(message);
