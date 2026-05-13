using System.Configuration;
using System.Net.Mail;
using System.Text;

namespace JobPortalKCV.Helpers
{
    public static class EmailHelper
    {
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
                    ConfigurationManager.AppSettings["MailFrom"] ?? "noreply@jobportalkcv.local",
                    ConfigurationManager.AppSettings["MailFromDisplayName"] ?? "KCV Job Portal",
                    Encoding.UTF8);

                using (var client = new SmtpClient())
                    client.Send(message);
            }
        }
    }
}
