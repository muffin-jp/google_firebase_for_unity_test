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
            if (userData?.data == null) return;
            userData.data.mailAddress = mailAddress;
            userData.data.password = password;
            userData.data.autoLogin = autoLogin;
            userData.data.Write();
        }
    }
}
