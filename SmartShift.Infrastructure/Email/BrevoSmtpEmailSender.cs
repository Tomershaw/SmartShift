using Microsoft.Extensions.Configuration;
using SmartShift.Infrastructure.Interfaces;
using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;

namespace SmartShift.Infrastructure.Email;

public sealed class BrevoSmtpEmailSender : IEmailSender
{
    private readonly IConfiguration _configuration;

    public BrevoSmtpEmailSender(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public async Task SendAsync(string toEmail, string subject, string htmlContent)
    {
        var host = _configuration["Brevo:SmtpHost"] ?? "smtp-relay.brevo.com";
        var portValue = _configuration["Brevo:SmtpPort"] ?? "587";
        var login = _configuration["Brevo:SmtpLogin"];
        var password = _configuration["Brevo:SmtpKey"];
        var fromEmail = _configuration["Brevo:FromEmail"];
        var fromName = _configuration["Brevo:FromName"] ?? "SmartShift";

        if (!int.TryParse(portValue, out var port))
            throw new InvalidOperationException("Brevo SmtpPort is invalid");

        if (string.IsNullOrWhiteSpace(login))
            throw new InvalidOperationException("Brevo SmtpLogin is missing");

        if (string.IsNullOrWhiteSpace(password))
            throw new InvalidOperationException("Brevo SmtpKey is missing");

        if (string.IsNullOrWhiteSpace(fromEmail))
            throw new InvalidOperationException("Brevo FromEmail is missing");

        var message = new MimeMessage();
        message.From.Add(new MailboxAddress(fromName, fromEmail));
        message.To.Add(MailboxAddress.Parse(toEmail));
        message.Subject = subject;
        message.Body = new BodyBuilder
        {
            HtmlBody = htmlContent
        }.ToMessageBody();

        using var client = new SmtpClient();

        var secureOption = port == 465
            ? SecureSocketOptions.SslOnConnect
            : SecureSocketOptions.StartTls;

        await client.ConnectAsync(host, port, secureOption);
        await client.AuthenticateAsync(login, password);
        await client.SendAsync(message);
        await client.DisconnectAsync(true);
    }
}
