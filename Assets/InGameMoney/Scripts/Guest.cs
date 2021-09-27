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
            userData = AccountTest.Userdata;
        }
        
        public void Validate()
        {
            if (!AccountTest.Instance.SignedIn && (string.IsNullOrEmpty(userData.AccountData.mailAddress) || string.IsNullOrEmpty(userData.AccountData.password)))
            {
                AccountTest.Instance.SignOutBecauseLocalDataIsEmpty();
                return;
            }
            Debug.Log($">>>> Guest {auth.CurrentUser.UserId}");
            AccountTest.Instance.SetupUI($"匿名@{auth.CurrentUser.UserId}", $"vw-guest-pass@{auth.CurrentUser.UserId}", false);
            AccountTest.Instance.Login();
            AccountTest.Instance.UpdatePurchaseAndShop();
        }
    }
}
