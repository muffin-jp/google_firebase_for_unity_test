using System.Threading.Tasks;
using Firebase.Auth;
using Firebase.Extensions;

namespace InGameMoney
{
    public class Guest : AccountBase, IAccountBase
    {
        private readonly FirebaseAuth auth;
        private readonly UserData userData;

        public Guest(FirebaseAuth auth)
        {
            this.auth = auth;
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

        public async Task PerformGuestLogin()
        {
            AccountManager.InitPersonalData();
            AccountManager.ResetPersonalData();
            await SignUpUsingAnonymous();
            ObjectManager.Instance.RegisterGuestAccount.SetActive(true);
        }
        
        private async Task SignUpUsingAnonymous()
        {
            Print.GreenLog("Creating Gust User Account....");

            var newUser = await SignInAnonymously();
            if (newUser.Result != null) 
                Print.GreenLog($">>>> Firebase guest user created successfully Email {newUser.Result.Email} id {newUser.Result.UserId} DisplayName {newUser.Result.DisplayName}");
            ProceedLogin();
            var userDataFromInputField = AccountManager.Instance.GetDefaultUserDataFromInputField();
            await AccountManager.Instance.SignUpToFirestoreAsync(userDataFromInputField);
            AccountManager.Instance.WriteUserData();
            AccountManager.Instance.Login();
        }

        public async Task<Task<FirebaseUser>> SignInAnonymously()
        {
            var task = auth.SignInAnonymouslyAsync().ContinueWith(signInTask => signInTask);

            await task;
            
            if (task.Result.IsCanceled)
            {
                Print.GreenLog(">>>> SignInAnonymouslyAsync was canceled.");
            }
            
            if (task.Result.IsFaulted)
            {
                Print.GreenLog($">>>>  SignInAnonymouslyAsync encountered an error: {task.Exception}");
            }

            Print.GreenLog($">>>> SignInAnonymously Successfully {task.Result.IsCompleted}");
            return task.Result.IsCompleted ? task.Result : default;
        }
        
        private void ProceedLogin()
        {
            if (ObjectManager.Instance.FirstBoot) ObjectManager.Instance.FirstBoot.SetActive(false);
            ObjectManager.Instance.InGameMoney.SetActive(true);
            AccountManager.Instance.SetupUI($"匿名@{auth.CurrentUser.UserId}", $"vw-guest-pass@{auth.CurrentUser.UserId}", false);
            if (AccountManager.Instance.SignedIn)
            {
                Print.GreenLog($">>>> OpenGameView from OnGuestLoginButton {auth.CurrentUser.Email}");
                AccountManager.Instance.OpenGameView();
            }
        }

        public async Task DeleteUserAsync()
        {
            await DeleteUserAsync(auth);
        }
    }
}
