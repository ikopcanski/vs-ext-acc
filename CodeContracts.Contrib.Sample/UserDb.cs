namespace CodeContracts.Contrib.Sample
{
    public class UserDb : IUserDb
    {
        public int SaveUser(UserDto user, bool isUpdate)
        {
            if (isUpdate)
            {
                //Update logic here

                user.DepartmentId = 777;

                return 1;
            }
            else
            {
                //Insert logic here

                user.Id = 56;

                return user.Id;
            }
        }
    }
}
