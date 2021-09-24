using System.Collections.Generic;
using System.Threading.Tasks;
using Firebase.Extensions;
using Firebase.Firestore;
using UnityEngine;
using UnityEngine.Assertions;
#pragma warning disable 4014

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

namespace InGameMoney {
	public class UserData
{
	//singleton.
	public static UserData Instance => instance ?? (instance = new UserData());

	private static UserData instance;
	private UserData(){}

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
	Data accountData;
	public Data AccountData => accountData;
	
	

	private PersonalData personalData;
	public PersonalData PersonalData
	{
		get => personalData;
		set => personalData = value;
	}

	private long moneyBalance;
	
	public int purchasedMoney => personalData.purchasedMoney;
	public bool unlockedA => personalData.unlockedA;
	public bool unlockedB => personalData.unlockedB;
	public bool unlockedC => personalData.unlockedC;

	public void Init()
	{
		if ( null != accountData ) return;
		accountData = Data.Read();
		
		AccountTest.OnLogin += OnLogin;
		AccountTest.OnLogout += OnLogout;
	}

	private void OnLogin()
	{
		if ( null != personalData ) return;
		personalData = PersonalData.Read();
	}

	public void UpdateLocalData()
	{
		UpdateLocalUserData();
		UpdatePurchaseAndShop();
	}

	private static void UpdateLocalUserData()
	{
		Debug.Log($">>>> UpdateLocalUserData HasKey FirebaseSignedWithAppleKey {PlayerPrefs.HasKey(AccountTest.FirebaseSignedWithAppleKey)}");
		IWriteUserData writeUserData = AccountTest.UserDataAccess;
		var mailAddress = AccountTest.Instance.InputFieldMailAddress.text;
		var password = AccountTest.Instance.InputFieldPassword.text;
		var autoLogin = AccountTest.Instance.AutoLogin;
		writeUserData.WriteAccountData(mailAddress, password, autoLogin);
	}

	private async void UpdatePurchaseAndShop()
	{
		await ReadUserData();

		personalData.purchasedMoney = (int) moneyBalance;
		personalData.Write();
		ObjectManager.Instance.Purchase.UpdateText();
		ObjectManager.Instance.Shop.UpdateText();
	}

	private void OnLogout()
	{
		ResetPersonalData();
		personalData = null;
		// TODO Reset AccountData
	}

	public void BuyMoney(int value)
	{
		ObjectManager.Instance.Logs.text = $"Buying {value} Money ... ";
		var buyId = $"{accountData.mailAddress} Money-{value}-{System.DateTime.Now:HH:mm:ss:tt}";
		var docRef = AccountTest.Db.Collection("UserMoney")
			.Document($"{buyId}");
		var userMoney = new UserMoney
		{
			Email = accountData.mailAddress,
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
		personalData.purchasedMoney += value;
		personalData.Write();
		UpdateUserMoneyBalance(personalData.purchasedMoney);
	}

	private void ResetPersonalData()
	{
		personalData.purchasedMoney = 0;
		personalData.unlockedA = false;
		personalData.unlockedB = false;
		personalData.unlockedC = false;
		personalData.Write();
		ObjectManager.Instance.MoneyBalanceText.text = $"{ObjectManager.PurchasedMoneyPrefix}{personalData.purchasedMoney}";
	}

	private void UpdateUserMoneyBalance(int moneyBalance)
	{
		ObjectManager.Instance.Logs.text = $"Updating User Money Balance ...";
		var docRef = AccountTest.Db.Collection("Users").Document(accountData.mailAddress);
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
				ObjectManager.Instance.Logs.text = "UpdateUserMoneyBalance Data Failed !";
				return;
			}

			if (task.IsCompleted)
			{
				ObjectManager.Instance.Logs.text = $"Succeed updating money balance | balance : {moneyBalance}";
			}
		});
	}

	public void UpdateFirestoreUserDataAfterCredentialLinked(string email, string password)
	{
		UpdateFirestoreUserData(email, password);
	}

	private async Task UpdateFirestoreUserData(string email, string password)
	{
		ObjectManager.Instance.Logs.text = $"Updating User data, such as email and password, etc";
		var oldUserData = await GetUserData();
		var newUserData = new User
		{
			Email = email,
			MoneyBalance = oldUserData.MoneyBalance,
			Password = password,
			SignUpTimeStamp = FieldValue.ServerTimestamp
		};
		await AccountTest.Instance.SignUpToFirestoreProcedure(newUserData);
		UpdateLocalData();
	}

	public enum Item {
		UnlockA,
		UnlockB,
		UnlockC,
	}

	private bool AbleToPayMoney(int value, Item item)
	{
		if ( personalData.purchasedMoney < value ) return false;
		personalData.purchasedMoney -= value;

		switch ( item ) {
		case Item.UnlockA:
			{
				personalData.unlockedA = true;
			}
			break;
		case Item.UnlockB:
			{
				personalData.unlockedB = true;
			}
			break;
		case Item.UnlockC:
			{
				personalData.unlockedC = true;
			}
			break;
		}

		personalData.Write();
		return true;
	}

	public async void TryUnlockItem(int value, Item item)
	{
		await ReadUserData();

		ObjectManager.Instance.Logs.text = $"MoneyBalance: {moneyBalance}";
		personalData.purchasedMoney = (int) moneyBalance;
		if (AbleToPayMoney(value, item))
		{
			PurchaseItem(value, item);
			UpdateUserMoneyBalance(personalData.purchasedMoney);
			ObjectManager.Instance.Shop.UpdateText();
		}
		else ObjectManager.Instance.Logs.text = $"Not Enough money to buy {item}";
	}

	// Sync user data from firestore to prevent cheat
	private async Task ReadUserData()
	{
		var userData = await GetUserData();
		Assert.IsNotNull(userData, "User data should not null");
		moneyBalance = userData.MoneyBalance;
	}

	private async Task<User> GetUserData()
	{
		Debug.Log($">>>> GetUserData _data == null {accountData == null} mailAddress {accountData.mailAddress}");
		var usersRef = AccountTest.Db.Collection("Users").Document(accountData.mailAddress);
		var task = usersRef.GetSnapshotAsync().ContinueWithOnMainThread(readTask => readTask);

		await task;
		
		if (task.Result.IsCanceled) 
			ObjectManager.Instance.Logs.text = "An Error Occurred !";
		
		if (task.Result.IsFaulted) 
			ObjectManager.Instance.Logs.text = "Add Data Failed Failed !";
		
		var snapshot = task.Result.Result;
		if (snapshot.Exists)
		{
			ObjectManager.Instance.Logs.text = $"Document exist! for {snapshot.Id}";
			var user = snapshot.ConvertTo<User>();
			return user;
		}

		ObjectManager.Instance.Logs.text = $"Document does not exist! {snapshot.Id}";
		return default;
	}

	private void PurchaseItem(int value, Item item)
	{
		ObjectManager.Instance.Logs.text = $"Purchase {item}";
		var docId = $"{accountData.mailAddress} Item-{item}-{System.DateTime.Now:HH:mm:ss:tt}";;
		var docRef = AccountTest.Db.Collection("UserPurchasedItems").Document(docId);
		var purchasedItem = new UserPurchasedItems
		{
			Email = accountData.mailAddress,
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
