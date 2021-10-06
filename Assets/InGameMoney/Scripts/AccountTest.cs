using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;

namespace InGameMoney {

class AccountTest : MonoBehaviour
{
	[SerializeField] InputField _inputfMailAdress;
	[SerializeField] InputField _inputfPassword;
	[SerializeField] GameObject _canvas;
	[SerializeField] Toggle _autoLogin;
	
	public static UnityAction onLogin = null;
	public static UnityAction onLogout = null;

	void Start()
	{
		var userdata = UserData.instance;
		userdata.Init();
		
		_inputfMailAdress.text = userdata.data.mailAddress;
		_inputfPassword.text = userdata.data.password;
		_canvas.SetActive(false);

		_autoLogin.isOn = userdata.data.autoLogin;
		if ( string.IsNullOrEmpty(userdata.data.mailAddress) 
			|| string.IsNullOrEmpty(userdata.data.password)
		) {
			_autoLogin.isOn = false;
		}
		if ( _autoLogin.isOn ) {
			OnButtonLogin();
		}
	}

	void OnApplicationQuit()
	{
		onLogout?.Invoke();

		var userdata = UserData.instance;
		userdata.data.mailAddress = _inputfMailAdress.text;
		userdata.data.password = _inputfPassword.text;
		userdata.data.autoLogin = _autoLogin.isOn;
		userdata.data.Write();
	}

	public void OnButtonMakeAccount()
	{
//TODO Firebase
		_canvas.SetActive(true);
		_inputfMailAdress.interactable = false;
		_inputfPassword.interactable = false;
		onLogin?.Invoke();
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
