using System;
using System.Configuration;
using System.Net;
using System.Net.Mail;
using System.Text;

namespace JobPortalKCV.Helpers
{
    public static class EmailHelper
    {
        private const string SmtpHostEnv = "KCV_SMTP_HOST";
        private const string SmtpPortEnv = "KCV_SMTP_PORT";
        private const string SmtpUsernameEnv = "KCV_SMTP_USERNAME";
        private const string SmtpPasswordEnv = "KCV_SMTP_PASSWORD";
        private const string SmtpEnableSslEnv = "KCV_SMTP_ENABLE_SSL";
        private const string MailFromEnv = "KCV_MAIL_FROM";
        private const string MailFromDisplayNameEnv = "KCV_MAIL_FROM_DISPLAY_NAME";

        public static void Send(string to, string subject, string body)
        {
            using (var message = new MailMessage())
            {
                message.To.Add(to);
                message.Subject = subject;
                message.Body = body;
                message.SubjectEncoding = Encoding.UTF8;
                message.BodyEncoding = Encoding.UTF8;
                message.HeadersEncoding = Encoding.UTF8;
                message.From = new MailAddress(
                    FirstNotEmpty(GetEnvironment(MailFromEnv), ConfigurationManager.AppSettings["MailFrom"], GetEnvironment(SmtpUsernameEnv), "noreply@jobportalkcv.local"),
                    FirstNotEmpty(GetEnvironment(MailFromDisplayNameEnv), ConfigurationManager.AppSettings["MailFromDisplayName"], "KCV Job Portal"),
                    Encoding.UTF8);

                using (var client = CreateSmtpClient())
                    client.Send(message);
            }
        }

        private static SmtpClient CreateSmtpClient()
        {
            var host = GetRequiredEnvironment(SmtpHostEnv);
            var username = GetRequiredEnvironment(SmtpUsernameEnv);
            var password = GetRequiredEnvironment(SmtpPasswordEnv);

            return new SmtpClient
            {
                Host = host,
                Port = GetEnvironmentInt(SmtpPortEnv, 587),
                EnableSsl = GetEnvironmentBool(SmtpEnableSslEnv, true),
                DeliveryMethod = SmtpDeliveryMethod.Network,
                UseDefaultCredentials = false,
                Credentials = new NetworkCredential(username, password)
            };
        }

        private static string GetRequiredEnvironment(string name)
        {
            var value = GetEnvironment(name);

            if (!String.IsNullOrWhiteSpace(value))
                return value;

            throw new InvalidOperationException("Missing required environment variable: " + name);
        }

        private static int GetEnvironmentInt(string name, int fallback)
        {
            int value;
            return Int32.TryParse(GetEnvironment(name), out value) ? value : fallback;
        }

        private static bool GetEnvironmentBool(string name, bool fallback)
        {
            bool value;
            return Boolean.TryParse(GetEnvironment(name), out value) ? value : fallback;
        }

        private static string GetEnvironment(string name)
        {
            return Environment.GetEnvironmentVariable(name);
        }

        private static string FirstNotEmpty(params string[] values)
        {
            foreach (var value in values)
            {
                if (!String.IsNullOrWhiteSpace(value))
                    return value;
            }

            return "";
        }
    }
}
