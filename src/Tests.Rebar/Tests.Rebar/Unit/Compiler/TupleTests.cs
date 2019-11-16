using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NationalInstruments.DataTypes;
using NationalInstruments.Dfir;
using Rebar.Common;
using Rebar.Compiler;
using Rebar.Compiler.Nodes;

namespace Tests.Rebar.Unit.Compiler
{
    [TestClass]
    public class TupleTests : CompilerTestBase
    {
        [TestMethod]
        public void BuildTupleNode_SetVariableTypes_OutputIsClusterType()
        {
            DfirRoot function = DfirRoot.Create();
            BuildTupleNode buildTuple = new BuildTupleNode(function.BlockDiagram, 2);
            ConnectConstantToInputTerminal(buildTuple.InputTerminals[0], PFTypes.Int32, false);
            ConnectConstantToInputTerminal(buildTuple.InputTerminals[1], PFTypes.Boolean, false);

            RunSemanticAnalysisUpToSetVariableTypes(function);

            VariableReference builtTupleOutputVariable = buildTuple.OutputTerminals[0].GetTrueVariable();
            NIType tupleType = builtTupleOutputVariable.Type;
            Assert.IsTrue(tupleType.IsCluster());
            Assert.IsTrue(tupleType.GetFields().ElementAt(0).GetDataType().IsInt32());
            Assert.IsTrue(tupleType.GetFields().ElementAt(1).GetDataType().IsBoolean());
        }
    }
}
