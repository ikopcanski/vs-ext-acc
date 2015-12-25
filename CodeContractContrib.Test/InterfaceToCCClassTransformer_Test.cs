using CodeContractsContrib.Managers;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.IO;
using System.Text;

namespace CodeContractsContrib.Test
{
    [TestClass]
    public class InterfaceToCCClassTransformer_Test
    {
        [TestMethod]
        public void GetCodeContractClass_Test()
        {
            //Arrange

            string sourceCode = null;
            using (var streamReader = new StreamReader("IInterfaceInput.txt", Encoding.UTF8))
            {
                sourceCode = streamReader.ReadToEnd();
            }
            var syntaxTree = CSharpSyntaxTree.ParseText(sourceCode);
            var rootNode = syntaxTree.GetRoot();

            //Act

            var creator = new InterfaceCCTransformer();
            var actual = creator.GetCodeContractClass(rootNode);

            //Assert

            var expected = File.ReadAllText("IInterfaceInput.Contract.txt");
            Assert.AreEqual(expected, actual);

        }
    }
}
