using System;
using System.Threading.Tasks;
using Firebase.Auth;
using Firebase.Extensions;
using Firebase.Firestore;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.UI;
using UnityEngine.Events;
#pragma warning disable 4014

namespace InGameMoney
{
	internal class AccountTest : MonoBehaviour
	{
		[SerializeField] InputField _inputfMailAdress;
		[SerializeField] InputField _inputfPassword;
		[SerializeField] private GameObject canvasIap;
		[SerializeField] Toggle _autoLogin;

		public static UnityAction OnLogin = null;
		public static UnityAction OnLogout = null;
		public static FirebaseFirestore Db => db;

		private static FirebaseFirestore db;
		private UserData userdata;
		private FirebaseAuth auth;
		private FirebaseUser user;
		private bool signedIn;
		private static ITaskFault taskFault;
		public static IWriteUserData UserDataAccess;
		public InputField InputFieldMailAddress => _inputfMailAdress;
		public InputField InputFieldPassword => _inputfPassword;
		public Toggle AutoLogin => _autoLogin;

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

			_inputfMailAdress.text = userdata.data.mailAddress;
			_inputfPassword.text = userdata.data.password;
			canvasIap.SetActive(false);

			_autoLogin.isOn = userdata.data.autoLogin;
			if (string.IsNullOrEmpty(userdata.data.mailAddress)
			    || string.IsNullOrEmpty(userdata.data.password)
			)
			{
				_autoLogin.isOn = false;
				auth.SignOut();
				ObjectManager.Instance.Logs.text = $"Sign Out: {auth.CurrentUser}";
			}

			if (_autoLogin.isOn && signedIn)
			{
				ObjectManager.Instance.Logs.text = $"Sign in: {auth.CurrentUser.Email}";
				Login();
			}
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
			canvasIap.SetActive(true);
			_inputfMailAdress.interactable = false;
			_inputfPassword.interactable = false;
			OnLogin?.Invoke();
		}

		private void SignUpToFirestore()
		{
			Assert.IsNotNull(_inputfMailAdress.text, "Email is Missing !");
			Assert.IsNotNull(_inputfPassword.text, "Password is Missing");

			ObjectManager.Instance.Logs.text = "Adding Data ...";
			var docRef = db.Collection("Users").Document(_inputfMailAdress.text);

			var data = new User
			{
				Email = _inputfMailAdress.text,
				Password = _inputfPassword.text,
				MoneyBalance = 0,
				SignUpTimeStamp = FieldValue.ServerTimestamp
			};

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

		public void OnFirebaseAuthSignUp()
		{
			FirebaseAuthSignUp();
		}

		private async Task FirebaseAuthSignUp()
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
			ObjectManager.Instance.Logs.text = $"Firebase user created successfully Email {newUser.Result.Email} id {newUser.Result.UserId}";
			SignUpToFirestore();
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
			OnLogout?.Invoke();
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
