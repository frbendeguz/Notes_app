namespace Notes.Api.Errors;

/// <summary>404: the resource does not exist OR belongs to someone else (design V1).</summary>
public sealed class NotFoundException(string message) : Exception(message);
