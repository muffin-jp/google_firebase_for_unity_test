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
            userData = ((UserDataAccess)AccountTest.UserDataAccess).UserData;
        }
        
        public void Validate()
        {
            if (!AccountTest.Instance.SignedIn && (string.IsNullOrEmpty(userData.AccountData.mailAddress) || string.IsNullOrEmpty(userData.AccountData.password)))
            {
                AccountTest.Instance.SignOutBecauseLocalDataIsEmpty();
                return;
            }
            AccountTest.Instance.SetupUI($"匿名@{auth.CurrentUser.UserId}", $"vw-guest-pass@{auth.CurrentUser.UserId}", false);
            if (AccountTest.Instance.SignedIn)
            {
                Print.GreenLog($">>>> OpenGameView from Guest if SignedIn {auth.CurrentUser.Email}");
                AccountTest.Instance.OpenGameView();
            }
            AccountTest.Instance.Login();
            AccountTest.Instance.UpdatePurchaseAndShop();
        }
    }
}
