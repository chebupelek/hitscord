namespace hitscord_net.OtherFunctions.EmailServer;

public interface IEmailSender
{
    Task SendEmailAsync(string toEmail, string subject, string body);
}