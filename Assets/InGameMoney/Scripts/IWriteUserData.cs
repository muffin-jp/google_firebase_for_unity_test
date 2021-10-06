namespace InGameMoney
{
    public interface IWriteUserData
    {
        void WriteData(string mailAddress, string password, bool autoLogin);
    }
}
