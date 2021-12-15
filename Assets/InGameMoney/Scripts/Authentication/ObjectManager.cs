using System;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

namespace InGameMoney
{
    public class ObjectManager : MonoBehaviour
    {
        [SerializeField] private PurchaseTest purchase;
        [SerializeField] private Text logs;
        [SerializeField] private Text firstBootLogs;
        [SerializeField] private Text moneyBalanceText;
        [SerializeField] private ShopTest shop;
        [SerializeField] private GameObject inGameMoney;
        [SerializeField] private GameObject firstBoot;
        [SerializeField] private Button signUpSignInWithEmailButton;
        [SerializeField] private GameObject canvasAccount;
        [SerializeField] private GameObject canvasInAppPurchase;
        [SerializeField] private GameObject canvasResetPassword;
        [SerializeField] private GameObject registerGuestAccount;
        [SerializeField] private Button loginButton;
        [SerializeField] private Button logoutButton;
        [SerializeField] private Button signUpWithEmailButton;
        [SerializeField] private Button signInOrTakeOver;
        [SerializeField] private Button signUpWithGuestButton;
        [SerializeField] private InputField mailInputField;
        [SerializeField] private InputField passwordInputField;
        [SerializeField] private Button forgotPasswordButton;
        [SerializeField] private Button resetPasswordButton;
        [SerializeField] private InputField newPasswordInput;
        [SerializeField] private InputField confirmNewPasswordInput;
        [SerializeField] private InputField resetPasswordEmailInputField;
        [SerializeField] private Button contactSupportButton;
        [SerializeField] private GameObject contactSupport;
        private Func<User, bool, Task> firestoreRegistrationAsync;
        
        [SerializeField] private InputField inputFieldMailAddress;
        [SerializeField] private InputField inputFieldPassword;
        [SerializeField] private GameObject canvasIap;
        [SerializeField] private Toggle autoLogin;
        [SerializeField] private Button signInButton;
        [SerializeField] private Button signOutButton;
        [SerializeField] private Button signUpButton;
        [SerializeField] private Button registerGuestAccountButton;

        public InputField InputFieldMailAddress => inputFieldMailAddress;
        public InputField InputFieldPassword => inputFieldPassword;
        public GameObject CanvasIAP => canvasIap;
        public Toggle AutoLogin => autoLogin;
        public Button SignInButton => signInButton;
        public Button SignOutButton => signOutButton;
        public Button SignUpButton => signUpButton;
        public Button RegisterGuestAccountButton => registerGuestAccountButton;
        
        public PurchaseTest Purchase => purchase;
        public Text Logs => logs;
        public GameObject InGameMoney => inGameMoney;
        public GameObject FirstBoot => firstBoot;
        public Text FirstBootLogs => firstBootLogs;
        
        public ShopTest Shop => shop;
        public static ObjectManager Instance { get; private set; }
        public Button SignUpSignInWithEmailButton => signUpSignInWithEmailButton;
        public GameObject RegisterGuestAccount => registerGuestAccount;
        public Text MoneyBalanceText => moneyBalanceText;
        public Button ForgotPasswordButton => forgotPasswordButton;
        
        public const string PurchasedMoneyPrefix = "Purchased Money :";
        
        public Func<User, bool, Task> FirestoreRegistrationAsync
        {
            get => firestoreRegistrationAsync;
            set => firestoreRegistrationAsync = value;
        }

        private void Awake()
        {
            if (Instance) Destroy(this);
            else Instance = this;
            DontDestroyOnLoad(this);
        }

        private void Start()
        {
            DynamicListeners();
            AddPassiveListeners();
        }

        private void AddPassiveListeners()
        {
            forgotPasswordButton.onClick.AddListener(OnForgotPasswordButtonPressed);
            resetPasswordButton.onClick.AddListener(OnResetPasswordButtonPressed);
            contactSupportButton.onClick.AddListener(OnContactSupportButtonPressed);
        }

        public void DynamicListeners()
        {
            RemoveAndAddListenerToButtons();
            AddActions();
        }

        private void AddActions()
        {
            firestoreRegistrationAsync = AccountManager.Instance.SignUpToFirestoreAsync;
        }

        private void RemoveAndAddListenerToButtons()
        {
            // Note SignUpSignInButton will act as SignUp button as default, until SignIn is clicked
            var signUpEmailButton = signUpSignInWithEmailButton;
            signUpEmailButton.onClick.RemoveAllListeners();
            signUpEmailButton.onClick.AddListener(OnSignUpWithEmailButtonPressed);
        }

        private void OnSignUpWithEmailButtonPressed()
        {
            inGameMoney.SetActive(true);
            firstBoot.SetActive(false);
            canvasInAppPurchase.SetActive(false);
            registerGuestAccount.SetActive(false);
            loginButton.interactable = false;
            logoutButton.interactable = false;
            signUpWithEmailButton.gameObject.SetActive(true);
            signUpWithEmailButton.interactable = true;
            forgotPasswordButton.gameObject.SetActive(false);
        }

        public void OnSignInWithEmailButtonPressed()
        {
            inGameMoney.SetActive(true);
            firstBoot.SetActive(false);
            canvasInAppPurchase.SetActive(false);
            registerGuestAccount.SetActive(false);
            signUpWithEmailButton.gameObject.SetActive(false);
            loginButton.interactable = true;
            logoutButton.interactable = false;
            ResetInputField();
            mailInputField.interactable = true;
            passwordInputField.interactable = true;
            forgotPasswordButton.gameObject.SetActive(true);
        }

        public void ResetFirstBootView()
        {
            if (signUpWithGuestButton) signUpWithGuestButton.gameObject.SetActive(true);
            if (signInOrTakeOver) signInOrTakeOver.gameObject.SetActive(true);
            if (forgotPasswordButton) forgotPasswordButton.gameObject.SetActive(false);
        }

        public void ResetInputField()
        {
            mailInputField.text = "";
            passwordInputField.text = "";
        }

        private void OnForgotPasswordButtonPressed()
        {
            canvasResetPassword.SetActive(true);
            canvasAccount.SetActive(false);
            if (AccountManager.Instance.SignedIn)
            {
                ResetPasswordInputSetActive(true);
            }
            else
            {
                SendResetPasswordEmailView();
            }
        }

        private void SendResetPasswordEmailView()
        {
            ResetPasswordInputSetActive(false);
            resetPasswordButton.onClick.RemoveAllListeners();
            resetPasswordButton.onClick.AddListener(OnSendResetPasswordEmail);
        }

        public void OnSendPasswordResetEmailInputChange()
        {
            resetPasswordButton.interactable = !string.IsNullOrEmpty(resetPasswordEmailInputField.text);
        }

        private void OnSendResetPasswordEmail()
        {
            AccountManager.SendPasswordResetEmail(resetPasswordEmailInputField.text);
        }

        private void ResetPasswordInputSetActive(bool activate)
        {
            newPasswordInput.gameObject.SetActive(activate);
            confirmNewPasswordInput.gameObject.SetActive(activate);
            resetPasswordEmailInputField.gameObject.SetActive(!activate);
        }

        public void OnInputConfirmPasswordChanged()
        {
            if (newPasswordInput.text != confirmNewPasswordInput.text)
            {
                resetPasswordButton.interactable = false;
                return;
            }
            resetPasswordButton.interactable = true;
        }
        
        private void OnResetPasswordButtonPressed()
        {
            if (newPasswordInput.text != confirmNewPasswordInput.text) return;
            AccountManager.ResetEmailAuthPassword(confirmNewPasswordInput.text);
            ResetCanvasPasswordObjects();
        }

        public void OnFinishResetPassword()
        {
            canvasResetPassword.SetActive(false);
            canvasAccount.SetActive(true);
            OnSignInWithEmailButtonPressed();
        }

        private void ResetCanvasPasswordObjects()
        {
            resetPasswordButton.interactable = false;
            newPasswordInput.text = "";
            confirmNewPasswordInput.text = "";
        }
        
        private void OnContactSupportButtonPressed()
        {
            contactSupport.SetActive(true);
            inGameMoney.SetActive(false);
            contactSupportButton.gameObject.SetActive(false);
        }

        public void CloseContactForm()
        {
            contactSupport.SetActive(false);
            inGameMoney.SetActive(true);
            contactSupportButton.gameObject.SetActive(true);
        }
    }
}
