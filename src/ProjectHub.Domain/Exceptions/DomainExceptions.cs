namespace ProjectHub.Domain.Exceptions;

/// <summary>Represents an invalid value rejected by a domain rule.</summary>
public sealed class DomainValidationException(string message) : ArgumentException(message);

/// <summary>Represents an operation that conflicts with the current domain state.</summary>
public sealed class DomainConflictException(string message) : InvalidOperationException(message);
