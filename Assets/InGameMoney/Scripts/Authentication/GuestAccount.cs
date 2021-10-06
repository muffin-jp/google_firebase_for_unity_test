using Firebase.Auth;
using UnityEngine;

namespace InGameMoney
{
    public class GuestAccount : MonoBehaviour
    {
        public void OnGuestLoginButton()
        {
            var guest = new Guest(FirebaseAuth.DefaultInstance);
#pragma warning disable 4014
            guest.PerformGuestLogin();
#pragma warning restore 4014
        }
    }
}
