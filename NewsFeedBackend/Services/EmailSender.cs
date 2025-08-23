using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;

namespace NewsFeedBackend;

public interface IEmailSender {
    Task SendHtmlAsync(string toEmail, string subject, string htmlBody, CancellationToken ct = default);
}

public class EmailSender(IConfiguration cfg) : IEmailSender {
    public async Task SendHtmlAsync(string toEmail, string subject, string htmlBody, CancellationToken ct = default) {
        var host = cfg["Smtp:Host"] ?? throw new("Missing Smtp:Host");
        var port = int.TryParse(cfg["Smtp:Port"], out var p) ? p : 587;
        var user = cfg["Smtp:User"] ?? throw new("Missing Smtp:User");
        var pass = cfg["Smtp:Password"] ?? throw new("Missing Smtp:Password");
        var fromEmail = cfg["Smtp:FromEmail"] ?? user;
        var fromName  = cfg["Smtp:FromName"]  ?? "News";

        var msg = new MimeMessage();
        msg.From.Add(new MailboxAddress(fromName, fromEmail));
        msg.To.Add(MailboxAddress.Parse(toEmail));
        msg.Subject = subject;
        msg.Body = new BodyBuilder { TextBody = "HTML preferred", HtmlBody = htmlBody }.ToMessageBody();

        using var smtp = new SmtpClient();
        await smtp.ConnectAsync(host, port, SecureSocketOptions.StartTls, ct);
        await smtp.AuthenticateAsync(user, pass, ct);
        await smtp.SendAsync(msg, ct);
        await smtp.DisconnectAsync(true, ct);
    }
}
