using Firebase.Auth;
using Firebase.Extensions;

namespace InGameMoney
{
    public class AccountBase
    {
        protected async void DeleteUserAsync(FirebaseAuth auth)
        {
            if (auth.CurrentUser != null)
            {
                var deleteUser = auth.CurrentUser.DeleteAsync()
                    .ContinueWithOnMainThread(task => task);

                await deleteUser;
                
                if (deleteUser.Result.IsCanceled)
                {
                    Print.RedLog(">>>> DeleteAsync was canceled.");
                    return;
                }

                if (deleteUser.Result.IsFaulted)
                {
                    Print.RedLog(">>>> DeleteAsync encountered an error: " + deleteUser.Exception);
                    return;
                }
                Print.GreenLog($">>>> User deleted successfully");
            }
        }
    }
}
