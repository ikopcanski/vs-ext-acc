using System.Diagnostics.Contracts;

namespace CodeContracts.Contrib.Sample
{
    [ContractClass(typeof (IUserDb_Contract))]
    public interface IUserDb
    {
        int SaveUser(UserDto user, bool isUpdate);
    }
}