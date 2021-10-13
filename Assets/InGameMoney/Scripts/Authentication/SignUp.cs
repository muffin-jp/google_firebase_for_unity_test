using System.Threading.Tasks;
using Firebase.Auth;

namespace InGameMoney
{
    public class SignUp : ITaskFault
    {
        public bool Validate(Task<FirebaseUser> task)
        {
            if (task.IsFaulted)
            {
                if (AuthUtility.CheckError (task.Exception, (int)AuthError.EmailAlreadyInUse)) {
                    Print.RedLog($">>>> Email is already in use");
                } else if (AuthUtility.CheckError (task.Exception, (int)AuthError.AccountExistsWithDifferentCredentials)) {
                    Print.RedLog($">>>> Account Exists With Different Credentials");
                } else if (AuthUtility.CheckError (task.Exception, (int)AuthError.InvalidEmail)) {
                    Print.RedLog($">>>> Invalid Email");
                } else if (AuthUtility.CheckError (task.Exception, (int)AuthError.MissingPassword)) {
                    Print.RedLog($">>>> Password is Missing");
                } else if (AuthUtility.CheckError (task.Exception, (int)AuthError.MissingEmail)) {
                    Print.RedLog($">>>> Email is Missing");
                } else {
                    Print.RedLog($">>>> Something went wrong, Try again later.");
                }
                return true;
            }

            return false;
        }
    }
}
