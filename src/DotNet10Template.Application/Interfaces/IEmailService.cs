namespace DotNet10Template.Application.Interfaces;

/// <summary>
/// Interface for email service
/// </summary>
public interface IEmailService
{
    Task SendEmailAsync(string to, string subject, string body, bool isHtml = true, CancellationToken cancellationToken = default);
    Task SendEmailAsync(IEnumerable<string> to, string subject, string body, bool isHtml = true, CancellationToken cancellationToken = default);
    Task SendEmailWithAttachmentAsync(string to, string subject, string body, byte[] attachment, string attachmentName, CancellationToken cancellationToken = default);
}
