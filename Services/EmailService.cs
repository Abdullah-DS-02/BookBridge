using BookBridge.Services.Interfaces;
using MailKit.Net.Smtp;
using MimeKit;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace BookBridge.Services;

public class EmailService : IEmailService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<EmailService> _logger;

    public EmailService(IConfiguration configuration, ILogger<EmailService> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    public async Task SendEmailAsync(string toEmail, string subject, string htmlMessage)
    {
        try
        {
            var emailSettings = _configuration.GetSection("Email");
            var host = emailSettings["Host"] ?? "smtp.gmail.com";
            var portStr = emailSettings["Port"] ?? "587";
            int port = 587;
            int.TryParse(portStr, out port);

            var username = emailSettings["Username"];
            var password = emailSettings["Password"];
            var fromName = emailSettings["FromName"] ?? "BookBridge";

            // If SMTP username is default placeholder, skip actual sending and output to log
            if (string.IsNullOrEmpty(username) || username.Contains("your@email.com") || string.IsNullOrEmpty(password) || password.Contains("your-app-password"))
            {
                _logger.LogWarning("SMTP Settings not configured properly. Skipping real email sending.");
                _logger.LogInformation($"[DEV EMAIL FALLBACK] To: {toEmail}\nSubject: {subject}\nContent: {htmlMessage}");
                throw new InvalidOperationException("SMTP credentials not configured.");
            }

            var message = new MimeMessage();
            message.From.Add(new MailboxAddress(fromName, username));
            message.To.Add(new MailboxAddress("", toEmail));
            message.Subject = subject;

            var bodyBuilder = new BodyBuilder { HtmlBody = htmlMessage };
            message.Body = bodyBuilder.ToMessageBody();

            using var client = new SmtpClient();
            
            // Bypass SSL certificate validation for development
            client.ServerCertificateValidationCallback = (s, c, h, e) => true;

            await client.ConnectAsync(host, port, MailKit.Security.SecureSocketOptions.StartTls);
            await client.AuthenticateAsync(username, password);
            await client.SendAsync(message);
            await client.DisconnectAsync(true);
            
            _logger.LogInformation($"Verification email successfully sent to {toEmail}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Failed to send email to {toEmail}. Error: {ex.Message}");
            // Re-throw so that controller can handle dev-mode fallback display if SMTP fails
            throw;
        }
    }
}
