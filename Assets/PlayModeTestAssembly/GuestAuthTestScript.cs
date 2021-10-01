using System.Collections;
using System.Collections.Generic;
using Firebase.Auth;
using InGameMoney;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Tests
{
    public class GuestAuthTestScript
    {
        private bool initialized;
        private AsyncOperation loadSceneOperation;
        private static IEnumerable<string> LevelTestCases => new List<string> {"AnonymousLogin"};
        
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
        public IEnumerator GuestAuthTestSignUp([ValueSource(nameof(LevelTestCases))] string sceneName)
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
            guest.DeleteUserAsync();
            AccountManager.Instance.SignOut();
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
        
    }
}
