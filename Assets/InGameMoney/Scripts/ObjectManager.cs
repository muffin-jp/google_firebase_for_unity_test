using UnityEngine;
using UnityEngine.UI;

namespace InGameMoney
{
    public class ObjectManager : MonoBehaviour
    {
        [SerializeField] private PurchaseTest purchase;
        [SerializeField] private Text logs;
        [SerializeField] private Text firstBootLogs;
        [SerializeField] private ShopTest shop;
        [SerializeField] private GameObject inGameMoney;
        [SerializeField] private GameObject firstBoot;
        
        public PurchaseTest Purchase => purchase;
        public Text Logs => logs;
        public GameObject InGameMoney => inGameMoney;
        public GameObject FirstBoot => firstBoot;
        public Text FirstBootLogs => firstBootLogs;
        
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
