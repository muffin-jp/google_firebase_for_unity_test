using UnityEngine;

namespace InGameMoney
{
    public class UserDataAccess : IWriteUserData
    {
        private readonly UserData userData;

        public UserDataAccess(UserData userData)
        {
            this.userData = userData;
        }

        public void WriteAccountData(string mailAddress, string password, bool autoLogin)
        {
            if (userData?.AccountData == null)
            {
                Debug.Log($">>>> Can not WriteAccountData because null");
                return;
            }
            userData.AccountData.mailAddress = mailAddress;
            userData.AccountData.password = password;
            userData.AccountData.autoLogin = autoLogin;
            userData.AccountData.Write();
        }

        public void WritePersonalData()
        {
            userData.PersonalData?.Write();
            userData.PersonalData = null;
        }

        public void ResetPersonalData()
        {
            userData.ResetPersonalData();
        }

        public void InitPersonalData()
        {
            userData.InitPersonalData();
        }

        public void UpdatePurchaseAndShop()
        {
            userData.UpdatePurchaseAndShop();
        }

        public void UpdateLocalData(User data)
        {
            userData.UpdateLocalData(data);
        }

        public void UpdateFirestoreUserDataAfterCredentialLinked(string email, string password)
        {
            userData.UpdateFirestoreUserDataAfterCredentialLinked(email, password);
        }
    }
}
