using System;
using System.ComponentModel;
using System.Net.Mail;
using UnityEngine;

namespace SendEmail.Scripts
{
    public class SendEmail
    {
        private readonly SmtpClient smtpClient;
        private readonly string userEmailAddress;
        private readonly string publisherEmailAddress;
        private readonly string subject;
        private readonly string messages;
        
        public SendEmail(string userEmailAddress, string publisherEmailAddress, string subject, string messages, SmtpClient smtpClient)
        {
            this.userEmailAddress = userEmailAddress;
            this.publisherEmailAddress = publisherEmailAddress;
            this.subject = subject;
            this.messages = messages;
            this.smtpClient = smtpClient;
        }

        public void Send(int mailPort, string mailPassword)
        {
            var mailMessage = 
                new MailMessage(userEmailAddress, publisherEmailAddress, subject, messages);
            
            smtpClient.Port = mailPort;
            smtpClient.EnableSsl = true;
            smtpClient.DeliveryMethod = SmtpDeliveryMethod.Network;
            smtpClient.UseDefaultCredentials = false;
            smtpClient.Credentials = new System.Net.NetworkCredential(publisherEmailAddress, mailPassword);
            
            System.Net.ServicePointManager.ServerCertificateValidationCallback =
                (s, certificate, chain, sslPolicyErrors) => true;
            
            var logText =
                $"mailFromAddress {publisherEmailAddress} " +
                $"toAddress {userEmailAddress} " +
                $"subject {subject} " +
                $"messages {messages}";
            Debug.Log($">>>> Send {logText}");
            
            smtpClient.SendAsync(mailMessage, mailMessage);
        }

        ~SendEmail()
        {
            smtpClient.Dispose();
        }
    }
}
