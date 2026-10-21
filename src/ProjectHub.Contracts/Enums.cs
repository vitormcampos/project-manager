using System.Text.Json.Serialization;

namespace ProjectHub.Contracts;

/// <summary>Classifies an account in the API contract.</summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum AccountRole
{
    /// <summary>Platform administration account.</summary>
    Admin,

    /// <summary>Conventional tenant account.</summary>
    User,
}

/// <summary>Defines a user's account-scoped role in the API contract.</summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum UserRole
{
    /// <summary>Account administrator.</summary>
    Admin,

    /// <summary>Conventional account member.</summary>
    User,
}

/// <summary>Defines a user's lifecycle state in the API contract.</summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum UserStatus
{
    /// <summary>Invited but not yet activated.</summary>
    Pending,

    /// <summary>Activated and able to authenticate.</summary>
    Active,

    /// <summary>Disabled and unable to authenticate.</summary>
    Disabled,
}
