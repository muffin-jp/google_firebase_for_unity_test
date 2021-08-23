using Firebase.Analytics;
using Firebase.Auth;
using UnityEngine;

public class FirebaseManager : MonoBehaviour
{
    private string localID = "12345678";
    private FirebaseAuth auth;
    private FirebaseUser user;
    private string displayName;
    private string emailAddress = "test@dummyemail.com";

    // Start is called before the first frame update
    private void Start()
    {
        InitializeFirebase();
        InitializeAuthFirebase();
        SignUp();
    }

    private void InitializeFirebase()
    {
        FirebaseAnalytics.SetAnalyticsCollectionEnabled(true);
        // Set the user ID.
        FirebaseAnalytics.SetUserId(localID);
        FirebaseAnalytics.LogEvent(FirebaseAnalytics.EventLogin);
    }

    private void InitializeAuthFirebase() {
        auth = FirebaseAuth.DefaultInstance;
        auth.StateChanged += AuthStateChanged;
        AuthStateChanged(this, null);
    }

    private void SignUp()
    {
        auth.CreateUserWithEmailAndPasswordAsync(emailAddress, "password").ContinueWith(task => {
            if (task.IsCanceled) {
                Debug.LogError("CreateUserWithEmailAndPasswordAsync was canceled.");
                return;
            }
            if (task.IsFaulted) {
                Debug.LogError("CreateUserWithEmailAndPasswordAsync encountered an error: " + task.Exception);
                return;
            }

            // Firebase user has been created.
            var newUser = task.Result;
            Debug.LogFormat("Firebase user created successfully: {0} ({1})",
                newUser.DisplayName, newUser.UserId);
        });
    }

    private void AuthStateChanged(object sender, System.EventArgs eventArgs) {
        if (auth.CurrentUser != user) {
            bool signedIn = user != auth.CurrentUser && auth.CurrentUser != null;
            if (!signedIn && user != null) {
                Debug.Log("Signed out " + user.UserId);
            }
            user = auth.CurrentUser;
            if (signedIn) {
                Debug.Log("Signed in " + user.UserId);
                displayName = user.DisplayName ?? "";
                emailAddress = user.Email ?? "";
            }
        }
    }
}
