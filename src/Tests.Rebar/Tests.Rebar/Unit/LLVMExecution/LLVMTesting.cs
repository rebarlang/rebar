using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LLVMSharp;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Tests.Rebar.Unit.LLVMExecution
{
    [TestClass]
    public class LLVMTesting
    {
        [TestMethod]
        public void LLVMModuleTest()
        {
            // interesting: keep in mind/look up LLVM.ContextCreate -- way of separating different module load contexts?
            var module = new Module("test");
            var functionType = LLVM.FunctionType(LLVM.VoidType(), new LLVMTypeRef[] { }, false);
            var topLevelFunction = module.AddFunction("f", functionType);
            LLVMBasicBlockRef entryBlock = topLevelFunction.AppendBasicBlock("entry");
            var builder = new IRBuilder();
            builder.PositionBuilderAtEnd(entryBlock);
            builder.CreateRetVoid();

            string moduleDump = module.PrintModuleToString();
        }
    }
}
