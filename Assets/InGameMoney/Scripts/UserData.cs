using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.IO;
using UnityEngine;
using UnityEngine.Events;

namespace InGameMoney {

class UserData
{
	//singleton.
	public static UserData instance => _instance;
	static UserData _instance = new UserData();
	UserData(){}

	[System.Serializable]
	public class Data {
		public string mailAddress;
		public string password;
		public bool autoLogin;
#region json_template
		static string path => JsonTool.CombineStreamingAssetsPath("data.json");
		static bool isExists => System.IO.File.Exists(path);
		public static Data Read() { return isExists ? JsonTool.Read<Data>(path) : new Data(); }
		public void Write() { JsonTool.Write(path, this); }
#endregion
	}
	Data _data;
	public Data data => _data;
	
	public class PersonalData {
		public int purchasedMoney;
		public bool unlockedA;
		public bool unlockedB;
		public bool unlockedC;
#region json_template
		static string path => JsonTool.CombineStreamingAssetsPath("personal.json");
		static bool isExists => System.IO.File.Exists(path);
		public static PersonalData Read() { return isExists ? JsonTool.Read<PersonalData>(path) : new PersonalData(); }
		public void Write() { JsonTool.Write(path, this); }
#endregion
	}
	PersonalData _personalData;
	public int purchasedMoney => _personalData.purchasedMoney;
	public bool unlockedA => _personalData.unlockedA;
	public bool unlockedB => _personalData.unlockedB;
	public bool unlockedC => _personalData.unlockedC;

	public void Init()
	{
		if ( null != _data ) return;
		_data = Data.Read();
		
		AccountTest.onLogin += OnLogin;
		AccountTest.onLogout += OnLogout;
	}

	void OnLogin()
	{
		if ( null != _personalData ) return;
		_personalData = PersonalData.Read();

		GameObject.FindObjectOfType<PurchaseTest>().UpdateText();
		GameObject.FindObjectOfType<ShopTest>().UpdateText();
	}

	void OnLogout()
	{
		_personalData?.Write();
		_personalData = null;
	}

	public bool BuyMoney(int value)
	{
//TODO UnityIAP
		_personalData.purchasedMoney += value;
		_personalData.Write();
		return true;
	}

	public enum Item {
		UnlockA,
		UnlockB,
		UnlockC,
	}
	public bool PayMoney(int value, Item item)
	{
		if ( _personalData.purchasedMoney < value ) return false;
		_personalData.purchasedMoney -= value;

		switch ( item ) {
		case Item.UnlockA:
			{
				_personalData.unlockedA = true;
			}
			break;
		case Item.UnlockB:
			{
				_personalData.unlockedB = true;
			}
			break;
		case Item.UnlockC:
			{
				_personalData.unlockedC = true;
			}
			break;
		default:
			break;
		}

		_personalData.Write();
		return true;
	}
}

}
