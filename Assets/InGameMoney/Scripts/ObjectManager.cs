using UnityEngine;
using UnityEngine.UI;

namespace InGameMoney
{
    public class ObjectManager : MonoBehaviour
    {
        [SerializeField] private PurchaseTest purchase;
        public PurchaseTest Purchase => purchase;
        
        [SerializeField] private Text logs;
        public Text Logs => logs;

        [SerializeField] private ShopTest shop;
        public ShopTest Shop => shop;
        public static ObjectManager Instance { get; private set; }

        private void Awake()
        {
            if (Instance) Destroy(this);
            else Instance = this;
            DontDestroyOnLoad(this);
        }
    }
}
