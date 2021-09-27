using Firebase.Auth;
using UnityEngine;
using UnityEngine.Assertions;

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
            if (!AccountTest.Instance.SignedIn && (string.IsNullOrEmpty(userData.AccountData.mailAddress) || string.IsNullOrEmpty(userData.AccountData.password)))
            {
                AccountTest.Instance.SignOutBecauseLocalDataIsEmpty();
                return;
            }
            Debug.Log($">>>> NonGuest Email {auth.CurrentUser.Email}");
            // Need to delete apple user id key before using email to sign in
            Assert.IsFalse(PlayerPrefs.HasKey(AccountTest.AppleUserIdKey));
            AccountTest.Instance.Login();
            AccountTest.Instance.UpdatePurchaseAndShop();
        }
    }
}
