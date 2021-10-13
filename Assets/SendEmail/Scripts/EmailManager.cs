using System.ComponentModel;
using System.Net.Mail;
using InGameMoney;
using UnityEngine;
using UnityEngine.UI;

namespace SendEmail.Scripts
{
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
        public Text Log => log;

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
            if (smtpClient == null)
            {
                smtpClient = new SmtpClient(mailHost);
                smtpClient.SendCompleted += SendCompleteHandler;
            }
            var body = GetBodyMessages();
            var sendEmail = new SendEmail(userEmailAddress.text, publisherEmailAddress, subject.text, body, smtpClient);
            sendEmail.Send(mailPort, mailPassword);
        }

        private string GetBodyMessages()
        {
            return $"User Name: {userName.text}\n" +
                   $"User Contact EmailAddress: {userEmailAddress.text}\n" +
                   $"Firestore Email（課金データに紐づく）: {((UserDataAccess)AccountManager.UserDataAccess).UserData.AccountData.mailAddress}\n" +
                   $"User Id: {AccountManager.CurrentUser.UserId}\n" +
                   $"Messages: \n{messages.text}\n";
        }

        private static void SendCompleteHandler(object sender, AsyncCompletedEventArgs e)
        {
            var msg = (MailMessage)e.UserState;

            if (e.Cancelled)
            {
                Debug.Log(">>>> <color=yellow>Cancelled sending message.</color>");
            }
            else if (e.Error != null)
            {
                Debug.Log($">>>> <color=red>An error has occurred.</color> Error: {e.Error}");
            }
            else
            {
                const string logText = ">>>> <color=lime>Mail has been sent.</color>";
                Debug.Log(logText);
                Instance.log.text = logText;
                ObjectManager.Instance.CloseContactForm();
            }

            msg.Dispose();
        }
    }
}