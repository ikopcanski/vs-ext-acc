namespace CodeContracts.Contrib.Sample
{
    public class UserManager : IUserManager
    {
        private IUserDb _userDb;

        public UserManager(IUserDb userDb)
        {
            _userDb = userDb;
        }

        public bool InsertUser(UserDto user)
        {
            return _userDb.SaveUser(user, false) > 0;
        }

        public bool UpdateUser(UserDto user)
        {
            return _userDb.SaveUser(user, true) > 0;
        }
    }
}
