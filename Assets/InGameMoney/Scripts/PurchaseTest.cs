using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace InGameMoney {

class PurchaseTest : MonoBehaviour
{
	[SerializeField] Text _text;

	public void UpdateText()
	{
	   _text.text = "Purchased Gold : " + UserData.instance.purchasedMoney.ToString();
	}

	public void OnButtonMoney100()
	{
		UserData.instance.BuyMoney(100);
		UpdateText();
	}

	public void OnButtonMoney600()
	{
		UserData.instance.BuyMoney(600);
		UpdateText();
	}
}

}
