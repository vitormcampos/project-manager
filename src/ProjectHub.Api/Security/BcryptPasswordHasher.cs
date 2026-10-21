using BCrypt.Net;
using ProjectHub.Application.Contracts;
using BC = BCrypt.Net.BCrypt;

namespace ProjectHub.Api.Security;

/// <summary>Hashes passwords using BCrypt with enhanced entropy and a fixed work factor.</summary>
public sealed class BcryptPasswordHasher : IPasswordHasher
{
    private const int WorkFactor = 12;

    /// <inheritdoc />
    public string Hash(string password)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(password);
        return BC.EnhancedHashPassword(password, WorkFactor, HashType.SHA384);
    }

    /// <inheritdoc />
    public bool Verify(string passwordHash, string password)
    {
        if (string.IsNullOrWhiteSpace(passwordHash) || string.IsNullOrEmpty(password))
        {
            return false;
        }

        try
        {
            return BC.EnhancedVerify(password, passwordHash, HashType.SHA384);
        }
        catch (SaltParseException)
        {
            return false;
        }
    }
}
