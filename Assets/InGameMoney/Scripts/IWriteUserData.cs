namespace InGameMoney
{
    public interface IWriteUserData
    {
        void WriteAccountData(string mailAddress, string password, bool autoLogin);
        void WritePersonalData();
    }
}
