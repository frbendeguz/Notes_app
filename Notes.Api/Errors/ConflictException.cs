namespace Notes.Api.Errors;

/// <summary>409: duplicate (user name, folder name, repeated share).</summary>
public sealed class ConflictException(string message) : Exception(message);
