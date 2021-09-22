using Firebase.Auth;

namespace InGameMoney
{
    public class Guest : IAccountBase
    {
        private readonly FirebaseAuth auth;
        private readonly UserData userData;
        
        public Guest()
        {
            auth = FirebaseAuth.DefaultInstance;
            userData = AccountTest.Userdata;
        }
        
        public void Validate()
        {
            if (!AccountTest.Instance.SignedIn && (string.IsNullOrEmpty(userData.data.mailAddress) || string.IsNullOrEmpty(userData.data.password)))
            {
                AccountTest.Instance.SignOutBecauseLocalDataIsEmpty();
                return;
            }
				
            AccountTest.Instance.SetupUI($"匿名@{auth.CurrentUser.UserId}", $"vw-guest-pass@{auth.CurrentUser.UserId}", false);
            AccountTest.Instance.Login();
        }
    }
}
