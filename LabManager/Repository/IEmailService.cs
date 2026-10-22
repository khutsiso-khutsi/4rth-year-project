using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using LabManager.Models;    
using MimeKit;
using MailKit.Net.Smtp;
using MailKit.Security;

namespace LabManager.Repository
{
    public  interface IEmailService
    {
        Task SendEmailAsync(
            string to,
            string subject,
            string body);   
    }

    public static class TemporaryPasswordGenerator
    {
        private const string Characters =
            "ABCDEFGHJKLMNPQRSTUVWXYZ" +
            "abcdefghijkmnopqrstuvwxyz" +
            "23456789!@$%";

        public static string Generate(int length = 14)
        {
            if (length < 12)
                throw new ArgumentOutOfRangeException(nameof(length));

            return new string(
                Enumerable.Range(0, length)
                    .Select(_ => Characters[
                        RandomNumberGenerator.GetInt32(
                            Characters.Length)])
                    .ToArray());
        }
    }


    public class EmailService : IEmailService
    {
        private readonly EmailSettings _settings;
        private readonly ILogger<EmailService> _logger;

        public EmailService(
            IOptions<EmailSettings> options,
            ILogger<EmailService> logger)
        {
            _settings = options.Value;
            _logger = logger;
        }

        public async Task SendEmailAsync(
            string to,
            string subject,
            string body)
        {
            if (string.IsNullOrWhiteSpace(to))
                throw new ArgumentException(
                    "Recipient email address is required.", nameof(to));

            if (string.IsNullOrWhiteSpace(_settings.Host))
                throw new InvalidOperationException(
                    "SMTP host is not configured.");

            if (string.IsNullOrWhiteSpace(_settings.FromEmail))
                throw new InvalidOperationException(
                    "Sender email address is not configured.");

            var message = new MimeMessage();

            message.From.Add(
                new MailboxAddress(
                    _settings.FromName,
                    _settings.FromEmail));

            message.To.Add(MailboxAddress.Parse(to));
            message.Subject = subject;

            message.Body = new MimeKit.BodyBuilder
            {
                HtmlBody = body
            }.ToMessageBody();

            using var client = new SmtpClient();

            try
            {
                await client.ConnectAsync(
                    _settings.Host,
                    _settings.Port,
                    _settings.UseSsl
                        ? SecureSocketOptions.SslOnConnect
                        : SecureSocketOptions.StartTls);

                if (!string.IsNullOrWhiteSpace(_settings.Username))
                {
                    await client.AuthenticateAsync(
                        _settings.Username,
                        _settings.Password);
                }

                await client.SendAsync(message);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Failed to send email to {Recipient}.",
                    to);

                throw;
            }
            finally
            {
                if (client.IsConnected)
                    await client.DisconnectAsync(true);
            }
        }
    }

}
