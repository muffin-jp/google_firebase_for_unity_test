namespace InGameMoney
{
    public class UserDataAccess : IWriteUserData
    {
        private readonly UserData userData;

        public UserDataAccess(UserData userData)
        {
            this.userData = userData;
        }

        public void WriteData(string mailAddress, string password, bool autoLogin)
        {
            if (userData?.AccountData == null) return;
            userData.AccountData.mailAddress = mailAddress;
            userData.AccountData.password = password;
            userData.AccountData.autoLogin = autoLogin;
            userData.AccountData.Write();
        }
    }
}
