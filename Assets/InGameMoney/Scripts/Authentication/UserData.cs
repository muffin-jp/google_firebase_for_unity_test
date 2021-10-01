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
		
		AccountManager.OnLogin += OnLogin;
		AccountManager.OnLogout += OnLogout;
	}

	private void OnLogin()
	{
		if ( null != personalData ) return;
		InitPersonalData();
	}

	public void InitPersonalData()
	{
		personalData = PersonalData.Read();
	}

	public void UpdateLocalData(User data)
	{
		UpdateLocalAccountData(data);
		UpdatePurchaseAndShop();
	}

	private static void UpdateLocalAccountData(User data)
	{
		Print.GreenLog($">>>> UpdateLocalUserData HasKey FirebaseSignedWithAppleKey {PlayerPrefs.HasKey(AccountManager.FirebaseSignedWithAppleKey)}");
		var writeUserData = AccountManager.UserDataAccess;
		var mailAddress = data.Email;
		var password = data.Password;
		var autoLogin = AccountManager.Instance.AutoLogin;
		writeUserData.WriteAccountData(mailAddress, password, autoLogin);
	}

	public async void UpdatePurchaseAndShop()
	{
		Print.GreenLog($">>>> UpdatePurchaseAndShop");
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
		ResetAccountData();
	}

	public void BuyMoney(int value)
	{
		ObjectManager.Instance.Logs.text = $"Buying {value} Money ... ";
		var buyId = $"{accountData.mailAddress} Money-{value}-{System.DateTime.Now:HH:mm:ss:tt}";
		var docRef = AccountManager.Db.Collection("UserMoney")
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

	public void ResetPersonalData()
	{
		if (personalData == null) return;
		personalData.purchasedMoney = 0;
		personalData.unlockedA = false;
		personalData.unlockedB = false;
		personalData.unlockedC = false;
		personalData.Write();
		ObjectManager.Instance.MoneyBalanceText.text = $"{ObjectManager.PurchasedMoneyPrefix}{personalData.purchasedMoney}";
	}

	public void ResetAccountData()
	{
		if (accountData == null) return;
		accountData.mailAddress = null;
		accountData.password = null;
		accountData.autoLogin = false;
		accountData.Write();
	}

	private void UpdateUserMoneyBalance(int moneyBalance)
	{
		ObjectManager.Instance.Logs.text = $"Updating User Money Balance ...";
		var docRef = AccountManager.Db.Collection("Users").Document(accountData.mailAddress);
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
		var user = new User
		{
			Email = email,
			Password = password
		};
		UpdateWithPreviousData(user);
	}

	private async void UpdateWithPreviousData(User user)
	{
		await UpdateFirestoreUserData(user);
		Print.GreenLog($">>>> Finish UpdateWithPreviousData");
	}

	public async Task UpdateFirestoreUserData(User newUser)
	{
		Print.GreenLog($">>>> Updating User data, such as email and password, newUser email {newUser.Email}");
		var previousUserData = await GetUserData(newUser);
		var newUserData = new User
		{
			Email = newUser.Email,
			MoneyBalance = previousUserData?.MoneyBalance ?? 0,
			Password = newUser.Password,
			SignUpTimeStamp = FieldValue.ServerTimestamp
		};
		await AccountManager.Instance.SignUpToFirestoreProcedure(newUserData);
		UpdateLocalData(newUserData);
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
		Print.GreenLog($">>>> ReadUserData");
		var userData = await GetUserData();
		Assert.IsNotNull(userData, "User data should not null, Or possible local data is not null but the server is null");
		moneyBalance = userData.MoneyBalance;
	}

	private async Task<User> GetUserData(User newUser = null)
	{
		Print.GreenLog($">>>> GetUserData _data == null {accountData == null} mailAddress {accountData?.mailAddress}");
		var email = accountData?.mailAddress ?? newUser?.Email;
		
		if (email == null)
		{
			// No previous data, possibly using new device, so we need to provide new email
			Print.GreenLog($">>>> GetUserData No previous email so return new email or default");
			return default;
		}
		
		var usersRef = AccountManager.Db.Collection("Users").Document(email);
		var task = usersRef.GetSnapshotAsync().ContinueWithOnMainThread(readTask => readTask);

		await task;
		
		if (task.Result.IsCanceled) 
			Print.GreenLog(">>>> An Error Occurred !");
		
		if (task.Result.IsFaulted) 
			Print.GreenLog(">>>> Add Data Failed Failed !");
		
		var snapshot = task.Result.Result;
		if (snapshot.Exists)
		{
			Print.GreenLog($">>>> Document exist! for {snapshot.Id}");
			var user = snapshot.ConvertTo<User>();
			return user;
		}

		Print.GreenLog($">>>> Document does not exist! {snapshot.Id}");
		return default;
	}

	private void PurchaseItem(int value, Item item)
	{
		ObjectManager.Instance.Logs.text = $"Purchase {item}";
		var docId = $"{accountData.mailAddress} Item-{item}-{System.DateTime.Now:HH:mm:ss:tt}";;
		var docRef = AccountManager.Db.Collection("UserPurchasedItems").Document(docId);
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
