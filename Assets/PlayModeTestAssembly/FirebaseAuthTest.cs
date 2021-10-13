using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using Firebase.Auth;
using InGameMoney;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace PlayModeTestAssembly
{
    public class FirebaseAuthTest
    {
        private bool initialized;
        private AsyncOperation loadSceneOperation;
        private static IEnumerable<string> LevelTestCases => new List<string> {"FirebaseAuthentication"};
        private const string Email = "test-runner@email.com";
        private const string Password = "1234567";
        
        [SetUp]
        public void Setup()
        {
            if (!initialized)
            {
                PlayerPrefs.DeleteAll();
                initialized = true;
            }
        }

        [UnityTest]
        [Timeout(3600000)]
        public IEnumerator Test1GuestAuthSignUp([ValueSource(nameof(LevelTestCases))] string sceneName)
        {
            yield return LoadScene(sceneName);
            Assert.IsTrue(loadSceneOperation.isDone, "Scene not loaded");

            GuestSignUp();

            // Wait to avoid errors missing firebase logger game object
            yield return new WaitForSeconds(2);
        }

        private static async void GuestSignUp()
        {
            var guest = new Guest(FirebaseAuth.DefaultInstance);
            var newUser = await guest.SignInAnonymously();

            Assert.That(newUser.Result.IsAnonymous);
            await guest.DeleteUserAsync();
            AccountManager.Instance.SignOut();
            Assert.IsFalse(AccountManager.Instance.SignedIn, "Should be signed out");
        }
        
        private IEnumerator LoadScene(string sceneName)
        {
            loadSceneOperation = SceneManager.LoadSceneAsync(sceneName);
            loadSceneOperation.allowSceneActivation = true;

            while (!loadSceneOperation.isDone)
            {
                yield return null;
            }
        }

        [UnityTest]
        [Timeout(3600000)]
        [TestCase("FirebaseAuthentication", true, false, ExpectedResult = (IEnumerator)null)]
        public IEnumerator Test2EmailAuthSignUpAndSignIn(string sceneName, bool testSignIn, bool testChangePassword)
        {
            yield return LoadScene(sceneName);
            Print.GreenLog($">>>> TestEmailAuthSignUp testSignIn {testSignIn} testChangePassword {testChangePassword}");
            Assert.IsTrue(loadSceneOperation.isDone, "Scene not loaded");
            
            EmailSignUp(testSignIn, testChangePassword);
            // Wait to avoid errors missing firebase logger game object
            yield return new WaitForSeconds(2);
        }
        
        [UnityTest]
        [Timeout(3600000)]
        [TestCase("FirebaseAuthentication", false, true, ExpectedResult = (IEnumerator)null)]
        [TestCase("FirebaseAuthentication", true, true, ExpectedResult = (IEnumerator)null)]
        public IEnumerator Test3EmailAuthSignUpAndChangePassword(string sceneName, bool testSignIn, bool testChangePassword)
        {
            yield return LoadScene(sceneName);
            Print.GreenLog($">>>> TestEmailAuthSignUp testSignIn {testSignIn} testChangePassword {testChangePassword}");
            Assert.IsTrue(loadSceneOperation.isDone, "Scene not loaded");
            
            EmailSignUp(testSignIn, testChangePassword);
            // Wait to avoid errors missing firebase logger game object
            yield return new WaitForSeconds(2);
        }

        private static async void EmailSignUp(bool testSignIn, bool testChangePassword)
        {
            if (AccountManager.Instance.SignedIn) 
                AccountManager.Instance.SignOut();
            var userData = ((UserDataAccess)AccountManager.UserDataAccess).UserData;
            var emailAuth = new EmailAuth(FirebaseAuth.DefaultInstance, userData);
            var newUser = await emailAuth.EmailAuthSignUp(Email, Password);
            if (newUser.Result != null)
                Print.GreenLog($">>>> Firebase email auth user created successfully Email {newUser.Result.Email} id {newUser.Result.UserId} IsAnonymous {newUser.Result.IsAnonymous} DisplayName {newUser.Result.DisplayName}");
            Assert.IsFalse(newUser.Result != null && newUser.Result.IsAnonymous, "newUser should not anonymous");
            Assert.IsTrue(newUser.Result != null && newUser.Result.Email == Email, "Email is not the same");

            if (testSignIn)
            {
                AccountManager.Instance.SignOut();
                await EmailSignIn(emailAuth);
            }
            if (testChangePassword) 
                await EmailAuthChangePassword(emailAuth);
            
            await emailAuth.DeleteUserAsync();
            AccountManager.Instance.SignOut();
            Assert.IsFalse(AccountManager.Instance.SignedIn, "Should be signed out");
        }

        private static async Task EmailSignIn(EmailAuth emailAuth)
        {
            var loginUser = await emailAuth.SignInWithEmailAndPassword(Email, Password);
            if (loginUser.Result != null)
                Print.GreenLog($">>>> Login Successfully");
            Assert.IsTrue(loginUser.Result != null && loginUser.Result.Email == Email, "Email is not the same");
        }
        
        private static async Task EmailAuthChangePassword(EmailAuth emailAuth)
        {
            const string newPassword = "myNewPass";
            var resetPasswordIsCompleted = await emailAuth.UpdatePasswordAsync(newPassword);
            Assert.IsTrue(resetPasswordIsCompleted, "Reset Password is not completed");
        }
        
        [UnityTest]
        [Timeout(3600000)]
        [TestCase("FirebaseAuthentication", ExpectedResult = null)]
        public IEnumerator Test4SendPasswordResetEmail(string sceneName)
        {
            yield return LoadScene(sceneName);
            Print.GreenLog($">>>> TestEmailAuthSignUp ");
            Assert.IsTrue(loadSceneOperation.isDone, "Scene not loaded");
            
            SendPasswordResetEmail();
            // Wait to avoid errors missing firebase logger game object
            yield return new WaitForSeconds(2);
        }

        private async void SendPasswordResetEmail()
        {
            if (AccountManager.Instance.SignedIn) 
                AccountManager.Instance.SignOut();
            Print.GreenLog($"SendPasswordResetEmail AccountManager.Instance.SignedIn {AccountManager.Instance.SignedIn}");
            var userData = ((UserDataAccess)AccountManager.UserDataAccess).UserData;
            var emailAuth = new EmailAuth(FirebaseAuth.DefaultInstance, userData);
            var newUser = await emailAuth.EmailAuthSignUp(Email, Password);
            if (newUser.Result != null)
                Print.GreenLog($">>>> Firebase email auth user created successfully Email {newUser.Result.Email} id {newUser.Result.UserId} IsAnonymous {newUser.Result.IsAnonymous} DisplayName {newUser.Result.DisplayName}");
            Assert.IsFalse(newUser.Result != null && newUser.Result.IsAnonymous, "newUser should not anonymous");

            var sendPasswordResetEmailCompleted = await emailAuth.SendPasswordResetEmail(Email);

            Assert.IsTrue(sendPasswordResetEmailCompleted, "sendPasswordResetEmail not completed");
            
            await emailAuth.DeleteUserAsync();
            AccountManager.Instance.SignOut();
            Assert.IsFalse(AccountManager.Instance.SignedIn, "Should be signed out");
        }
    }
}