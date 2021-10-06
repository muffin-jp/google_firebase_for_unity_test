using UnityEngine;

namespace InGameMoney
{
    public class SignInButton : MonoBehaviour
    {
        public void OnButtonSignIn()
        {
            var signInWithEmailButton = ObjectManager.Instance.SignUpSignInWithEmailButton;
            signInWithEmailButton.onClick.AddListener(() => ObjectManager.Instance.OnButtonSignInWithEmail());

            ObjectManager.Instance.FirestoreRegistrationAsync = UserData.Instance.UpdateFirestoreUserData;
        }
    }
}
