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
        [SerializeField] private GameObject canvasInAppPurchase;
        [SerializeField] private GameObject registerGuestAccount;
        [SerializeField] private Button loginButton;
        [SerializeField] private Button logoutButton;
        [SerializeField] private Button signUpWithEmailButton;
        [SerializeField] private Button signInOrTakeOver;
        [SerializeField] private Button signUpWithGuestButton;
        private Func<User, Task> firestoreRegistrationAsync;
        
        public PurchaseTest Purchase => purchase;
        public Text Logs => logs;
        public GameObject InGameMoney => inGameMoney;
        public GameObject FirstBoot => firstBoot;
        public Text FirstBootLogs => firstBootLogs;
        
        public ShopTest Shop => shop;
        public static ObjectManager Instance { get; private set; }
        public Button SignUpSignInWithEmailButton => signUpSignInWithEmailButton;
        public Text MoneyBalanceText => moneyBalanceText;
        
        public const string PurchasedMoneyPrefix = "Purchased Money :";
        
        public Func<User, Task> FirestoreRegistrationAsync
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
            AddListenerToButtons();
            AddActions();
        }

        private void AddActions()
        {
            firestoreRegistrationAsync = AccountTest.Instance.SignUpToFirestoreAsync;
        }

        private void AddListenerToButtons()
        {
            // Note SignUpSignInButton will act as SignUp button as default, until SignIn is clicked
            var signUpEmailButton = signUpSignInWithEmailButton;
            signUpEmailButton.onClick.AddListener(OnButtonSignUpWithEmail);
        }

        private void OnButtonSignUpWithEmail()
        {
            inGameMoney.SetActive(true);
            firstBoot.SetActive(false);
            canvasInAppPurchase.SetActive(false);
            registerGuestAccount.SetActive(false);
            loginButton.interactable = false;
            logoutButton.interactable = false;
        }

        public void OnButtonSignInWithEmail()
        {
            inGameMoney.SetActive(true);
            firstBoot.SetActive(false);
            canvasInAppPurchase.SetActive(false);
            registerGuestAccount.SetActive(false);
            signUpWithEmailButton.gameObject.SetActive(false);
            loginButton.interactable = true;
            logoutButton.interactable = false;
        }

        public void ResetFirstBootView()
        {
            signUpWithGuestButton.gameObject.SetActive(true);
            signInOrTakeOver.gameObject.SetActive(true);
        }
    }
}
