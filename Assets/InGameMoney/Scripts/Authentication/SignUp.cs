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
                    ObjectManager.Instance.Logs.text = "Email is already in use";
                } else if (AuthUtility.CheckError (task.Exception, (int)AuthError.AccountExistsWithDifferentCredentials)) {
                    ObjectManager.Instance.Logs.text = "Account Exists With Different Credentials";
                } else if (AuthUtility.CheckError (task.Exception, (int)AuthError.InvalidEmail)) {
                    ObjectManager.Instance.Logs.text = "Invalid Email";
                } else if (AuthUtility.CheckError (task.Exception, (int)AuthError.MissingPassword)) {
                    ObjectManager.Instance.Logs.text = "Password is Missing";
                } else if (AuthUtility.CheckError (task.Exception, (int)AuthError.MissingEmail)) {
                    ObjectManager.Instance.Logs.text = "Email is Missing";
                } else {
                    ObjectManager.Instance.Logs.text = "Something went wrong, Try again later.";
                }
                return true;
            }

            return false;
        }
    }
}
