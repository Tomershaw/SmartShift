namespace SmartShift.Infrastructure.Interfaces;

public interface IEmailSender
{
    Task SendAsync(string toEmail, string subject, string htmlContent);
}
