using UnityEngine;

namespace InGameMoney
{
    public class UserDataAccess : IWriteUserData
    {
        private readonly UserData userData;

        public UserData UserData => userData;

        public UserDataAccess()
        {
            userData = UserData.Instance;
            Init();
        }

        private void Init()
        {
            userData.Init();
        }

        public void WriteAccountData(string mailAddress, string password, bool autoLogin)
        {
            Print.GreenLog($">>>> WriteAccountData");
            if (userData?.AccountData == null)
            {
                Print.GreenLog($">>>> Can not WriteAccountData because null");
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

        public void UpdateFirestoreUserData(string email, string password, bool login = true)
        {
            userData.UpdateFirestoreUserData(email, password, login);
        }
    }
}
