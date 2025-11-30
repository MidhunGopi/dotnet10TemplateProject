using DotNet10Template.Application.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Net;
using System.Net.Mail;

namespace DotNet10Template.Infrastructure.Services;

/// <summary>
/// Email service implementation using SMTP
/// </summary>
public class EmailService : IEmailService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<EmailService> _logger;

    public EmailService(IConfiguration configuration, ILogger<EmailService> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    public async Task SendEmailAsync(string to, string subject, string body, bool isHtml = true, CancellationToken cancellationToken = default)
    {
        await SendEmailAsync(new[] { to }, subject, body, isHtml, cancellationToken);
    }

    public async Task SendEmailAsync(IEnumerable<string> to, string subject, string body, bool isHtml = true, CancellationToken cancellationToken = default)
    {
        try
        {
            var smtpHost = _configuration["Email:SmtpHost"] ?? "localhost";
            var smtpPort = int.Parse(_configuration["Email:SmtpPort"] ?? "587");
            var smtpUser = _configuration["Email:SmtpUser"];
            var smtpPass = _configuration["Email:SmtpPassword"];
            var fromEmail = _configuration["Email:FromEmail"] ?? "noreply@dotnet10template.com";
            var fromName = _configuration["Email:FromName"] ?? "DotNet10 Template";
            var enableSsl = bool.Parse(_configuration["Email:EnableSsl"] ?? "true");

            using var client = new SmtpClient(smtpHost, smtpPort)
            {
                EnableSsl = enableSsl,
                DeliveryMethod = SmtpDeliveryMethod.Network
            };

            if (!string.IsNullOrEmpty(smtpUser) && !string.IsNullOrEmpty(smtpPass))
            {
                client.Credentials = new NetworkCredential(smtpUser, smtpPass);
            }

            using var message = new MailMessage
            {
                From = new MailAddress(fromEmail, fromName),
                Subject = subject,
                Body = body,
                IsBodyHtml = isHtml
            };

            foreach (var recipient in to)
            {
                message.To.Add(recipient);
            }

            await client.SendMailAsync(message, cancellationToken);

            _logger.LogInformation("Email sent successfully to {Recipients}", string.Join(", ", to));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send email to {Recipients}", string.Join(", ", to));
            throw;
        }
    }

    public async Task SendEmailWithAttachmentAsync(string to, string subject, string body, byte[] attachment, string attachmentName, CancellationToken cancellationToken = default)
    {
        try
        {
            var smtpHost = _configuration["Email:SmtpHost"] ?? "localhost";
            var smtpPort = int.Parse(_configuration["Email:SmtpPort"] ?? "587");
            var smtpUser = _configuration["Email:SmtpUser"];
            var smtpPass = _configuration["Email:SmtpPassword"];
            var fromEmail = _configuration["Email:FromEmail"] ?? "noreply@dotnet10template.com";
            var fromName = _configuration["Email:FromName"] ?? "DotNet10 Template";
            var enableSsl = bool.Parse(_configuration["Email:EnableSsl"] ?? "true");

            using var client = new SmtpClient(smtpHost, smtpPort)
            {
                EnableSsl = enableSsl,
                DeliveryMethod = SmtpDeliveryMethod.Network
            };

            if (!string.IsNullOrEmpty(smtpUser) && !string.IsNullOrEmpty(smtpPass))
            {
                client.Credentials = new NetworkCredential(smtpUser, smtpPass);
            }

            using var message = new MailMessage
            {
                From = new MailAddress(fromEmail, fromName),
                Subject = subject,
                Body = body,
                IsBodyHtml = true
            };

            message.To.Add(to);

            using var stream = new MemoryStream(attachment);
            message.Attachments.Add(new Attachment(stream, attachmentName));

            await client.SendMailAsync(message, cancellationToken);

            _logger.LogInformation("Email with attachment sent successfully to {Recipient}", to);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send email with attachment to {Recipient}", to);
            throw;
        }
    }
}
