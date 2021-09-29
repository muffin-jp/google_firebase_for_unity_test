using Firebase.Firestore;

namespace InGameMoney
{
    [FirestoreData]
    public class User
    {
        [FirestoreProperty] 
        public string Email { get; set; }

        [FirestoreProperty] 
        public string Password { get; set; }

        [FirestoreProperty] 
        public long MoneyBalance { get; set; }

        [FirestoreProperty] 
        public object SignUpTimeStamp { get; set; }
    }

    [FirestoreData]
    public class UserMoney
    {
        [FirestoreProperty] 
        public string Email { get; set; }
        
        [FirestoreProperty] 
        public int PurchasedMoney { get; set; }
        
        [FirestoreProperty]
        public object PurchasedTimeStamp { get; set; }
    }

    [FirestoreData]
    public class UserPurchasedItems
    {
        [FirestoreProperty] 
        public string Email { get; set; }
        
        [FirestoreProperty] 
        public string PurchasedItem { get; set; }

        [FirestoreProperty] 
        public long Price { get; set; }

        [FirestoreProperty]
        public object PurchasedTimeStamp { get; set; }
    }
}
