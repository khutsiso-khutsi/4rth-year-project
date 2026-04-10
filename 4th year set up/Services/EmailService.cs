using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;

namespace _4th_year_set_up.Services
{
    public class EmailService
    {
        private readonly IConfiguration _config;

        public EmailService(IConfiguration config)
        {
            _config = config;
        }

        public void SendEmail(string toEmail, string subject, string body)
        {
            var settings = _config.GetSection("EmailSettings");

            var senderEmail = settings["SenderEmail"]
                ?? throw new InvalidOperationException("SenderEmail is missing from appsettings.json");
            var senderName = settings["SenderName"]
                ?? throw new InvalidOperationException("SenderName is missing from appsettings.json");
            var appPassword = settings["AppPassword"]
                ?? throw new InvalidOperationException("AppPassword is missing from appsettings.json");
            var smtpHost = settings["SmtpHost"]
                ?? throw new InvalidOperationException("SmtpHost is missing from appsettings.json");

            var message = new MimeMessage();
            message.From.Add(new MailboxAddress(senderName, senderEmail));
            message.To.Add(new MailboxAddress("", toEmail));
            message.Subject = subject;
            message.Body = new TextPart("html") { Text = body };

            using var smtp = new SmtpClient();
            smtp.Connect(smtpHost, 587, SecureSocketOptions.StartTls);
            smtp.Authenticate(senderEmail, appPassword);
            smtp.Send(message);
            smtp.Disconnect(true);
        }
    }
}