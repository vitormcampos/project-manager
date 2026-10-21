namespace ProjectHub.Domain.Models;

/// <summary>Defines the system-wide classification of an account.</summary>
public enum AccountRole
{
    /// <summary>Platform administration account; all its users have system-wide access.</summary>
    Admin,

    /// <summary>Conventional tenant account.</summary>
    User,
}

/// <summary>Defines a user's permissions within the associated account.</summary>
public enum UserRole
{
    /// <summary>Owner and administrator of a conventional account.</summary>
    Admin,

    /// <summary>Conventional account member.</summary>
    User,
}

/// <summary>Defines the lifecycle state of a user.</summary>
public enum UserStatus
{
    /// <summary>Invited but not yet activated.</summary>
    Pending,

    /// <summary>Activated and able to authenticate.</summary>
    Active,

    /// <summary>Disabled and unable to authenticate.</summary>
    Disabled,
}
