using UnityEngine;

namespace InGameMoney
{
    public class SignInButton : MonoBehaviour
    {
        public void OnButtonSignIn()
        {
            var signInWithEmailButton = ObjectManager.Instance.SignUpSignInWithEmailButton;
            signInWithEmailButton.onClick.RemoveAllListeners();
            signInWithEmailButton.onClick.AddListener(() => ObjectManager.Instance.OnSignInWithEmailButtonPressed());

            ObjectManager.Instance.FirestoreRegistrationAsync = UserData.Instance.UpdateFirestoreUserData;
        }
    }
}
