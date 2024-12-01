using MimeKit;
using SmtpClient = MailKit.Net.Smtp.SmtpClient;

namespace hitscord_net.OtherFunctions.EmailServer;

public class EmailSenderMailDev : IEmailSender
{
    private readonly string _smtpServer = "localhost";
    private readonly int _smtpPort = 1025;

    public async Task SendEmailAsync(string toEmail, string subject, string body)
    {
        var emailMessage = new MimeMessage();
        emailMessage.From.Add(new MailboxAddress("MyApp", "no-reply@myapp.com"));
        emailMessage.To.Add(new MailboxAddress("", toEmail));
        emailMessage.Subject = subject;
        emailMessage.Body = new TextPart("html")
        {
            Text = body
        };

        using var client = new SmtpClient();
        await client.ConnectAsync(_smtpServer, _smtpPort, MailKit.Security.SecureSocketOptions.None);
        await client.SendAsync(emailMessage);
        await client.DisconnectAsync(true);
    }
}
