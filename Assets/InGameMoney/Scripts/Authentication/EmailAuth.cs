using Firebase.Auth;
using UnityEngine;
using UnityEngine.Assertions;
using System.Threading.Tasks;
using Firebase.Extensions;
#pragma warning disable 4014

namespace InGameMoney
{
    public class EmailAuth : AccountBase, IAccountBase
    {
        private readonly FirebaseAuth auth;
        private readonly UserData userData;

        public EmailAuth(FirebaseAuth auth, UserData userData)
        {
            this.auth = auth;
            this.userData = userData;
        }
        
        public void Validate()
        {
            if (!AccountManager.Instance.SignedIn && (string.IsNullOrEmpty(userData.AccountData.mailAddress) || string.IsNullOrEmpty(userData.AccountData.password)))
            {
                AccountManager.Instance.SignOutBecauseLocalDataIsEmpty();
                return;
            }
            Print.GreenLog($">>>> NonGuest Email {auth.CurrentUser.Email} HasKey Apple {PlayerPrefs.HasKey(AccountManager.AppleUserIdKey)}");
            // Need to delete apple user id key before using email to sign in
            Assert.IsFalse(PlayerPrefs.HasKey(AccountManager.AppleUserIdKey));
            
            // Check if auto login is on for email authentication
            AutoLoginValidation(userData);
        }

        private void AutoLoginValidation(UserData currentUserData)
        {
            AccountManager.Instance.SetupUI(currentUserData.AccountData.mailAddress, currentUserData.AccountData.password, currentUserData.AccountData.autoLogin);
            if (AccountManager.Instance.SignedIn)
            {
                Print.GreenLog($">>>> OpenGameView from AutoLoginValidation {currentUserData.AccountData.mailAddress}");
                AccountManager.Instance.OpenGameView();
            }
            
            AccountManager.Instance.RegisterGuestAccount.interactable = false;
            
            if (AccountManager.Instance.AutoLogin.isOn)
            {
                ObjectManager.Instance.Logs.text = $"Sign in: {auth.CurrentUser.Email}";
                AccountManager.Instance.Login();
                AccountManager.Instance.UpdatePurchaseAndShop();
            }
        }

        public void PerformSignUpWithEmail(string emailAddress, string password)
        {
            if (auth?.CurrentUser != null && auth.CurrentUser.IsAnonymous && !AccountManager.Instance.RegisterGuestAccount.gameObject.activeSelf)
            {
                LinkAuthWithEmailCredential();
            }
            else
            {
                FirebaseEmailAuthSignUp(emailAddress, password);
            }
        }
        
        private void LinkAuthWithEmailCredential()
        {
            ObjectManager.Instance.Logs.text = "Linking Guest auth credential ...";
            var credential = EmailAuthProvider.GetCredential(AccountManager.Instance.InputFieldMailAddress.text, AccountManager.Instance.InputFieldPassword.text);
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
                AccountManager.LinkAccountToFirestore(AccountManager.Instance.InputFieldMailAddress.text, AccountManager.Instance.InputFieldPassword.text);
                AccountManager.Instance.SetAuthButtonInteraction();
                AccountManager.Instance.OpenGameView();
            }, TaskScheduler.FromCurrentSynchronizationContext());
        }
        
        private async Task FirebaseEmailAuthSignUp(string emailAddress, string password)
        {
            var newUser = await EmailAuthSignUp(emailAddress, password);

            if (newUser.Result != null)
                Print.GreenLog($">>>> Firebase email auth user created successfully Email {newUser.Result.Email} id {newUser.Result.UserId} IsAnonymous {newUser.Result.IsAnonymous} DisplayName {newUser.Result.DisplayName}");
        
            await AccountManager.Instance.SignUpToFirestoreAsync(AccountManager.Instance.GetDefaultUserDataFromInputField());
            AccountManager.Instance.WriteUserData();
            AccountManager.Instance.Login();
        }

        public async Task<Task<FirebaseUser>> EmailAuthSignUp(string emailAddress, string password)
        {
            Print.GreenLog(">>>> Creating User Account....");
        
            var task = auth.CreateUserWithEmailAndPasswordAsync(emailAddress, password)
                .ContinueWithOnMainThread(signUpTask => signUpTask);
        
            await task;
        
            if (task.Result.IsCanceled)
            {
                Print.GreenLog(">>>> Create User With Email And Password was canceled.");
            }

            if (AccountManager.IsFaultedTask(task.Result))
            {
                Print.RedLog($"Exception {task.Result.Exception}");
            }

            return task.Result.IsCompleted ? task.Result : default;
        }
        
        public void DeleteUserAsync()
        {
            DeleteUserAsync(auth);
        }
    }
}
