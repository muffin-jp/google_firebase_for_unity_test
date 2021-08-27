using Firebase.Extensions;
using Firebase.Firestore;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.UI;
using UnityEngine.Events;

namespace InGameMoney
{

	class AccountTest : MonoBehaviour
	{
		[SerializeField] InputField _inputfMailAdress;
		[SerializeField] InputField _inputfPassword;
		[SerializeField] GameObject _canvas;
		[SerializeField] Toggle _autoLogin;

		public static UnityAction onLogin = null;
		public static UnityAction onLogout = null;
		public static FirebaseFirestore Db => db;

		private static FirebaseFirestore db;
		private UserData userdata;

		public static AccountTest Instance { get; private set; }

		private void Awake()
		{
			if (Instance) Destroy(this);
			else Instance = this;
		}

		private void Start()
		{
			db = FirebaseFirestore.DefaultInstance;
			userdata = UserData.Instance;
			_inputfPassword.inputType = InputField.InputType.Password;
			_inputfPassword.asteriskChar = "$!£%&*"[5];
			userdata.Init();

			_inputfMailAdress.text = userdata.data.mailAddress;
			_inputfPassword.text = userdata.data.password;
			_canvas.SetActive(false);

			_autoLogin.isOn = userdata.data.autoLogin;
			if (string.IsNullOrEmpty(userdata.data.mailAddress)
			    || string.IsNullOrEmpty(userdata.data.password)
			)
			{
				_autoLogin.isOn = false;
			}

			if (_autoLogin.isOn)
			{
				OnButtonLogin();
			}
		}

		void OnApplicationQuit()
		{
			onLogout?.Invoke();
			WriteUserData();
		}

		private void WriteUserData()
		{
			userdata.data.mailAddress = _inputfMailAdress.text;
			userdata.data.password = _inputfPassword.text;
			userdata.data.autoLogin = _autoLogin.isOn;
			userdata.data.Write();
		}

		private void OnButtonMakeAccount()
		{
			_canvas.SetActive(true);
			_inputfMailAdress.interactable = false;
			_inputfPassword.interactable = false;
			onLogin?.Invoke();
		}

		public void SignUp()
		{
			Assert.IsNotNull(_inputfMailAdress.text, "Email is Missing !");
			Assert.IsNotNull(_inputfPassword.text, "Password is Missing");

			ObjectManager.Instance.Logs.text = "Adding Data ...";
			var docRef = db.Collection("Users").Document(_inputfMailAdress.text);

			var user = new User
			{
				Email = _inputfMailAdress.text,
				Password = _inputfPassword.text,
				MoneyBalance = 0,
				SignUpTimeStamp = FieldValue.ServerTimestamp
			};

			docRef.SetAsync(user).ContinueWithOnMainThread(task =>
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
					OnButtonMakeAccount();
				}
			});
		}

		public void OnButtonLogin()
		{
//TODO Firebase
			_canvas.SetActive(true);
			_inputfMailAdress.interactable = false;
			_inputfPassword.interactable = false;
			onLogin?.Invoke();
		}

		public void OnButtonLogout()
		{
//TODO Firebase
			_canvas.SetActive(false);
			_inputfMailAdress.interactable = true;
			_inputfPassword.interactable = true;
			onLogout?.Invoke();
		}
	}
}
