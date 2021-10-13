using System;
using System.Threading.Tasks;
using AppleAuth;
using AppleAuth.Native;
using Firebase.Auth;
using Firebase.Extensions;
using Firebase.Firestore;
using Gravitons.UI.Modal;
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
		private static FirebaseAuth auth;
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
		public static FirebaseUser CurrentUser => auth.CurrentUser;

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
			var userData = ((UserDataAccess)UserDataAccess).UserData;
			if (auth.CurrentUser.IsAnonymous)
			{
				accountBase = new Guest(FirebaseAuth.DefaultInstance);
			}
			else if (PlayerPrefs.HasKey(FirebaseSignedWithAppleKey))
			{
				accountBase = new AppleAuth(appleAuthManager);
			}
			else
			{
				accountBase = new EmailAuth(FirebaseAuth.DefaultInstance, userData);
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
			if (ObjectManager.Instance.InGameMoney) ObjectManager.Instance.InGameMoney.SetActive(false);
			if (ObjectManager.Instance.FirstBoot) ObjectManager.Instance.FirstBoot.SetActive(true);
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

		public async Task SignUpToFirestoreProcedure(User data, bool login = true)
		{
			await SignUpToFirestoreAsync(data);
			if (login) Login();
		}

		public async Task SignUpToFirestoreAsync(User data, bool login = false)
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
			var userData = ((UserDataAccess)UserDataAccess).UserData;
			var emailAuth = new EmailAuth(FirebaseAuth.DefaultInstance, userData);
			emailAuth.PerformSignUpWithEmail(inputfMailAdress.text, inputfPassword.text);
		}
		
		public void OnButtonLoginWithEmailAuth()
		{
			var userData = ((UserDataAccess)UserDataAccess).UserData;
			var emailAuth = new EmailAuth(FirebaseAuth.DefaultInstance, userData);
			emailAuth.FirebaseEmailAuthLogin(inputfMailAdress.text, inputfPassword.text);
		}

		public void UpdateLocalData(User userData)
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
			ObjectManager.Instance.ForgotPasswordButton.gameObject.SetActive(false);
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
			ObjectManager.Instance.DynamicListeners();
		}

		public static void UpdateFirestoreUserDataAfterCredentialLinked(string email, string password)
		{
			UpdateFirestoreUserData(email, password);
		}

		private static void UpdateFirestoreUserData(string email, string password, bool login = true)
		{
			var dataAccess = UserDataAccess as UserDataAccess;
			dataAccess?.UpdateFirestoreUserData(email, password, login);
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

		/// <summary>
		/// Reset Password current signed in user.
		/// Note this method doesn't work if auth.CurrentUser is null,
		/// it means user must login once using email authentication,
		/// this is also useful if user want to change password
		/// </summary>
		/// <param name="newPassword"></param>
		public static async void ResetEmailAuthPassword(string newPassword)
		{
			var userData = ((UserDataAccess)UserDataAccess).UserData;
			var emailAuth = new EmailAuth(FirebaseAuth.DefaultInstance, userData);
			
			var resetPasswordIsCompleted = await emailAuth.UpdatePasswordAsync(newPassword);

			if (resetPasswordIsCompleted)
			{
				UserDataAccess.WriteAccountData(userData.AccountData.mailAddress, newPassword, false);
				ModalManager.Show("Successfully Reset Password", "You can now sign in\n with your new password", new[]
				{
					new ModalButton
					{
						Text = "OK",
						CallbackWithParams = OnFinishResetPasswordCallback,
						Params = new object[]{ $"{userData.AccountData.mailAddress}", $"{newPassword}"}
					}
				});
			}
			else
			{
				Print.RedLog($">>>> Password reset failed!");
			}
		}

		public static async void SendPasswordResetEmail(string emailAddress)
		{
			var userData = ((UserDataAccess)UserDataAccess).UserData;
			var emailAuth = new EmailAuth(FirebaseAuth.DefaultInstance, userData);
			PlayerPrefs.SetString(EmailAuth.NeedToUpdatePassword, "yes");
			
			var sendPasswordResetEmailCompleted = await emailAuth.SendPasswordResetEmail(emailAddress);

			if (sendPasswordResetEmailCompleted)
			{
				ModalManager.Show("Successfully Send Password Reset Email", $"Please check your email at {emailAddress}", new[]
				{
					new ModalButton
					{
						Text = "OK",
						Callback = ObjectManager.Instance.OnFinishResetPassword
					}
				});
			}
			else
			{
				Print.RedLog($">>>> Password reset failed!");
			}
		}

		private static void OnFinishResetPasswordCallback(object[] items)
		{
			ObjectManager.Instance.OnFinishResetPassword();
			UpdateFirestoreUserData(items[0] as string, items[1] as string, false);
		}

		public static void InitPersonalData()
		{
			var userData = UserDataAccess as UserDataAccess;
			userData?.InitPersonalData();
		}
	}
}
