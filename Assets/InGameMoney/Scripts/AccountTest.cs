using System;
using System.Text;
using System.Threading.Tasks;
using AppleAuth;
using AppleAuth.Enums;
using AppleAuth.Extensions;
using AppleAuth.Interfaces;
using AppleAuth.Native;
using Firebase.Auth;
using Firebase.Extensions;
using Firebase.Firestore;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.UI;
using UnityEngine.Events;
using UnityEngine.Serialization;

#pragma warning disable 4014

namespace InGameMoney
{
	internal class AccountTest : MonoBehaviour
	{
		[FormerlySerializedAs("_inputfMailAdress")] [SerializeField]
		private InputField inputfMailAdress;
		[FormerlySerializedAs("_inputfPassword")] [SerializeField]
		private InputField inputfPassword;
		[SerializeField] private GameObject canvasIap;
		[FormerlySerializedAs("_autoLogin")] [SerializeField]
		private Toggle autoLogin;
		[SerializeField] private Button signInButton;
		[SerializeField] private Button signOutButton;
		[SerializeField] private Button signUpButton;
		[SerializeField] private Button registerGuestAccount;

		private static FirebaseFirestore db;
		private static UserData userdata;
		private FirebaseAuth auth;
		private FirebaseUser user;
		private bool signedIn;
		private static ITaskFault taskFault;
		private IAppleAuthManager appleAuthManager;
		private bool linkGuestAccount;
		
		public const string FirebaseSignedWithAppleKey = "FirebaseSignedWithApple";
		public static UnityAction OnLogin = null;
		public static UnityAction OnLogout = null;
		public static FirebaseFirestore Db => db;
		public static IWriteUserData UserDataAccess;
		public InputField InputFieldMailAddress => inputfMailAdress;
		public InputField InputFieldPassword => inputfPassword;
		public Toggle AutoLogin => autoLogin;
		public GameObject CanvasIAP => canvasIap;
		public Button SignInButton => signInButton;
		public Button SignOutButton => signOutButton;
		public Button SignUpButton => signUpButton;
		public Button RegisterGuestAccount => registerGuestAccount;
		public static UserData Userdata => userdata;
		public bool SignedIn => signedIn;

		public const string AppleUserIdKey = "AppleUserId";

		public static AccountTest Instance { get; private set; }

		private void Awake()
		{
			if (Instance) Destroy(this);
			else Instance = this;
			InitializeFirebase();
		}
		
		private void InitializeFirebase ()
		{
			// FirebaseAuth.DefaultInstanceはSignOutしない限り最後ログインしているユーザーを維持する
			auth = FirebaseAuth.DefaultInstance;
			auth.StateChanged += AuthStateChanged;
			AuthStateChanged (this, null);
		}

		private void AuthStateChanged(object sender, EventArgs eventArgs)
		{
			if (auth.CurrentUser != user) {
				// SignOutしない限りCurrentUserはnullではない
				signedIn = user != auth.CurrentUser && auth.CurrentUser != null;
				user = auth.CurrentUser;
				switch (signedIn)
				{
					case false when user != null:
						ObjectManager.Instance.Logs.text = $"Signed out {user?.UserId}";
						break;
					case true:
						ObjectManager.Instance.Logs.text = $"Current user id {user?.UserId}";
						break;
				}
			}
		}
		
		private void Start()
		{
			db = FirebaseFirestore.DefaultInstance;
			userdata = UserData.Instance;
			UserDataAccess = new UserDataAccess(userdata);
			inputfPassword.inputType = InputField.InputType.Password;
			inputfPassword.asteriskChar = "$!£%&*"[5];
			userdata.Init();

			AppleAuthValidation();
			InitializeAuthentication();
		}

		private void AppleAuthValidation()
		{
			// If the current platform is supported
			if (AppleAuthManager.IsCurrentPlatformSupported)
			{
				// Creates a default JSON deserializer, to transform JSON Native responses to C# instances
				var deserializer = new PayloadDeserializer();
				// Creates an Apple Authentication manager with the deserializer
				appleAuthManager = new AppleAuthManager(deserializer);
				ObjectManager.Instance.FirstBootLogs.text = "AppleAuthValidation is called";
			}
		}

		private void InitializeAuthentication()
		{
			if (signedIn)
			{
				IAccountBase accountBase;
				if (auth.CurrentUser.IsAnonymous)
				{
					accountBase = new Guest();
				}
				else if (PlayerPrefs.HasKey(FirebaseSignedWithAppleKey))
				{
					accountBase = new AppleAuth(appleAuthManager);
				}
				else
				{
					accountBase = new NonGuest();
				}
				
				accountBase.Validate();
			}
		}

		private void Update()
		{
			// Updates the AppleAuthManager instance to execute
			// pending callbacks inside Unity's execution loop
			appleAuthManager?.Update();
		}

		public void SignOutBecauseLocalDataIsEmpty()
		{
			SignOut();
		}

		public void SignOut()
		{
			autoLogin.isOn = false;
			auth?.SignOut();
			ObjectManager.Instance.Logs.text = $"Sign Out: {auth?.CurrentUser}";
			OpenSignUpOptionView();
			PlayerPrefs.DeleteKey(FirebaseSignedWithAppleKey);
		}

		public static void OpenSignUpOptionView()
		{
			ObjectManager.Instance.InGameMoney.SetActive(false);
			ObjectManager.Instance.FirstBoot.SetActive(true);
			ObjectManager.Instance.ResetFirstBootView();
		}

		public void SetupUI(string emailAddress, string password, bool autoLogin)
		{
			inputfMailAdress.text = emailAddress;
			inputfPassword.text = password;
			this.autoLogin.isOn = autoLogin;
			canvasIap.SetActive(false);

			if (signedIn)
			{
				OpenGameView();
			}
		}
		
		public void OpenGameView()
		{
			ObjectManager.Instance.FirstBoot.SetActive(false);
			ObjectManager.Instance.InGameMoney.SetActive(true);
			signUpButton.interactable = false;
		}

		private void OnApplicationQuit()
		{
			OnLogout?.Invoke();
			if (!PlayerPrefs.HasKey(FirebaseSignedWithAppleKey))
			{
				WriteUserData();
			}
		}

		public void WriteUserData(User data = null)
		{
			if (data == null)
				UserDataAccess?.WriteData(inputfMailAdress.text, inputfPassword.text, autoLogin.isOn);
			else
			{
				UserDataAccess?.WriteData(data.Email, data.Password, autoLogin.isOn);
			}
		}

		public async Task SignUpToFirestoreProcedure(User data)
		{
			await SignUpToFirestoreAsync(data);
			WriteUserData();
			Login();
		}

		public async Task SignUpToFirestoreAsync(User data)
		{
			var docRef = db.Collection("Users").Document(data.Email);
			var task = docRef.SetAsync(data).ContinueWithOnMainThread(signUpTask => signUpTask);
			
			await task;
			
			if (task.Result.IsCanceled)
			{
				ObjectManager.Instance.Logs.text = "Task IsCanceled !";
			}
			
			if (task.Result.IsFaulted)
			{
				ObjectManager.Instance.FirstBootLogs.text = 
					$"SignUp task is faulted ! Exception: {task.Exception} Result exception {task.Result.Exception}";
			}

			if (task.Result.IsCompleted)
			{
				ObjectManager.Instance.FirstBootLogs.text =
					$"SignUpToFirestore, New Data Added, Now You can read and update data using Email : {data.Email}";
			}
			Debug.Log($">>>> SignUpToFirestoreAsync task.Result.IsCompleted {task.Result.IsCompleted}");
		}

		public User GetDefaultUserDataFromInputField()
		{
			Assert.IsNotNull(inputfMailAdress.text, "Email is Missing !");
			Assert.IsNotNull(inputfPassword.text, "Password is Missing");
			return new User
			{
				Email = inputfMailAdress.text,
				Password = inputfPassword.text,
				MoneyBalance = 0,
				SignUpTimeStamp = FieldValue.ServerTimestamp
			};
		}
		
		public void OnButtonSignUpWithEmailFirebaseAuth()
		{
			if (auth?.CurrentUser != null && auth.CurrentUser.IsAnonymous && !registerGuestAccount.gameObject.activeSelf)
			{
				LinkAuthWithEmailCredential();
			}
			else
			{
				FirebaseEmailAuthSignUp();
			}
		}

		private async Task FirebaseEmailAuthSignUp()
		{
			ObjectManager.Instance.Logs.text = "Creating User Account....";

			var task = auth.CreateUserWithEmailAndPasswordAsync(inputfMailAdress.text, inputfPassword.text)
				.ContinueWithOnMainThread(signUpTask => signUpTask);

			await task;

			if (task.Result.IsCanceled)
			{
				ObjectManager.Instance.Logs.text = "Create User With Email And Password was canceled.";
				return;
			}

			if (IsFaultedTask(task.Result)) return;

			var newUser = task.Result;
			ObjectManager.Instance.Logs.text =
				$"Firebase user created successfully Email {newUser.Result.Email} id {newUser.Result.UserId} DisplayName {newUser.Result.DisplayName}";

			await SignUpToFirestoreAsync(GetDefaultUserDataFromInputField());
			WriteUserData();
			Login();
		}

		public void OnButtonLoginFirebaseAuth()
		{
			ObjectManager.Instance.Logs.text = "Logging In User Account...";
			FirebaseEmailAuthLogin();
		}

		private async Task FirebaseEmailAuthLogin()
		{
			ObjectManager.Instance.Logs.text = "Logging In User Account...";
			var loginTask = auth.SignInWithEmailAndPasswordAsync(inputfMailAdress.text, inputfPassword.text)
				.ContinueWithOnMainThread(task => task);
			
			await loginTask;

			if (loginTask.Result.IsCanceled)
			{
				ObjectManager.Instance.Logs.text = $"SignIn With email {inputfMailAdress.text} And Password Async was canceled ";
				return;
			}
			
			if (IsFaultedTask(loginTask.Result, true)) return;
			
			var login = loginTask.Result;
			ObjectManager.Instance.Logs.text = $"Account Logged In, your user ID: {login.Result.UserId}";
			WriteUserData();
			Login();
			UserData.Instance.UpdateLocalData();
		}

		public void Login()
		{
			SetAuthButtonInteraction();
			canvasIap.SetActive(true);
			inputfMailAdress.interactable = false;
			inputfPassword.interactable = false;
			OnLogin?.Invoke();
		}

		public void OnButtonLogoutFirebaseAuth()
		{
			auth.SignOut();
			Logout();
			OpenSignUpOptionView();
		}

		private void Logout()
		{
			canvasIap.SetActive(false);
			inputfMailAdress.interactable = true;
			inputfPassword.interactable = true;
			signInButton.interactable = true;
			registerGuestAccount.interactable = true;
			OnLogout?.Invoke();
		}
		
		private void LinkAuthWithEmailCredential()
		{
			ObjectManager.Instance.Logs.text = "Linking Guest auth credential ...";
			var credential = EmailAuthProvider.GetCredential(inputfMailAdress.text, inputfPassword.text);
			var currentUser = auth.CurrentUser;

			currentUser.LinkWithCredentialAsync(credential).ContinueWith(task =>
			{
				if (task.IsCanceled) {
					ObjectManager.Instance.Logs.text = "LinkWithCredentialAsync was canceled.";
					return;
				}
				if (task.IsFaulted) {
					ObjectManager.Instance.Logs.text = $"LinkWithCredentialAsync encountered an error: {task.Exception}";
					return;
				}

				var newUser = task.Result;
				ObjectManager.Instance.Logs.text = $"Credentials successfully linked to Firebase userId {newUser.UserId}";
				LinkAccountToFirestore(inputfMailAdress.text, inputfPassword.text);
				SetAuthButtonInteraction();
			}, TaskScheduler.FromCurrentSynchronizationContext());
		}

		public static void LinkAccountToFirestore(string email, string password)
		{
			UserData.Instance.UpdateFirestoreUserDataAfterCredentialLinked(email, password);
		}
		
		public void SetAuthButtonInteraction()
		{
			signInButton.interactable = false;
			signUpButton.interactable = false;
			signOutButton.interactable = true;
		}

		public void OnSignInWithAppleButton()
		{
			if (!AppleAuthManager.IsCurrentPlatformSupported) return;
			var appleAuth = new AppleAuth(appleAuthManager);
			
			appleAuth.PerformLoginWithAppleIdAndFirebase();
		}

		public void SetupLogin(UserData userData)
		{
			SetupUI(userData.AccountData.mailAddress, userData.AccountData.password, userData.AccountData.autoLogin);
			registerGuestAccount.interactable = false;
			if (AutoLogin.isOn)
			{
				ObjectManager.Instance.Logs.text = $"Sign in: {auth.CurrentUser.Email}";
				Login();
			}
		}
		
		public static bool IsFaultedTask(Task<FirebaseUser> task, bool isLogin = false)
		{
			if (!isLogin)
				taskFault = new SignUp();
			else
				taskFault = new Login();

			return taskFault.Validate(task);
		}
	}
}
