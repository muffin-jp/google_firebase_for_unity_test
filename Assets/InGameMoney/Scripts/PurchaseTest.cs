using UnityEngine;
using UnityEngine.UI;

namespace InGameMoney {
	public class PurchaseTest : MonoBehaviour
{
	[SerializeField] Text _text;

	public void UpdateText()
	{
	   _text.text = "Purchased Gold : " + UserData.Instance.purchasedMoney;
	}

	public void OnButtonMoney100()
	{
		// Todo call Unity IAP mStoreController.InitiatePurchase(goldProductId) first 
		// move below code to PurchaseProcessingResult (Unity IAP)
		UserData.Instance.BuyMoney(100);
	}

	public void OnButtonMoney600()
	{
		UserData.Instance.BuyMoney(600);
	}
}

}
