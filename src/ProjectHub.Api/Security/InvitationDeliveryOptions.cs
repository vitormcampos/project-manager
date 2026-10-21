namespace ProjectHub.Api.Security;

/// <summary>SMTP and frontend settings required to deliver user invitations.</summary>
public sealed class InvitationDeliveryOptions
{
    /// <summary>Gets or sets the SMTP host.</summary>
    public string Host { get; set; } = string.Empty;

    /// <summary>Gets or sets the SMTP port.</summary>
    public int Port { get; set; } = 587;

    /// <summary>Gets or sets the SMTP username.</summary>
    public string Username { get; set; } = string.Empty;

    /// <summary>Gets or sets the SMTP password.</summary>
    public string Password { get; set; } = string.Empty;

    /// <summary>Gets or sets the sender email address.</summary>
    public string FromEmail { get; set; } = string.Empty;

    /// <summary>Gets or sets the sender display name.</summary>
    public string FromName { get; set; } = "ProjectHub";

    /// <summary>Gets or sets the frontend base URL used to build an activation link.</summary>
    public string WebBaseUrl { get; set; } = string.Empty;
}
