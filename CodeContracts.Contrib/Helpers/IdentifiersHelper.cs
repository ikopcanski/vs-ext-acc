namespace CodeContracts.Contrib.Helpers
{
    internal class IdentifiersHelper
    {
        private const string CodeContractClassFileSuffix = ".contract";
        private const string CodeContractClassNameSuffix = "_Contract";

        public const string Attribute_Namespace = "System.Diagnostics.Contracts";
        public const string AttributeName_ContractClassFor = "ContractClassFor";
        public const string AttributeName_ContractClass = "ContractClass";

        public static string GetGeneratedCodeContractClassFile(string interfaceName)
        {
            return interfaceName + CodeContractClassFileSuffix;
        }

        public static string GetCodeContractClassName(string interfaceName)
        {
            return interfaceName + CodeContractClassNameSuffix;
        }
    }
}
