using System.ComponentModel;
using System.Net.Mail;
using UnityEngine;
using UnityEngine.UI;

public class EmailManager : MonoBehaviour
{
    [SerializeField] private InputField userEmailAddress;
    [SerializeField] private InputField userName;
    [SerializeField] private InputField subject;
    [SerializeField] private InputField messages;

    [SerializeField] private string publisherEmailAddress;
    [SerializeField] private string mailHost;
    [SerializeField] private int mailPort;
    [SerializeField] private string mailPassword;
    [SerializeField] private Text log;

    private SmtpClient smtpClient;
    
    public static EmailManager Instance { get; private set; }

    private void Awake()
    {
        if (Instance) Destroy(this);
        else
        {
            Instance = this;
        }
    }

    public void SendEmail()
    {
        Send();
    }

    private void Send()
    {
        messages.text = $"User Name: {userName.text}\n Messages: \n{messages.text}";
        var mailMessage = new MailMessage(userEmailAddress.text, publisherEmailAddress, subject.text, messages.text);

        if (smtpClient == null)
        {
            smtpClient = new SmtpClient(mailHost);
            smtpClient.SendCompleted += SendCompleteHandler;
        }

        smtpClient.Port = mailPort;
        smtpClient.EnableSsl = true;
        smtpClient.DeliveryMethod = SmtpDeliveryMethod.Network;
        smtpClient.UseDefaultCredentials = false;
        smtpClient.Credentials = new System.Net.NetworkCredential(publisherEmailAddress, mailPassword);
        
        System.Net.ServicePointManager.ServerCertificateValidationCallback = 
            (s, certificate, chain, sslPolicyErrors) => true;

        var logText =
            $">>>> Send Email mailHost {mailHost} " +
            $"mailFromAddress {publisherEmailAddress} " +
            $"toAddress {userEmailAddress.text} " +
            $"subject {subject.text} " +
            $"messages {messages.text}";
        Debug.Log(logText);
        log.text = logText;
        smtpClient.SendAsync(mailMessage, mailMessage);
    }

    private static void SendCompleteHandler(object sender, AsyncCompletedEventArgs e)
    {
        var msg = (MailMessage) e.UserState;
		
        if (e.Cancelled) {
            Debug.Log (">>>> <color=yellow>Cancelled sending message.</color>");
        } else if (e.Error != null) {
            Debug.Log ($">>>> <color=red>An error has occurred.</color> Error: {e.Error}");
        } else
        {
            const string logText = ">>>> <color=lime>Mail has been sent.</color>"; 
            Debug.Log (logText);
            Instance.log.text = logText;
        }
        
        msg.Dispose();
    }
}
