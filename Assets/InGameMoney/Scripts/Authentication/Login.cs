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
                    ObjectManager.Instance.Logs.text = "Password is Wrong";
                } else if (AuthUtility.CheckError (task.Exception, (int)AuthError.AccountExistsWithDifferentCredentials)) {
                    ObjectManager.Instance.Logs.text = "Account Exists With Different Credentials";
                } else if (AuthUtility.CheckError (task.Exception, (int)AuthError.InvalidEmail)) {
                    ObjectManager.Instance.Logs.text = "Invalid Email";
                } else if (AuthUtility.CheckError (task.Exception, (int)AuthError.MissingPassword)) {
                    ObjectManager.Instance.Logs.text = "Password is Missing";
                } else if (AuthUtility.CheckError (task.Exception, (int)AuthError.MissingEmail)) {
                    ObjectManager.Instance.Logs.text = "Email is Missing";
                } else if (AuthUtility.CheckError (task.Exception, (int)AuthError.UserNotFound)) {
                    ObjectManager.Instance.Logs.text = "User Not Found";
                } else if (AuthUtility.CheckError (task.Exception, (int)AuthError.TooManyRequests)) {
                    ObjectManager.Instance.Logs.text = "Too Many Requests, Please wait for 60 Seconds";
                } else {
                    ObjectManager.Instance.Logs.text = "Something went wrong, Try again later.";
                }

                return true;
            }

            return false;
        }
    }
}
