using LLVMSharp;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Rebar.RebarTarget.LLVM;

namespace Tests.Rebar.Unit.LLVMExecution
{
    [TestClass]
    public class LLVMTesting
    {
        [TestMethod]
        public void LLVMModuleTest()
        {
            using (var contextWrapper = new ContextWrapper())
            {
                var module = contextWrapper.CreateModule("test");
                var functionType = LLVM.FunctionType(contextWrapper.VoidType, new LLVMTypeRef[] { }, false);
                var topLevelFunction = module.AddFunction("f", functionType);
                LLVMBasicBlockRef entryBlock = topLevelFunction.AppendBasicBlock("entry");
                var builder = contextWrapper.CreateIRBuilder();
                builder.PositionBuilderAtEnd(entryBlock);
                builder.CreateRetVoid();

                string moduleDump = module.PrintModuleToString();
            }
        }
    }
}
