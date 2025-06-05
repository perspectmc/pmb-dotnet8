using System;
using System.Net.Mail;
using System.Net;

namespace MBS.Common
{
    public class MailSender
    {
        public MailSender()
        {
        }

        public string SendEmail(string to, string subject, string messagebody)
        {
            var result = string.Empty;

            var mailMessage = new MailMessage();
            mailMessage.Subject = subject;
            mailMessage.To.Add(new MailAddress(to));
            mailMessage.Body = messagebody;
            mailMessage.IsBodyHtml = true;

            var smtpClient = new SmtpClient();

            try 
            { 
                smtpClient.Send(mailMessage);
            }
            catch (Exception ex) 
            { 
                result = ex.Message;  
            }

            return result;
        }

		public string SendEmail(string[] recipents, string subject, string messagebody)
		{
			var result = string.Empty;

			var mailMessage = new MailMessage();
			mailMessage.Subject = subject;

			var toAddresses = string.Join(",", recipents);
			mailMessage.To.Add(toAddresses);
						
			mailMessage.Body = messagebody;
			mailMessage.IsBodyHtml = true;

			var smtpClient = new SmtpClient();

			try
			{
				smtpClient.Send(mailMessage);
			}
			catch (Exception ex)
			{
				result = ex.Message;
			}

			return result;
		}    
    }
}
