using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace InGameMoney {

class ShopTest : MonoBehaviour
{
	[SerializeField] Text _textMoney;
	[SerializeField] Text _textA;
	[SerializeField] Text _textB;
	[SerializeField] Text _textC;

	public void UpdateText()
	{
		var data = UserData.instance;
	   _textMoney.text = "Purchased Gold : " + data.purchasedMoney.ToString();
	   _textA.text = "Unlocked A : " + data.unlockedA.ToString();
	   _textB.text = "Unlocked B : " + data.unlockedB.ToString();
	   _textC.text = "Unlocked C : " + data.unlockedC.ToString();
	}

	public void OnButtonUnlockA()
	{
		var userdata = UserData.instance;
		if ( userdata.unlockedA ) return;
		if ( userdata.PayMoney(100, UserData.Item.UnlockA) ) {
			UpdateText();
		}
	}

	public void OnButtonUnlockB()
	{
		var userdata = UserData.instance;
		if ( userdata.unlockedB ) return;
		if ( userdata.PayMoney(100, UserData.Item.UnlockB) ) {
			UpdateText();
		}
	}

	public void OnButtonUnlockC()
	{
		var userdata = UserData.instance;
		if ( userdata.unlockedC ) return;
		if ( userdata.PayMoney(100, UserData.Item.UnlockC) ) {
			UpdateText();
		}
	}
}

}
