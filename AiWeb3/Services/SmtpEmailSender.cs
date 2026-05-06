using System.Net;
using System.Net.Mail;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.Extensions.Options;

public class SmtpOptions
{
    public string Host { get; set; } = default!;
    public int Port { get; set; } = 587;
    public bool EnableSsl { get; set; } = true;
    public string User { get; set; } = default!;
    public string Password { get; set; } = default!;
    public string From { get; set; } = default!;
    public string FromName { get; set; } = "AiWeb3";
    public int TimeoutMs { get; set; } = 10000; // 10 s
}

public class SmtpEmailSender : IEmailSender
{
    private readonly SmtpOptions _opt;
    private readonly ILogger<SmtpEmailSender> _log;

    public SmtpEmailSender(IOptions<SmtpOptions> opt, ILogger<SmtpEmailSender> log)
    {
        _opt = opt.Value;
        _log = log;
    }

    public async Task SendEmailAsync(string email, string subject, string htmlMessage)
    {
        using var msg = new MailMessage
        {
            From = new MailAddress(_opt.From, _opt.FromName),
            Subject = subject,
            Body = htmlMessage,
            IsBodyHtml = true
        };
        msg.To.Add(email);

        using var client = new SmtpClient(_opt.Host, _opt.Port)
        {
            EnableSsl = _opt.EnableSsl,
            Credentials = new NetworkCredential(_opt.User, _opt.Password),
            DeliveryMethod = SmtpDeliveryMethod.Network,
            UseDefaultCredentials = false,
            Timeout = _opt.TimeoutMs
        };

        try
        {
            _log.LogInformation("SMTP: connecting {Host}:{Port} (SSL={SSL}, Timeout={Timeout} ms)", _opt.Host, _opt.Port, _opt.EnableSsl, _opt.TimeoutMs);
            await client.SendMailAsync(msg);
            _log.LogInformation("SMTP: sent to {Email}", email);
        }
        catch (Exception ex)
        {
            _log.LogError(ex, "SMTP send failed to {Email}", email);
            throw; 
        }
    }
}
