using System;
using AppleAuth;
using AppleAuth.Enums;
using AppleAuth.Extensions;
using AppleAuth.Interfaces;
using AppleAuth.Native;
using Firebase.Auth;
using UnityEngine;

namespace InGameMoney
{
    public class AppleAuth : IAccountBase
    {
        private readonly FirebaseAuth auth;
        private readonly UserData userData;
        private readonly IAppleAuthManager appleAuthManager;
        public IAppleAuthManager AppleAuthManager => appleAuthManager;

        public AppleAuth()
        {
            auth = FirebaseAuth.DefaultInstance;
            userData = AccountTest.Userdata;
            ObjectManager.Instance.FirstBootLogs.text = $"New AppleAuth";
        }
        public void Validate()
        {
            if (string.IsNullOrEmpty(userData.data.mailAddress) || string.IsNullOrEmpty(userData.data.password))
            {
                AccountTest.Instance.SignOutBecauseLocalDataIsEmpty();
                return;
            }

            if (AccountTest.Instance.SignedIn)
            {
                AccountTest.Instance.SetupUI(userData.data.mailAddress, userData.data.password, userData.data.autoLogin);
                AccountTest.Instance.RegisterGuestAccount.interactable = false;
                if (AccountTest.Instance.AutoLogin.isOn)
                {
                    ObjectManager.Instance.Logs.text = $"Sign in: {auth.CurrentUser.Email}";
                    AccountTest.Instance.Login();
                }
                return;
            }
            
            InitializeLoginMenu();
        }

        private void InitializeLoginMenu()
        {
            if (appleAuthManager == null)
            {
                ObjectManager.Instance.FirstBootLogs.text = $"Initialize appleAuthManager is null, Unsupported platform";
                return;
            }
            
            // If at any point we receive a credentials revoked notification, we delete the stored User ID, and go back to login
            appleAuthManager.SetCredentialsRevokedCallback(result =>
            {
                ObjectManager.Instance.FirstBootLogs.text = $"Received revoked callback {result}";
                AccountTest.Instance.SignOut();
                PlayerPrefs.DeleteKey(AccountTest.AppleUserIdKey);
            });
            
            // If we have an Apple User Id available, get the credential status for it
            if (PlayerPrefs.HasKey(AccountTest.AppleUserIdKey))
            {
                ObjectManager.Instance.FirstBootLogs.text = $"We have an Apple User Id available, get the credential status for it";
                var storedAppleUserId = PlayerPrefs.GetString(AccountTest.AppleUserIdKey);
                CheckCredentialStatusForUserId(storedAppleUserId);
            }
            // If we do not have an stored Apple User Id, attempt a quick login
            else
            {
                ObjectManager.Instance.FirstBootLogs.text = "we do not have an stored Apple User Id, attempt a quick login";
                AttemptQuickLogin();
            }
        }

        private void AttemptQuickLogin()
        {
            var quickLoginArgs = new AppleAuthQuickLoginArgs();
            
            // Quick login should succeed if the credential was authorized before and not revoked
            appleAuthManager.QuickLogin(
                quickLoginArgs,
                credential =>
                {
                    // If it's an Apple credential, save the user ID, for later logins
                    if (credential is IAppleIDCredential appleIdCredential)
                    {
                        ObjectManager.Instance.FirstBootLogs.text = $"Quick login appleIdCredential " +
                                                                    $"User {appleIdCredential.User} \n" +
                                                                    $"RealUserStatus{appleIdCredential.RealUserStatus} \n" +
                                                                    $"Email {appleIdCredential.Email} \n" +
                                                                    $"FullName {appleIdCredential.FullName}";
                        PlayerPrefs.SetString(AccountTest.AppleUserIdKey, credential.User);
                    }
                },
                error =>
                {
                    // If Quick Login fails, we should show the normal sign in with apple menu, to allow for a normal Sign In with apple
                    var authorizationErrorCode = error.GetAuthorizationErrorCode();
                    ObjectManager.Instance.FirstBootLogs.text = $"Quick Login Failed authorizationErrorCode {authorizationErrorCode} error {error}";
                });
        }

        private void CheckCredentialStatusForUserId(string appleUserId)
        {
            // If there is an apple ID available, we should check the credential state
            appleAuthManager.GetCredentialState(
                appleUserId,
                state =>
                {
                    switch (state)
                    {
                        // If it's authorized, login with that user id
                        case CredentialState.Authorized:
                            ObjectManager.Instance.FirstBootLogs.text = $"Authorized {appleUserId}";
                            return;
                        
                        // If it was revoked, or not found, we need a new sign in with apple attempt
                        // Discard previous apple user id
                        case CredentialState.Revoked:
                        case CredentialState.NotFound:
                            ObjectManager.Instance.FirstBootLogs.text = $"CredentialState Revoked or NotFound  {appleUserId}";
                            AccountTest.Instance.SignOut();
                            PlayerPrefs.DeleteKey(AccountTest.AppleUserIdKey);
                            return;
                        case CredentialState.Transferred:
                            ObjectManager.Instance.FirstBootLogs.text = "CredentialState.Transferred";
                            break;
                        default:
                            throw new ArgumentOutOfRangeException(nameof(state), state, null);
                    }
                },
                error =>
                {
                    var authorizationErrorCode = error.GetAuthorizationErrorCode();
                    ObjectManager.Instance.FirstBootLogs.text = $"Error while trying to get credential state {authorizationErrorCode} {error}";
                    AccountTest.Instance.SignOut();
                });
        }
    }
}
