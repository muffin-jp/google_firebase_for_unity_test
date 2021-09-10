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
		public static UserData Userdata => userdata;
		public bool SignedIn => signedIn;
		public Button RegisterGuestAccount => registerGuestAccount;
		public const string AppleUserIdKey = "AppleUserId";

		public static AccountTest Instance { get; private set; }

		private void Awake()
		{
			if (Instance) Destroy(this);
			else Instance = this;
			InitializeFirebase();
		}

		private void Start()
		{
			db = FirebaseFirestore.DefaultInstance;
			userdata = UserData.Instance;
			UserDataAccess = new UserDataAccess(userdata);
			inputfPassword.inputType = InputField.InputType.Password;
			inputfPassword.asteriskChar = "$!£%&*"[5];
			userdata.Init();

			CurrentUserValidation();
		}

		private void CurrentUserValidation()
		{
			IAccountBase accountBase;
			if (auth?.CurrentUser != null && auth.CurrentUser.IsAnonymous)
			{
				accountBase = new Guest();
			}
			else
			{
				// If the current platform is supported
				if (AppleAuthManager.IsCurrentPlatformSupported)
				{
					accountBase = new AppleAuth();
					appleAuthManager = ((AppleAuth) accountBase).AppleAuthManager;
				}
				else 
					accountBase = new NonGuest();
			}
			
			accountBase.Validate();
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
			ObjectManager.Instance.InGameMoney.SetActive(false);
			ObjectManager.Instance.FirstBoot.SetActive(true);
		}

		public void SetupUI(string emailAddress, string password, bool autoLogin)
		{
			inputfMailAdress.text = emailAddress;
			inputfPassword.text = password;
			this.autoLogin.isOn = autoLogin;
			canvasIap.SetActive(false);

			if (signedIn)
			{
				OpenLoginView();
			}
		}
		
		private void OpenLoginView()
		{
			ObjectManager.Instance.FirstBoot.SetActive(false);
			ObjectManager.Instance.InGameMoney.SetActive(true);
			signUpButton.interactable = false;
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

		private void OnApplicationQuit()
		{
			OnLogout?.Invoke();
			WriteUserData();
		}

		private void WriteUserData()
		{
			UserDataAccess.WriteData(inputfMailAdress.text, inputfPassword.text, autoLogin.isOn);
		}

		private void ProceedAfterLogin()
		{
			SetAuthButtonInteraction();
			canvasIap.SetActive(true);
			inputfMailAdress.interactable = false;
			inputfPassword.interactable = false;
			OnLogin?.Invoke();
		}

		public async Task SignUpToFirestoreProcedure(User data)
		{
			await SignUpToFirestoreAsync(data);
			WriteUserData();
			ProceedAfterLogin();
		}

		private async Task SignUpToFirestoreAsync(User data)
		{
			Assert.IsNotNull(inputfMailAdress.text, "Email is Missing !");
			Assert.IsNotNull(inputfPassword.text, "Password is Missing");
			
			var docRef = db.Collection("Users").Document(inputfMailAdress.text);
			var task = docRef.SetAsync(data).ContinueWithOnMainThread(signUpTask => signUpTask);
			if (task.IsCanceled)
			{
				ObjectManager.Instance.Logs.text = "Task IsCanceled !";
			}

			await task;
			
			if (task.IsFaulted)
			{
				ObjectManager.Instance.Logs.text = 
					$"SignUp task is faulted ! Exception: {task.Exception} Result exception {task.Result.Exception}";
			}

			if (task.IsCompleted)
			{
				ObjectManager.Instance.Logs.text =
					$"SignUpToFirestore, New Data Added, Now You can read and update data using id : {inputfMailAdress.text}";
			}
		}

		public User GetDefaultUserData()
		{
			return new User
			{
				Email = inputfMailAdress.text,
				Password = inputfPassword.text,
				MoneyBalance = 0,
				SignUpTimeStamp = FieldValue.ServerTimestamp
			};
		}
		
		public void OnButtonSignUpFirebaseAuth()
		{
			if (auth?.CurrentUser != null && auth.CurrentUser.IsAnonymous && !registerGuestAccount.gameObject.activeSelf)
			{
				LinkAuthCredential();
			}
			else
			{
				FirebaseAuthSignUp();
			}
		}

		private async Task FirebaseAuthSignUp()
		{
			ObjectManager.Instance.Logs.text = "Creating User Account....";

			var task = auth.CreateUserWithEmailAndPasswordAsync(inputfMailAdress.text, inputfPassword.text)
				.ContinueWithOnMainThread(signUpTask => signUpTask);

			if (task.IsCanceled)
			{
				ObjectManager.Instance.Logs.text = "Create User With Email And Password was canceled.";
				return;
			}

			await task;

			if (IsFaultedTask(task.Result)) return;

			var newUser = task.Result;
			ObjectManager.Instance.Logs.text =
				$"Firebase user created successfully Email {newUser.Result.Email} id {newUser.Result.UserId} DisplayName {newUser.Result.DisplayName}";

			await SignUpToFirestoreAsync(GetDefaultUserData());
			WriteUserData();
			ProceedAfterLogin();
		}

		public void OnButtonLoginFirebaseAuth()
		{
			ObjectManager.Instance.Logs.text = "Logging In User Account...";
			ProceedFirebaseAuthLogin();
		}

		private async Task ProceedFirebaseAuthLogin()
		{
			ObjectManager.Instance.Logs.text = "Logging In User Account...";
			var loginTask = auth.SignInWithEmailAndPasswordAsync(inputfMailAdress.text, inputfPassword.text)
				.ContinueWithOnMainThread(task => task);

			if (loginTask.IsCanceled)
			{
				ObjectManager.Instance.Logs.text = $"SignIn With email {inputfMailAdress.text} And Password Async was canceled ";
				return;
			}

			await loginTask;
			
			if (IsFaultedTask(loginTask.Result, true)) return;
			
			var login = loginTask.Result;
			ObjectManager.Instance.Logs.text = $"Account Logged In, your user ID: {login.Result.UserId}";
			Login();
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
		
		private void LinkAuthCredential()
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
				LinkAccountToFirestore();
				SetAuthButtonInteraction();
			}, TaskScheduler.FromCurrentSynchronizationContext());
		}

		private static void LinkAccountToFirestore()
		{
			UserData.Instance.UpdateFirestoreUserDataAfterCredentialLinked();
		}
		
		private void SetAuthButtonInteraction()
		{
			signInButton.interactable = false;
			signUpButton.interactable = false;
			signOutButton.interactable = true;
		}

		public void OnSignInWithAppleButton()
		{	
			SignInWithApple();
		}

		private void SignInWithApple()
		{
			var loginArgs = new AppleAuthLoginArgs(LoginOptions.IncludeEmail | LoginOptions.IncludeFullName);
			ObjectManager.Instance.FirstBootLogs.text = $"SignInWithApple... {loginArgs.Options}";
			appleAuthManager.LoginWithAppleId(
				loginArgs,
				credential =>
				{
					// Obtained credential, cast it to IAppleIDCredential
					var appleIdCredential = credential as IAppleIDCredential;
					if (appleIdCredential != null)
					{
						// Apple User ID
						// You should save the user ID somewhere in the device
						var userId = appleIdCredential.User;
						PlayerPrefs.SetString(AppleUserIdKey, userId);

						// Email (Received ONLY in the first login)
						var email = appleIdCredential.Email;

						// Full name (Received ONLY in the first login)
						var fullName = appleIdCredential.FullName;

						// Identity token
						var identityToken = Encoding.UTF8.GetString(
							appleIdCredential.IdentityToken,
							0,
							appleIdCredential.IdentityToken.Length);

						// Authorization code
						var authorizationCode = Encoding.UTF8.GetString(
							appleIdCredential.AuthorizationCode,
							0,
							appleIdCredential.AuthorizationCode.Length);

						ObjectManager.Instance.FirstBootLogs.text = $"Succeed SignInWithApple userId {userId} " +
						                                            $"email {email} fullName {fullName}" +
						                                            $"identityToken {identityToken} " +
						                                            $"authorizationCode {authorizationCode}";
					}
					else ObjectManager.Instance.FirstBootLogs.text = $"appleIdCredential is null";

				}, error =>
				{
					ObjectManager.Instance.FirstBootLogs.text =
						$"Error SignInWithApple {error.GetAuthorizationErrorCode()}";
				});
		}
		
		private static bool IsFaultedTask(Task<FirebaseUser> task, bool isLogin = false)
		{
			if (!isLogin)
				taskFault = new SignUp();
			else
				taskFault = new Login();

			return taskFault.Validate(task);
		}
	}
}
