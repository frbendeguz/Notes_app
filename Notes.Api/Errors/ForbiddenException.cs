namespace Notes.Api.Errors;

/// <summary>403: the user can see the resource but their role does not allow the operation.</summary>
public sealed class ForbiddenException(string message) : Exception(message);
