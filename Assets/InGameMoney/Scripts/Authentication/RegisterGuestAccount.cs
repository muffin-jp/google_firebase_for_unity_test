using UnityEngine;

namespace InGameMoney
{
    public class RegisterGuestAccount : MonoBehaviour
    {
        public void OnRegisterGuestButton()
        {
            ObjectManager.Instance.CanvasIAP.SetActive(false);
            ObjectManager.Instance.SignInButton.interactable = false;
            ObjectManager.Instance.SignOutButton.interactable = false;
            ObjectManager.Instance.SignUpButton.interactable = true;
            ObjectManager.Instance.InputFieldMailAddress.text = "";
            ObjectManager.Instance.InputFieldPassword.text = "";
            ObjectManager.Instance.InputFieldMailAddress.interactable = true;
            ObjectManager.Instance.InputFieldPassword.interactable = true;
            AccountManager.OpenSignUpOptionView();
            gameObject.SetActive(false);
        }
    }
}
