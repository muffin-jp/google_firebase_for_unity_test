using UnityEngine;

namespace InGameMoney
{
    public class RegisterGuestAccount : MonoBehaviour
    {
        public void OnRegisterGuestButton()
        {
            AccountTest.Instance.CanvasIAP.SetActive(false);
            AccountTest.Instance.SignInButton.interactable = false;
            AccountTest.Instance.SignOutButton.interactable = false;
            AccountTest.Instance.SignUpButton.interactable = true;
            AccountTest.Instance.InputFieldMailAddress.text = "";
            AccountTest.Instance.InputFieldPassword.text = "";
            AccountTest.Instance.InputFieldMailAddress.interactable = true;
            AccountTest.Instance.InputFieldPassword.interactable = true;
            AccountTest.OpenSignUpOptionView();
            gameObject.SetActive(false);
        }
    }
}
