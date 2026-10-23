
using Microsoft.Extensions.Configuration;
using System;
using System.ComponentModel.DataAnnotations;


namespace LabManager.Models
{

    public class EmailSettings
    {
       
            [ConfigurationKeyName("SmtpHost")]
            public string Host { get; set; } = "";

            [ConfigurationKeyName("SmtpPort")]
            public int Port { get; set; } = 587;

            public bool UseSsl { get; set; } = false;

        public string Username { get; set; } = "";

        [ConfigurationKeyName("AppPassword")]
        public string Password { get; set; } = "";

            [ConfigurationKeyName("SenderEmail")]
            public string FromEmail { get; set; } = "";

            [ConfigurationKeyName("SenderName")]
            public string FromName { get; set; } = "Lab Manager";
        
        // OAuth settings
        public bool UseOAuth { get; set; } = false;
        public string OAuthClientId { get; set; } = "";
        public string OAuthClientSecret { get; set; } = "";
    }
}

