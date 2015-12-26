namespace CodeContracts.Contrib.Helpers
{
    internal class IdentifiersHelper
    {
        private const string CodeContractClassFileSuffix = ".contract";
        private const string CodeContractClassNameSuffix = "_Contract";

        private const string ContractProxyClassFileSuffix = ".proxy";
        private const string ContractProxyClassNameSuffix = "_Proxy";

        public const string Attribute_Namespace = "System.Diagnostics.Contracts";
        public const string AttributeName_ContractClassFor = "ContractClassFor";
        public const string AttributeName_ContractClass = "ContractClass";

        public static string GetCodeContractClassFile(string interfaceName)
        {
            return interfaceName + CodeContractClassFileSuffix;
        }

        public static string GetCodeContractClassName(string interfaceName)
        {
            return interfaceName + CodeContractClassNameSuffix;
        }

        public static string GetContractProxyClassFile(string className)
        {
            return className.Replace(CodeContractClassFileSuffix, ContractProxyClassFileSuffix);
        }

        public static string GetContractProxyClassName(string className)
        {
            return className.Replace(CodeContractClassNameSuffix, ContractProxyClassNameSuffix);
        }
    }
}
