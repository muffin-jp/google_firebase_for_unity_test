using System.Threading.Tasks;
using Firebase.Auth;

namespace InGameMoney
{
    public interface ITaskFault
    {
        bool Validate(Task<FirebaseUser> task);
    }
}
