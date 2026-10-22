
using System;
using System.ComponentModel.DataAnnotations;


namespace LabManager.Models
{

    public class EmailSettings
    {
        public string Host { get; set; } = "";
        public int Port { get; set; } = 587;
        public bool UseSsl { get; set; }          // true = port 465, false = port 587 (StartTls)
        public string Username { get; set; } = "";
        public string Password { get; set; } = "";
        public string FromEmail { get; set; } = "";
        public string FromName { get; set; } = "LabManager";
        // OAuth settings
        public bool UseOAuth { get; set; } = false;
        public string OAuthClientId { get; set; } = "";
        public string OAuthClientSecret { get; set; } = "";
    }
}

