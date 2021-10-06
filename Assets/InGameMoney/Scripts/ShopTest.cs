using UnityEngine;
using UnityEngine.UI;

namespace InGameMoney {
	public class ShopTest : MonoBehaviour
{
	[SerializeField] Text _textMoney;
	[SerializeField] Text _textA;
	[SerializeField] Text _textB;
	[SerializeField] Text _textC;

	public void UpdateText()
	{
		var data = UserData.Instance;
	   _textMoney.text = "Purchased Gold : " + data.purchasedMoney;
	   _textA.text = "Unlocked A : " + data.unlockedA;
	   _textB.text = "Unlocked B : " + data.unlockedB;
	   _textC.text = "Unlocked C : " + data.unlockedC;
	}

	public void OnButtonUnlockA()
	{
		var userdata = UserData.Instance;
		if ( userdata.unlockedA ) return;
		userdata.TryUnlockItem(100, UserData.Item.UnlockA);
	}

	public void OnButtonUnlockB()
	{
		var userdata = UserData.Instance;
		if ( userdata.unlockedB ) return;
		userdata.TryUnlockItem(100, UserData.Item.UnlockB);
	}

	public void OnButtonUnlockC()
	{
		var userdata = UserData.Instance;
		if ( userdata.unlockedC ) return;
		userdata.TryUnlockItem(100, UserData.Item.UnlockC);
	}
}

}
