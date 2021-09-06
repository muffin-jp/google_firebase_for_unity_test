using System;
using System.Threading.Tasks;
using Firebase.Auth;
using Firebase.Extensions;
using Firebase.Firestore;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.UI;
using UnityEngine.Events;
using Object = UnityEngine.Object;

#pragma warning disable 4014

namespace InGameMoney
{
	internal class AccountTest : MonoBehaviour
	{
		[SerializeField] InputField _inputfMailAdress;
		[SerializeField] InputField _inputfPassword;
		[SerializeField] private GameObject canvasIap;
		[SerializeField] Toggle _autoLogin;
		[SerializeField] private Button signInButton;
		[SerializeField] private Button signOutButton;
		[SerializeField] private Button signUpButton;
		[SerializeField] private Button registerGuestAccount;

		public static UnityAction OnLogin = null;
		public static UnityAction OnLogout = null;
		public static FirebaseFirestore Db => db;

		private static FirebaseFirestore db;
		private static UserData userdata;
		private FirebaseAuth auth;
		private FirebaseUser user;
		private bool signedIn;
		private static ITaskFault taskFault;
		public static IWriteUserData UserDataAccess;
		public InputField InputFieldMailAddress => _inputfMailAdress;
		public InputField InputFieldPassword => _inputfPassword;
		public Toggle AutoLogin => _autoLogin;
		public GameObject CanvasIAP => canvasIap;
		public Button SignInButton => signInButton;
		public Button SignOutButton => signOutButton;
		public Button SignUpButton => signUpButton;

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
			_inputfPassword.inputType = InputField.InputType.Password;
			_inputfPassword.asteriskChar = "$!£%&*"[5];
			userdata.Init();

			CurrentUserValidation();
		}

		private void CurrentUserValidation()
		{
			if (auth?.CurrentUser != null && auth.CurrentUser.IsAnonymous)
			{
				if (string.IsNullOrEmpty(userdata.data.mailAddress) || string.IsNullOrEmpty(userdata.data.password))
				{
					SignOutBecauseLocalDataIsEmpty();
					return;
				}
				
				SetupUI($"匿名@{auth.CurrentUser.UserId}", $"vw-guest-pass@{auth.CurrentUser.UserId}", false);
				Login();
			}
			else
			{
				if (string.IsNullOrEmpty(userdata.data.mailAddress) || string.IsNullOrEmpty(userdata.data.password))
				{
					SignOutBecauseLocalDataIsEmpty();
					return;
				}

				if (signedIn)
				{
					SetupUI(userdata.data.mailAddress, userdata.data.password, userdata.data.autoLogin);
					registerGuestAccount.interactable = false;
					if (_autoLogin.isOn)
					{
						ObjectManager.Instance.Logs.text = $"Sign in: {auth.CurrentUser.Email}";
						Login();
					}
				}
			}
		}

		private void SignOutBecauseLocalDataIsEmpty()
		{
			_autoLogin.isOn = false;
			auth?.SignOut();
			ObjectManager.Instance.Logs.text = $"Sign Out: {auth?.CurrentUser}";
			ObjectManager.Instance.InGameMoney.SetActive(false);
			ObjectManager.Instance.FirstBoot.SetActive(true);
		}

		public void SetupUI(string emailAddress, string password, bool autoLogin)
		{
			_inputfMailAdress.text = emailAddress;
			_inputfPassword.text = password;
			_autoLogin.isOn = autoLogin;
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
			UserDataAccess.WriteData(_inputfMailAdress.text, _inputfPassword.text, _autoLogin.isOn);
		}

		private void ProceedAfterLogin()
		{
			SetAuthButtonInteraction();
			canvasIap.SetActive(true);
			_inputfMailAdress.interactable = false;
			_inputfPassword.interactable = false;
			OnLogin?.Invoke();
		}

		public void SignUpToFirestore(User data)
		{
			Assert.IsNotNull(_inputfMailAdress.text, "Email is Missing !");
			Assert.IsNotNull(_inputfPassword.text, "Password is Missing");

			ObjectManager.Instance.Logs.text = "Adding Data ...";
			var docRef = db.Collection("Users").Document(_inputfMailAdress.text);

			docRef.SetAsync(data).ContinueWithOnMainThread(task =>
			{
				if (task.IsCanceled)
				{
					ObjectManager.Instance.Logs.text = "An Error Occurred !";
					return;
				}

				if (task.IsFaulted)
				{
					ObjectManager.Instance.Logs.text = "Add Data Failed Failed !";
					return;
				}

				if (task.IsCompleted)
				{
					ObjectManager.Instance.Logs.text =
						$"New Data Added, Now You can read and update data using id : {_inputfMailAdress.text}";
					WriteUserData();
					ProceedAfterLogin();
				}
			});
		}

		public User GetDefaultUserData()
		{
			return new User
			{
				Email = _inputfMailAdress.text,
				Password = _inputfPassword.text,
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
				FirebaseAuthSignUp(true);
			}
		}

		private async Task FirebaseAuthSignUp(bool signUpToFirestore = false)
		{
			ObjectManager.Instance.Logs.text = "Creating User Account....";

			var task = auth.CreateUserWithEmailAndPasswordAsync(_inputfMailAdress.text, _inputfPassword.text)
				.ContinueWithOnMainThread(signUpTask => signUpTask);

			if (task.IsCanceled)
			{
				ObjectManager.Instance.Logs.text = "Create User With Email And Password was canceled.";
				return;
			}

			await task;
			
			if (IsFaultedTask(task.Result)) return;
			
			var newUser = task.Result;
			ObjectManager.Instance.Logs.text = $"Firebase user created successfully Email {newUser.Result.Email} id {newUser.Result.UserId} DisplayName {newUser.Result.DisplayName}";
			if (signUpToFirestore) SignUpToFirestore(GetDefaultUserData());
		}

		public void OnButtonLoginFirebaseAuth()
		{
			ObjectManager.Instance.Logs.text = "Logging In User Account...";
			ProceedFirebaseAuthLogin();
		}

		private async Task ProceedFirebaseAuthLogin()
		{
			ObjectManager.Instance.Logs.text = "Logging In User Account...";
			var loginTask = auth.SignInWithEmailAndPasswordAsync(_inputfMailAdress.text, _inputfPassword.text)
				.ContinueWithOnMainThread(task => task);

			if (loginTask.IsCanceled)
			{
				ObjectManager.Instance.Logs.text = $"SignIn With email {_inputfMailAdress.text} And Password Async was canceled ";
				return;
			}

			await loginTask;
			
			if (IsFaultedTask(loginTask.Result, true)) return;
			
			var login = loginTask.Result;
			ObjectManager.Instance.Logs.text = $"Account Logged In, your user ID: {login.Result.UserId}";
			Login();
		}

		private void Login()
		{
			SetAuthButtonInteraction();
			canvasIap.SetActive(true);
			_inputfMailAdress.interactable = false;
			_inputfPassword.interactable = false;
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
			_inputfMailAdress.interactable = true;
			_inputfPassword.interactable = true;
			signInButton.interactable = true;
			registerGuestAccount.interactable = true;
			OnLogout?.Invoke();
		}
		
		private void LinkAuthCredential()
		{
			ObjectManager.Instance.Logs.text = "Linking Guest auth credential ...";
			var credential = EmailAuthProvider.GetCredential(_inputfMailAdress.text, _inputfPassword.text);
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
				canvasIap.SetActive(true);
			}, TaskScheduler.FromCurrentSynchronizationContext());
		}

		private void LinkAccountToFirestore()
		{
			UserData.Instance.UpdateFirestoreUserDataAfterCredentialLinked(_inputfMailAdress.text, _inputfPassword.text);
		}
		
		private void SetAuthButtonInteraction()
		{
			signInButton.interactable = false;
			signUpButton.interactable = false;
			signOutButton.interactable = true;
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
