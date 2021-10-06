using System.Threading.Tasks;
using Firebase.Auth;

namespace InGameMoney
{
    public class Login : ITaskFault
    {
        public bool Validate(Task<FirebaseUser> task)
        {
            if (task.IsFaulted) {

                if (AuthUtility.CheckError (task.Exception, (int)AuthError.WrongPassword)) {
                    Print.RedLog($">>>> Password is Wrong");
                } else if (AuthUtility.CheckError (task.Exception, (int)AuthError.AccountExistsWithDifferentCredentials)) {
                    Print.RedLog($">>>> Account Exists With Different Credentials");
                } else if (AuthUtility.CheckError (task.Exception, (int)AuthError.InvalidEmail)) {
                    Print.RedLog($">>>> Invalid Email");
                } else if (AuthUtility.CheckError (task.Exception, (int)AuthError.MissingPassword)) {
                    Print.RedLog($">>>> Password is Missing");
                } else if (AuthUtility.CheckError (task.Exception, (int)AuthError.MissingEmail)) {
                    Print.RedLog($">>>> Email is Missing");
                } else if (AuthUtility.CheckError (task.Exception, (int)AuthError.UserNotFound)) {
                    Print.RedLog($">>>> User Not Found");
                } else if (AuthUtility.CheckError (task.Exception, (int)AuthError.TooManyRequests)) {
                    Print.RedLog($">>>> Too Many Requests, Please wait for 60 Seconds");
                } else {
                    Print.RedLog($">>>> Something went wrong, Try again later.");
                }

                return true;
            }

            return false;
        }
    }
}
