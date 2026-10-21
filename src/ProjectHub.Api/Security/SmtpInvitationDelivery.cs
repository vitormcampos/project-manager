using System.Net;
using System.Net.Mail;
using ProjectHub.Application.Contracts;

namespace ProjectHub.Api.Security;

/// <summary>Delivers activation links through a configured SMTP server.</summary>
public sealed class SmtpInvitationDelivery(IConfiguration configuration) : IInvitationDelivery
{
    /// <inheritdoc />
    public async Task SendAsync(
        string email,
        string firstName,
        string rawToken,
        CancellationToken cancellationToken
    )
    {
        var settings =
            configuration.GetSection("InvitationDelivery").Get<InvitationDeliveryOptions>()
            ?? new InvitationDeliveryOptions();
        if (
            string.IsNullOrWhiteSpace(settings.Host)
            || string.IsNullOrWhiteSpace(settings.FromEmail)
            || string.IsNullOrWhiteSpace(settings.WebBaseUrl)
            || settings.Port is < 1 or > 65535
        )
        {
            throw new TimeoutException("Invitation delivery is not configured.");
        }

        try
        {
            using var message = new MailMessage(
                new MailAddress(settings.FromEmail, settings.FromName),
                new MailAddress(email)
            )
            {
                Subject = "Activate your ProjectHub account",
                Body =
                    $"Hello {firstName},\n\nActivate your ProjectHub account using this one-time link:\n"
                    + $"{settings.WebBaseUrl.TrimEnd('/')}/accept-invitation?token={Uri.EscapeDataString(rawToken)}\n\n"
                    + "If you did not expect this invitation, you can ignore this message.",
                IsBodyHtml = false,
            };

            using var client = new SmtpClient(settings.Host, settings.Port)
            {
                EnableSsl = true,
                DeliveryMethod = SmtpDeliveryMethod.Network,
            };

            if (!string.IsNullOrWhiteSpace(settings.Username))
            {
                client.Credentials = new NetworkCredential(settings.Username, settings.Password);
            }

            await client.SendMailAsync(message, cancellationToken);
        }
        catch (Exception exception)
            when (exception is SmtpException or FormatException or InvalidOperationException)
        {
            throw new TimeoutException("Invitation delivery failed.", exception);
        }
    }
}
