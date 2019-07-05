using System.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NationalInstruments.Dfir;
using Rebar.Common;
using Rebar.Compiler.Nodes;

namespace Tests.Rebar.Unit.Execution
{
    [TestClass]
    public class FileExecutionTests : ExecutionTestBase
    {
        public TestContext TestContext { get; set; }

        [TestMethod]
        public void OpenFileHandleAndWriteString_Execute_FileCreatedWithCorrectContents()
        {
            DfirRoot function = DfirRoot.Create();
            FunctionalNode openFileHandle = new FunctionalNode(function.BlockDiagram, Signatures.OpenFileHandleType);
            string filePath = Path.Combine(TestContext.DeploymentDirectory, Path.GetRandomFileName());
            Constant pathConstant = ConnectConstantToInputTerminal(openFileHandle.InputTerminals[0], DataTypes.StringSliceType.CreateImmutableReference(), filePath, false);
            Frame frame = Frame.Create(function.BlockDiagram);
            UnwrapOptionTunnel unwrapOption = new UnwrapOptionTunnel(frame);
            Wire.Create(function.BlockDiagram, openFileHandle.OutputTerminals[1], unwrapOption.InputTerminals[0]);
            FunctionalNode writeStringToFileHandle = new FunctionalNode(frame.Diagram, Signatures.WriteStringToFileHandleType);
            Wire.Create(frame.Diagram, unwrapOption.OutputTerminals[0], writeStringToFileHandle.InputTerminals[0]);
            const string data = "data";
            Constant dataConstant = ConnectConstantToInputTerminal(writeStringToFileHandle.InputTerminals[1], DataTypes.StringSliceType.CreateImmutableReference(), data, false);

            TestExecutionInstance executionInstance = CompileAndExecuteFunction(function);

            AssertFileExistsAndContainsExpectedData(filePath, data);
        }

        private void AssertFileExistsAndContainsExpectedData(string filePath, string data)
        {
            Assert.IsTrue(File.Exists(filePath));
            string fileData;
            using (FileStream fileStream = File.Open(filePath, FileMode.Open, FileAccess.Read))
            {
                using (StreamReader streamReader = new StreamReader(fileStream))
                {
                    fileData = streamReader.ReadToEnd();
                }
            }
            Assert.AreEqual(data, fileData);
        }
    }
}
