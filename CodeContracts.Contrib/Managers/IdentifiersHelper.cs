namespace CodeContracts.Contrib.Managers
{
    internal class IdentifiersHelper
    {
        private const string CCGeneratedClassFileSuffix = ".contract";
        private const string CCGeneratedClassNameSuffix = "_Contract";

        public const string Attribute_Namespace = "System.Diagnostics.Contracts";
        public const string AttributeName_ContractClassFor = "ContractClassFor";
        public const string AttributeName_ContractClass = "ContractClass";

        public static string GetGeneratedClassFile(string interfaceName)
        {
            return interfaceName + CCGeneratedClassFileSuffix;
        }

        public static string GetGeneratedClassName(string interfaceName)
        {
            return interfaceName + CCGeneratedClassNameSuffix;
        }
    }
}
