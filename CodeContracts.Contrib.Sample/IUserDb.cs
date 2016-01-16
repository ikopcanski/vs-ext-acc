using System.Diagnostics.Contracts;

namespace CodeContracts.Contrib.Sample
{
    
    public interface IUserDb
    {
        int SaveUser(UserDto user, bool isUpdate);
    }
}