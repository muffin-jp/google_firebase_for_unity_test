using System.Threading.Tasks;
using Firebase.Auth;
using Firebase.Extensions;
using UnityEngine;

namespace InGameMoney
{
    public class GameCenterButton : MonoBehaviour
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
        
        public void OnSignInWithGameCenterButton()
        {
#if UNITY_IOS
            SignInWithGameCenter();
#else
            ObjectManager.Instance.Logs.text = "Game Center is not supported on this platform.";
#endif
        }

        private static async void SignInWithGameCenter()
        {
            ObjectManager.Instance.Logs.text = $"SignInWithGameCenter...";

            var credential = await GetGameCenterCredential();
            
            var loginTask = auth.SignInWithCredentialAsync(credential).ContinueWithOnMainThread(task => task);
            
            await loginTask;

            if (loginTask.IsFaulted)
            {
                ObjectManager.Instance.Logs.text = $"IsFaulted {loginTask.Exception} {loginTask.Result.Exception}";
            }

            if (loginTask.IsCompleted)
            {
                ObjectManager.Instance.Logs.text =
                    $"Login Completed {loginTask.Result.Result.DisplayName} {loginTask.Result.Result.UserId}";
            }
        }

        private static async Task<Credential> GetGameCenterCredential()
        {
            var credentialTask = GameCenterAuthProvider.GetCredentialAsync();
            var continueTask = credentialTask.ContinueWithOnMainThread(task => task);

            await continueTask;

            if (continueTask.IsFaulted)
            {
                ObjectManager.Instance.Logs.text = $"IsFaulted {continueTask.Exception} {continueTask.Result.Exception}";
            }

            if (continueTask.IsCompleted)
            {
                ObjectManager.Instance.Logs.text = $"GetCredential IsCompleted";
                return continueTask.Result.Result;
            }

            return default;
        }

        public void OnAuthenticateToGameCenter()
        {
#if UNITY_IOS
            Social.localUser.Authenticate(success =>
            {
                ObjectManager.Instance.Logs.text = $"Game Center Initialization Complete - success: {success}";
            });
#else
            ObjectManager.Instance.Logs.text = "Game Center is not supported on this platform.";
#endif
        }
    }
}
