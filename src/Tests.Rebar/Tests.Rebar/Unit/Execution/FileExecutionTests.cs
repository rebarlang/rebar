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
            Constant pathConstant = ConnectStringConstantToInputTerminal(openFileHandle.InputTerminals[0], filePath);
            Frame frame = Frame.Create(function.BlockDiagram);
            UnwrapOptionTunnel unwrapOption = new UnwrapOptionTunnel(frame);
            Wire.Create(function.BlockDiagram, openFileHandle.OutputTerminals[1], unwrapOption.InputTerminals[0]);
            FunctionalNode writeStringToFileHandle = new FunctionalNode(frame.Diagram, Signatures.WriteStringToFileHandleType);
            Wire.Create(frame.Diagram, unwrapOption.OutputTerminals[0], writeStringToFileHandle.InputTerminals[0]);
            const string data = "data";
            Constant dataConstant = ConnectStringConstantToInputTerminal(writeStringToFileHandle.InputTerminals[1], data);

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

        [TestMethod]
        public void OpenFileHandleAndReadLine_Execute_LineReadFromFile()
        {
            string filePath = Path.Combine(TestContext.DeploymentDirectory, Path.GetRandomFileName());
            const string data = "data";
            CreateFileAndWriteData(filePath, data + "\r\n");
            DfirRoot function = DfirRoot.Create();
            FunctionalNode openFileHandle = new FunctionalNode(function.BlockDiagram, Signatures.OpenFileHandleType);
            Constant pathConstant = ConnectStringConstantToInputTerminal(openFileHandle.InputTerminals[0], filePath);
            Frame frame = Frame.Create(function.BlockDiagram);
            UnwrapOptionTunnel unwrapOption = new UnwrapOptionTunnel(frame);
            Wire.Create(function.BlockDiagram, openFileHandle.OutputTerminals[1], unwrapOption.InputTerminals[0]);
            FunctionalNode readLineFromFileHandle = new FunctionalNode(frame.Diagram, Signatures.ReadLineFromFileHandleType);
            Wire.Create(frame.Diagram, unwrapOption.OutputTerminals[0], readLineFromFileHandle.InputTerminals[0]);
            Frame innerFrame = Frame.Create(frame.Diagram);
            UnwrapOptionTunnel innerUnwrapOption = new UnwrapOptionTunnel(innerFrame);
            Wire.Create(frame.Diagram, readLineFromFileHandle.OutputTerminals[1], innerUnwrapOption.InputTerminals[0]);
            FunctionalNode output = new FunctionalNode(innerFrame.Diagram, Signatures.OutputType);
            Wire.Create(innerFrame.Diagram, innerUnwrapOption.OutputTerminals[0], output.InputTerminals[0]);

            TestExecutionInstance executionInstance = CompileAndExecuteFunction(function);

            Assert.AreEqual(data, executionInstance.RuntimeServices.LastOutputValue);
        }

        private void CreateFileAndWriteData(string filePath, string data)
        {
            using (FileStream fileStream = File.Open(filePath, FileMode.Create, FileAccess.Write))
            {
                using (StreamWriter streamWriter = new StreamWriter(fileStream))
                {
                    streamWriter.Write(data);
                }
            }
        }
    }
}
