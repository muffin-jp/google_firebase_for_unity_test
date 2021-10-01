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
	public class AccountManager : MonoBehaviour
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
		private FirebaseAuth auth;
		private FirebaseUser user;
		private bool signedIn;
		private static ITaskFault taskFault;
		private IAppleAuthManager appleAuthManager;
		private bool linkGuestAccount;
		private const string InstallationKey = "IsInstalled";
		
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
		public bool SignedIn => signedIn;

		public const string AppleUserIdKey = "AppleUserId";

		public static AccountManager Instance { get; private set; }

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
		
		private async void Start()
		{
			db = FirebaseFirestore.DefaultInstance;
			inputfPassword.inputType = InputField.InputType.Password;
			inputfPassword.asteriskChar = "$!£%&*"[5];
			UserDataAccess = new UserDataAccess();

			AppleAuthValidation();
			
			if (signedIn)
			{
				var validUser = await ReloadAndCheckErrorPreviousUser();
				Print.GreenLog($">>>> validUser {validUser} HasKey IsFirebaseAuthInitialized {PlayerPrefs.HasKey(InstallationKey)}");
				if (validUser && PlayerPrefs.HasKey(InstallationKey))
					InitializeAuthentication();
				else
					SignOut();
			}
		}

		private void AppleAuthValidation()
		{
			Print.GreenLog($">>>> AppleAuthValidation IsCurrentPlatformSupported {AppleAuthManager.IsCurrentPlatformSupported}");
			Assert.IsFalse(AppleAuthManager.IsCurrentPlatformSupported);
			// If the current platform is supported
			if (AppleAuthManager.IsCurrentPlatformSupported)
			{
				// Creates a default JSON deserializer, to transform JSON Native responses to C# instances
				var deserializer = new PayloadDeserializer();
				// Creates an Apple Authentication manager with the deserializer
				appleAuthManager = new AppleAuthManager(deserializer);
				Print.GreenLog($">>>> Creates an Apple Authentication manager");
			}
		}

		private void InitializeAuthentication()
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
				accountBase = new EmailAuth();
			}

			accountBase.Validate();
		}
		
		/// <summary>
		/// After Reinstall check if user is valid
		/// </summary>
		/// <returns></returns>
		private async Task<bool> ReloadAndCheckErrorPreviousUser()
		{
			var reloadAsync = auth.CurrentUser.ReloadAsync().ContinueWithOnMainThread(task => task);
			await reloadAsync;
			if (reloadAsync.Result.IsCanceled)
				return false;
			if (reloadAsync.Result.IsFaulted)
			{
				Print.RedLog($">>>> ReloadAndCheckErrorPreviousUser Exception {reloadAsync.Result.Exception?.Message}");
				return false;
			}
			return reloadAsync.Result.IsCompleted;
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
			Print.GreenLog($"Sign Out: {auth?.CurrentUser}");
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
		}
		
		public void OpenGameView()
		{
			ObjectManager.Instance.FirstBoot.SetActive(false);
			ObjectManager.Instance.InGameMoney.SetActive(true);
			signUpButton.interactable = false;
		}

		private void OnApplicationQuit()
		{
			UserDataAccess?.WritePersonalData();
			if (!PlayerPrefs.HasKey(FirebaseSignedWithAppleKey))
			{
				WriteUserData();
			}
		}

		public void WriteUserData(User data = null)
		{
			if (data == null)
				UserDataAccess?.WriteAccountData(inputfMailAdress.text, inputfPassword.text, autoLogin.isOn);
			else
			{
				UserDataAccess?.WriteAccountData(data.Email, data.Password, autoLogin.isOn);
			}
		}

		public async Task SignUpToFirestoreProcedure(User data)
		{
			await SignUpToFirestoreAsync(data);
			// Print.GreenLog($">>>> WriteUserData Email {data.Email}");
			// WriteUserData(data);
			Login();
		}

		public async Task SignUpToFirestoreAsync(User data)
		{
			Print.GreenLog($">>>> SignUpToFirestoreAsync Email {data.Email}");
			var docRef = db.Collection("Users").Document(data.Email);
			var task = docRef.SetAsync(data).ContinueWithOnMainThread(signUpTask => signUpTask);
			
			await task;
			
			if (task.Result.IsCanceled)
			{
				Print.GreenLog(">>>> Task IsCanceled !");
			}
			
			if (task.Result.IsFaulted)
			{
				Print.GreenLog(
					$">>>> SignUp task is faulted ! Exception: {task.Exception} Result exception {task.Result.Exception}");
			}

			if (task.Result.IsCompleted)
			{
				Print.GreenLog(
					$">>>> SignUpToFirestore IsCompleted, New Data Added, Now You can read and update data using Email : {data.Email}");
			}
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
			var emailAuth = new EmailAuth();
			emailAuth.PerformSignUpWithEmail();
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
			var userData = new User
			{
				Email = inputfMailAdress.text,
				Password = inputfPassword.text
			};
			UpdateLocalData(userData);
		}

		private void UpdateLocalData(User userData)
		{
			var dataAccess = UserDataAccess as UserDataAccess;
			dataAccess?.UpdateLocalData(userData);
		}

		public void UpdatePurchaseAndShop()
		{
			var dataAccess = UserDataAccess as UserDataAccess;
			dataAccess?.UpdatePurchaseAndShop();
		}

		public void Login()
		{
			SetAuthButtonInteraction();
			canvasIap.SetActive(true);
			inputfMailAdress.interactable = false;
			inputfPassword.interactable = false;
			OnLogin?.Invoke();
			if (!PlayerPrefs.HasKey(InstallationKey))
				PlayerPrefs.SetString(InstallationKey, "Yes");
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
			ObjectManager.Instance.AddDefaultActions();
		}

		public static void LinkAccountToFirestore(string email, string password)
		{
			var dataAccess = UserDataAccess as UserDataAccess;
			dataAccess?.UpdateFirestoreUserDataAfterCredentialLinked(email, password);
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

		public static bool IsFaultedTask(Task<FirebaseUser> task, bool isLogin = false)
		{
			if (!isLogin)
				taskFault = new SignUp();
			else
				taskFault = new Login();

			return taskFault.Validate(task);
		}

		public static void ResetPersonalData()
		{
			var userData = UserDataAccess as UserDataAccess;
			userData?.ResetPersonalData();
		}

		public static void InitPersonalData()
		{
			var userData = UserDataAccess as UserDataAccess;
			userData?.InitPersonalData();
		}
	}
}
