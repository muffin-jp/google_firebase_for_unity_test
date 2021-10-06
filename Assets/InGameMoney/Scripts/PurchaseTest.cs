using UnityEngine;
using UnityEngine.Purchasing;
using UnityEngine.UI;

namespace InGameMoney {
	public class PurchaseTest : MonoBehaviour, IStoreListener
	{
		[SerializeField] Text _text;
		
		// Your products IDs. They should match the ids of your products in your store.
		[SerializeField] private string money100ProductId;
		[SerializeField] private string money600ProductId;

		// The Unity Purchasing system.
		private IStoreController mStoreController; 
		
		private void Start()
		{
			InitializePurchasing();
		}

		/// <summary>
		/// Initialize all available products
		/// </summary>
		private void InitializePurchasing()
		{
			var builder = ConfigurationBuilder.Instance(StandardPurchasingModule.Instance());

			// Add products that will be purchasable and indicate its type.
			builder.AddProduct(money100ProductId, ProductType.Consumable);
			builder.AddProduct(money600ProductId, ProductType.Consumable);

			UnityPurchasing.Initialize(this, builder);
		}

		public void UpdateText()
		{
			_text.text = "Purchased Gold : " + UserData.Instance.purchasedMoney;
		}
		
		/// <summary>
		/// Handle variation of money to buy, the amount is set from Unity Inspector
		/// </summary>
		/// <param name="moneyAmount"></param>
		public void OnBuyMoneyButton(int moneyAmount)
		{
			switch (moneyAmount)
			{
				case 100:
					ObjectManager.Instance.Logs.text = $"Buy 100 money {money100ProductId}";
					mStoreController.InitiatePurchase(money100ProductId);
					break;
				case 600:
					ObjectManager.Instance.Logs.text = $"Buy 600 money {money600ProductId}";
					mStoreController.InitiatePurchase(money600ProductId);
					break;
			}
		}

		/// <summary>
		/// Informs Unity Purchasing as to whether an Application
		/// has finished processing a purchase.
		/// </summary>
		public PurchaseProcessingResult ProcessPurchase(PurchaseEventArgs args)
		{
			// Retrieve the purchased product
			var product = args.purchasedProduct;

			// Add the purchased product to the players inventory
			if (product.definition.id == money100ProductId)
			{
				UserData.Instance.BuyMoney(100);
			}
			else if (product.definition.id == money600ProductId)
			{
				UserData.Instance.BuyMoney(600);
			}

			ObjectManager.Instance.Logs.text = $"Purchase Complete - Product: {product.definition.id}";

			// We return Complete, informing IAP that the processing on our side is done
			// and the transaction can be closed.
			return PurchaseProcessingResult.Complete;
		}

		/// <summary>
		/// The various reasons a purchase can fail.
		/// </summary>
		public void OnPurchaseFailed(Product product, PurchaseFailureReason failureReason)
		{
			ObjectManager.Instance.Logs.text =
				$"Purchase failed - Product: '{product.definition.id}', PurchaseFailureReason: {failureReason}";
		}

		/// <summary>
		/// Used by Applications to control Unity Purchasing.
		/// </summary>
		public void OnInitialized(IStoreController controller, IExtensionProvider extensions)
		{
#if false
			foreach (var product in controller.products.all)
			{
				ObjectManager.Instance.Logs.text =
					$"In-App Purchasing successfully initialized definition {product.definition.id}";
			}
#endif

			mStoreController = controller;
		}
		
		/// <summary>
		/// Reasons for which purchasing initialization could fail.
		/// </summary>
		public void OnInitializeFailed(InitializationFailureReason error)
		{
			ObjectManager.Instance.Logs.text = $"In-App Purchasing initialize failed: {error}";
		}
	}
}
