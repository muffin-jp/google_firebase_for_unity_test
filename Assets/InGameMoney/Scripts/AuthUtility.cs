using System;

namespace InGameMoney
{
    public static class AuthUtility
    {
        public static bool CheckError (AggregateException exception, int firebaseExceptionCode)
        {
            Firebase.FirebaseException fbEx = null;
            foreach (var e in exception.Flatten().InnerExceptions)
            {
                fbEx = e as Firebase.FirebaseException;
                if (fbEx != null)
                    break;
            }

            if (fbEx != null)
            {
                return fbEx.ErrorCode == firebaseExceptionCode;
            }
            return false;
        }
    }
}
