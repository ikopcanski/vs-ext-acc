namespace CodeContracts.Contrib.Sample
{
    public interface IUserManager
    {
        bool InsertUser(UserDto user);

        bool UpdateUser(UserDto user);
    }
}
