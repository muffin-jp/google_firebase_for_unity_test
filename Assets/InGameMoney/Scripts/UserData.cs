using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Linq;
using Firebase.Extensions;
using Firebase.Firestore;
using UnityEngine;
using UnityEngine.Events;

namespace InGameMoney {

class UserData
{
	//singleton.
	public static UserData Instance => _instance ?? (_instance = new UserData());

	static UserData _instance;
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

		ObjectManager.Instance.Purchase.UpdateText();
		ObjectManager.Instance.Shop.UpdateText();
	}

	void OnLogout()
	{
		_personalData?.Write();
		_personalData = null;
	}

	public void BuyMoney(int value)
	{
		ObjectManager.Instance.Logs.text = $"Buying {value} Money ... ";
		var buyId = $"{_data.mailAddress} Money-{value}-{System.DateTime.Now:HH:mm:ss:tt}";
		var docRef = AccountTest.Db.Collection("UserMoney")
			.Document($"{buyId}");
		var userMoney = new UserMoney
		{
			Email = _data.mailAddress,
			PurchasedMoney = value,
			PurchasedTimeStamp = FieldValue.ServerTimestamp
		};
		docRef.SetAsync(userMoney).ContinueWithOnMainThread(task =>
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

			if (!task.IsCompleted) return;
			ObjectManager.Instance.Logs.text = $"Succeed Buy {value} Money Completed! ";
			CompletePurchaseMoney(value);
			ObjectManager.Instance.Purchase.UpdateText();
		});
	}

	private void CompletePurchaseMoney(int value)
	{
		_personalData.purchasedMoney += value;
		_personalData.Write();
		UpdateUserMoneyBalance(_personalData.purchasedMoney);
	}

	private void UpdateUserMoneyBalance(int moneyBalance)
	{
		ObjectManager.Instance.Logs.text = $"Updating User Money Balance ...";
		var docRef = AccountTest.Db.Collection("Users").Document(_data.mailAddress);
		var updates = new Dictionary<string, object>
		{
			{"MoneyBalance", moneyBalance}
		};

		docRef.UpdateAsync(updates).ContinueWithOnMainThread(task =>
		{
			if (task.IsCanceled)
			{
				ObjectManager.Instance.Logs.text = "An Error Occurred !";
				return;
			}

			if (task.IsFaulted)
			{
				ObjectManager.Instance.Logs.text = "Update Data Failed Failed !";
				return;
			}

			if (task.IsCompleted)
			{
				ObjectManager.Instance.Logs.text = $"Succeed updating money balance | balance : {moneyBalance}";
			}
		});
	}

	public enum Item {
		UnlockA,
		UnlockB,
		UnlockC,
	}

	private bool AbleToPayMoney(int value, Item item)
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
		}

		_personalData.Write();
		return true;
	}

	public async void TryUnlockItem(int value, Item item)
	{
		var usersRef = AccountTest.Db.Collection("Users");
		long moneyBalance = 0;
		await usersRef.GetSnapshotAsync().ContinueWithOnMainThread(task =>
		{
			if (task.IsCanceled) { ObjectManager.Instance.Logs.text = "An Error Occurred !"; return; }
			if (task.IsFaulted) { ObjectManager.Instance.Logs.text = "Add Data Failed Failed !"; return; }
			var snapshot = task.Result;
			
			if (snapshot.Documents == null || !snapshot.Documents.Any())
			{
				ObjectManager.Instance.Logs.text = $"Data is null or empty";
				return;
			}

			var document = snapshot.Documents.FirstOrDefault(x =>
			{
				var dict = x.ToDictionary();
				return dict.ContainsValue(_data.mailAddress);
			});
			
			var documentDict = document?.ToDictionary();
			if (documentDict != null)
				moneyBalance = (long) documentDict["MoneyBalance"];
			else Debug.LogError($"MoneyBalance is null {_data.mailAddress}");
		});

		ObjectManager.Instance.Logs.text = $"MoneyBalance: {moneyBalance}";
		_personalData.purchasedMoney = (int) moneyBalance;
		if (AbleToPayMoney(value, item))
		{
			PurchaseItem(value, item);
			UpdateUserMoneyBalance(_personalData.purchasedMoney);
			ObjectManager.Instance.Shop.UpdateText();
		}
		else ObjectManager.Instance.Logs.text = $"Not Enough money to buy {item}";
	}

	private void PurchaseItem(int value, Item item)
	{
		ObjectManager.Instance.Logs.text = $"Purchase {item}";
		var docId = $"{_data.mailAddress} Item-{item}-{System.DateTime.Now:HH:mm:ss:tt}";;
		var docRef = AccountTest.Db.Collection("UserPurchasedItems").Document(docId);
		var purchasedItem = new UserPurchasedItems
		{
			Email = _data.mailAddress,
			PurchasedItem = item.ToString(),
			Price = value,
			PurchasedTimeStamp = FieldValue.ServerTimestamp
		};
		docRef.SetAsync(purchasedItem).ContinueWithOnMainThread(task =>
		{
			if (task.IsCanceled)
			{
				ObjectManager.Instance.Logs.text = "An Error Occurred !";
				return;
			}

			if (task.IsFaulted)
			{
				ObjectManager.Instance.Logs.text = "Purchase Data Failed !";
				return;
			}

			if (task.IsCompleted)
			{
				ObjectManager.Instance.Logs.text = $"Succeed purchasing item {item}";
			}
		});
	}
}

}
