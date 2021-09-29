using Firebase.Auth;
using UnityEngine;

namespace InGameMoney
{
    public class GuestAccount : MonoBehaviour
    {
        private static FirebaseAuth auth;
        
        private void Awake()
        {
            InitializeFirebase();
        }

        private static void InitializeFirebase()
        {
            auth = FirebaseAuth.DefaultInstance;
        }

        public void OnGuestLoginButton()
        {
            AccountTest.InitPersonalData();
            AccountTest.ResetPersonalData();
            SignUpUsingAnonymous();
        }

        private async void SignUpUsingAnonymous()
        {
            ObjectManager.Instance.Logs.text = "Creating Gust User Account....";
            var task = auth.SignInAnonymouslyAsync().ContinueWith(signInTask => signInTask);

            await task;
            
            if (task.Result.IsCanceled)
            {
                ObjectManager.Instance.Logs.text = "SignInAnonymouslyAsync was canceled.";
            }
            
            if (task.Result.IsFaulted)
            {
                ObjectManager.Instance.Logs.text = $"SignInAnonymouslyAsync encountered an error: {task.Exception}";
            }

            var newUser = task.Result;
            ObjectManager.Instance.Logs.text = $"Firebase user created successfully Email {newUser.Result.Email} id {newUser.Result.UserId} DisplayName {newUser.Result.DisplayName}";
            ProceedLogin();
            var userData = AccountTest.Instance.GetDefaultUserDataFromInputField();
            await AccountTest.Instance.SignUpToFirestoreAsync(userData);
            AccountTest.Instance.WriteUserData();
            AccountTest.Instance.Login();
        }

        private static void ProceedLogin()
        {
            ObjectManager.Instance.FirstBoot.SetActive(false);
            ObjectManager.Instance.InGameMoney.SetActive(true);
            AccountTest.Instance.SetupUI($"匿名@{auth.CurrentUser.UserId}", $"vw-guest-pass@{auth.CurrentUser.UserId}", false);
            if (AccountTest.Instance.SignedIn) AccountTest.Instance.OpenGameView();
        }
    }
}
