using SimpleInjector;

namespace CodeContracts.Contrib.Sample
{
    class Program
    {
        private static Container _container;

        static void Main(string[] args)
        {
            ConfigureDI();

            var manager = _container.GetInstance<IUserManager>();

            var newUser = new UserDto()
            {
                Username = "username"
            };

            manager.InsertUser(newUser);

            var updatedUser = new UserDto()
            {
                Id = 1,
                Username = "username_new"
            };

            manager.UpdateUser(newUser);
        }

        private static void ConfigureDI()
        {
            _container.Register<IUserManager, UserManager>();
            _container.Register<IUserDb, UserDb>();
        }
    }
}
