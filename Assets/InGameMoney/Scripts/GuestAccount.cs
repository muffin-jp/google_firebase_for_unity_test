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
            SignInAnonymous();
        }

        private async void SignInAnonymous()
        {
            ObjectManager.Instance.Logs.text = "Creating Gust User Account....";
            var task = auth.SignInAnonymouslyAsync().ContinueWith(signInTask => signInTask);

            if (task.IsCanceled)
            {
                ObjectManager.Instance.Logs.text = "SignInAnonymouslyAsync was canceled.";
            }

            await task;
            
            if (task.IsFaulted)
            {
                ObjectManager.Instance.Logs.text = $"SignInAnonymouslyAsync encountered an error: {task.Exception}";
            }

            var newUser = task.Result;
            ObjectManager.Instance.Logs.text = $"Firebase user created successfully Email {newUser.Result.Email} id {newUser.Result.UserId} DisplayName {newUser.Result.DisplayName}";
            ProceedLogin();
        }

        private static void ProceedLogin()
        {
            ObjectManager.Instance.FirstBoot.SetActive(false);
            ObjectManager.Instance.InGameMoney.SetActive(true);
            AccountTest.Instance.SetupUI($"匿名@{auth.CurrentUser.UserId}", $"vw-guest-pass@{auth.CurrentUser.UserId}", false);
            var userData = AccountTest.Instance.GetDefaultUserData();
            AccountTest.Instance.SignUpToFirestore(userData);
        }
    }
}
