using System.Threading.Tasks;
using Firebase.Auth;
using Firebase.Extensions;

namespace InGameMoney
{
    public class AccountBase
    {
        protected async Task DeleteUserAsync(FirebaseAuth auth)
        {
            if (auth.CurrentUser != null)
            {
                var deleteUser = auth.CurrentUser.DeleteAsync()
                    .ContinueWithOnMainThread(task => task);

                await deleteUser;
                
                if (deleteUser.Result.IsCanceled)
                {
                    Print.RedLog(">>>> DeleteAsync was canceled.");
                }

                if (deleteUser.Result.IsFaulted)
                {
                    Print.RedLog(">>>> DeleteAsync encountered an error: " + deleteUser.Exception);
                }
                Print.GreenLog($">>>> User deleted successfully");
            }
        }
    }
}
