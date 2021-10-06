using UnityEngine;

namespace InGameMoney
{
    public class RegisterGuestAccount : MonoBehaviour
    {
        public void OnRegisterGuestButton()
        {
            AccountManager.Instance.CanvasIAP.SetActive(false);
            AccountManager.Instance.SignInButton.interactable = false;
            AccountManager.Instance.SignOutButton.interactable = false;
            AccountManager.Instance.SignUpButton.interactable = true;
            AccountManager.Instance.InputFieldMailAddress.text = "";
            AccountManager.Instance.InputFieldPassword.text = "";
            AccountManager.Instance.InputFieldMailAddress.interactable = true;
            AccountManager.Instance.InputFieldPassword.interactable = true;
            AccountManager.OpenSignUpOptionView();
            gameObject.SetActive(false);
        }
    }
}
