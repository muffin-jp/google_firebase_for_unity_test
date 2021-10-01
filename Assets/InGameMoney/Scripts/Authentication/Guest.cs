using Firebase.Auth;
using UnityEngine;

namespace InGameMoney
{
    public class Guest : IAccountBase
    {
        private readonly FirebaseAuth auth;
        private readonly UserData userData;
        
        public Guest()
        {
            auth = FirebaseAuth.DefaultInstance;
            userData = ((UserDataAccess)AccountManager.UserDataAccess).UserData;
        }
        
        public void Validate()
        {
            if (!AccountManager.Instance.SignedIn && (string.IsNullOrEmpty(userData.AccountData.mailAddress) || string.IsNullOrEmpty(userData.AccountData.password)))
            {
                AccountManager.Instance.SignOutBecauseLocalDataIsEmpty();
                return;
            }
            AccountManager.Instance.SetupUI($"匿名@{auth.CurrentUser.UserId}", $"vw-guest-pass@{auth.CurrentUser.UserId}", false);
            if (AccountManager.Instance.SignedIn)
            {
                Print.GreenLog($">>>> OpenGameView from Guest if SignedIn {auth.CurrentUser.Email}");
                AccountManager.Instance.OpenGameView();
            }
            AccountManager.Instance.Login();
            AccountManager.Instance.UpdatePurchaseAndShop();
        }
    }
}
