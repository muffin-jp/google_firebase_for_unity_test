using Firebase.Firestore;

namespace FirestoreTest
{
    [FirestoreData]
    public class User
    {
        [FirestoreProperty]
        public string Name { get; set; }
        
        [FirestoreProperty]
        public string Email { get; set; }
        
        [FirestoreProperty]
        public string Bio { get; set; }
        
        [FirestoreProperty]
        public object TimeStamp { get; set; }
    }
}
