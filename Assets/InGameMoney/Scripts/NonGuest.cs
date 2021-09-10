using Firebase.Auth;

namespace InGameMoney
{
    public class NonGuest : IAccountBase
    {
        private readonly FirebaseAuth auth;
        private readonly UserData userData;

        public NonGuest()
        {
            auth = FirebaseAuth.DefaultInstance;
            userData = AccountTest.Userdata;
            ObjectManager.Instance.FirstBootLogs.text = $"New NonGuest AppName {auth.App.Name}";
        }
        
        public void Validate()
        {
            if (string.IsNullOrEmpty(userData.data.mailAddress) || string.IsNullOrEmpty(userData.data.password))
            {
                AccountTest.Instance.SignOutBecauseLocalDataIsEmpty();
                return;
            }

            if (AccountTest.Instance.SignedIn)
            {
                AccountTest.Instance.SetupUI(userData.data.mailAddress, userData.data.password, userData.data.autoLogin);
                AccountTest.Instance.RegisterGuestAccount.interactable = false;
                if (AccountTest.Instance.AutoLogin.isOn)
                {
                    ObjectManager.Instance.Logs.text = $"Sign in: {auth.CurrentUser.Email}";
                    AccountTest.Instance.Login();
                }
            }
        }
    }
}
