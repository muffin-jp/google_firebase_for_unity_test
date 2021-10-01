using Firebase.Auth;
using UnityEngine;
using UnityEngine.Assertions;
using System.Threading.Tasks;
using Firebase.Extensions;
#pragma warning disable 4014

namespace InGameMoney
{
    public class EmailAuth : IAccountBase
    {
        private readonly FirebaseAuth auth;
        private readonly UserData userData;

        public EmailAuth()
        {
            auth = FirebaseAuth.DefaultInstance;
            userData = ((UserDataAccess)AccountTest.UserDataAccess).UserData;
            ObjectManager.Instance.FirstBootLogs.text = $"New Email Auth AppName {auth.App.Name}";
        }
        
        public void Validate()
        {
            if (!AccountTest.Instance.SignedIn && (string.IsNullOrEmpty(userData.AccountData.mailAddress) || string.IsNullOrEmpty(userData.AccountData.password)))
            {
                AccountTest.Instance.SignOutBecauseLocalDataIsEmpty();
                return;
            }
            Print.GreenLog($">>>> NonGuest Email {auth.CurrentUser.Email} HasKey Apple {PlayerPrefs.HasKey(AccountTest.AppleUserIdKey)}");
            // Need to delete apple user id key before using email to sign in
            Assert.IsFalse(PlayerPrefs.HasKey(AccountTest.AppleUserIdKey));
            
            // Check if auto login is on for email authentication
            AutoLoginValidation(userData);
        }

        private void AutoLoginValidation(UserData currentUserData)
        {
            AccountTest.Instance.SetupUI(currentUserData.AccountData.mailAddress, currentUserData.AccountData.password, currentUserData.AccountData.autoLogin);
            if (AccountTest.Instance.SignedIn)
            {
                Print.GreenLog($">>>> OpenGameView from AutoLoginValidation {currentUserData.AccountData.mailAddress}");
                AccountTest.Instance.OpenGameView();
            }
            
            AccountTest.Instance.RegisterGuestAccount.interactable = false;
            
            if (AccountTest.Instance.AutoLogin.isOn)
            {
                ObjectManager.Instance.Logs.text = $"Sign in: {auth.CurrentUser.Email}";
                AccountTest.Instance.Login();
                AccountTest.Instance.UpdatePurchaseAndShop();
            }
        }

        public void PerformSignUpWithEmail()
        {
            if (auth?.CurrentUser != null && auth.CurrentUser.IsAnonymous && !AccountTest.Instance.RegisterGuestAccount.gameObject.activeSelf)
            {
                LinkAuthWithEmailCredential();
            }
            else
            {
                FirebaseEmailAuthSignUp();
            }
        }
        
        private void LinkAuthWithEmailCredential()
        {
            ObjectManager.Instance.Logs.text = "Linking Guest auth credential ...";
            var credential = EmailAuthProvider.GetCredential(AccountTest.Instance.InputFieldMailAddress.text, AccountTest.Instance.InputFieldPassword.text);
            var currentUser = auth.CurrentUser;

            currentUser.LinkWithCredentialAsync(credential).ContinueWith(task =>
            {
                if (task.IsCanceled) {
                    ObjectManager.Instance.Logs.text = "LinkWithCredentialAsync was canceled.";
                    return;
                }
                if (task.IsFaulted) {
                    ObjectManager.Instance.Logs.text = $"LinkWithCredentialAsync encountered an error: {task.Exception}";
                    return;
                }
                
                var newUser = task.Result;
                ObjectManager.Instance.Logs.text = $"Credentials successfully linked to Firebase userId {newUser.UserId}";
                AccountTest.LinkAccountToFirestore(AccountTest.Instance.InputFieldMailAddress.text, AccountTest.Instance.InputFieldPassword.text);
                AccountTest.Instance.SetAuthButtonInteraction();
                AccountTest.Instance.OpenGameView();
            }, TaskScheduler.FromCurrentSynchronizationContext());
        }
        
        private async Task FirebaseEmailAuthSignUp()
        {
            ObjectManager.Instance.Logs.text = "Creating User Account....";
        
            var task = auth.CreateUserWithEmailAndPasswordAsync(AccountTest.Instance.InputFieldMailAddress.text, AccountTest.Instance.InputFieldPassword.text)
                .ContinueWithOnMainThread(signUpTask => signUpTask);
        
            await task;
        
            if (task.Result.IsCanceled)
            {
                ObjectManager.Instance.Logs.text = "Create User With Email And Password was canceled.";
                return;
            }
        
            if (AccountTest.IsFaultedTask(task.Result)) return;
        
            var newUser = task.Result;
            ObjectManager.Instance.Logs.text =
                $"Firebase user created successfully Email {newUser.Result.Email} id {newUser.Result.UserId} DisplayName {newUser.Result.DisplayName}";
        
            await AccountTest.Instance.SignUpToFirestoreAsync(AccountTest.Instance.GetDefaultUserDataFromInputField());
            AccountTest.Instance.WriteUserData();
            AccountTest.Instance.Login();
        }
    }
}
