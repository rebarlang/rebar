using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NationalInstruments.Compiler;
using NationalInstruments.DataTypes;
using NationalInstruments.Dfir;
using Rebar.Common;
using Rebar.Compiler;
using Rebar.Compiler.Nodes;
using Rebar.RebarTarget;
using Rebar.RebarTarget.LLVM;
using Tests.Rebar.Unit.Execution;

namespace Tests.Rebar.Unit.Compiler
{
    [TestClass]
    public class AllocatorTests : ExecutionTestBase
    {
        [TestMethod]
        public void IntegerConstantIntoInspect_Allocate_IntegerVariableGetsConstantValueSource()
        {
            DfirRoot function = DfirRoot.Create();
            FunctionalNode inspect = new FunctionalNode(function.BlockDiagram, Signatures.InspectType);
            Constant constant = ConnectConstantToInputTerminal(inspect.InputTerminals[0], PFTypes.Int32, 5, false);

            Dictionary<VariableReference, ValueSource> valueSources = RunAllocator(function);

            ValueSource integerValueSource = valueSources[constant.OutputTerminal.GetTrueVariable()];
            Assert.IsInstanceOfType(integerValueSource, typeof(ConstantValueSource));
            ValueSource inspectInputValueSource = valueSources[inspect.InputTerminals[0].GetTrueVariable()];
            Assert.IsInstanceOfType(inspectInputValueSource, typeof(ReferenceToConstantValueSource));
        }

        private Dictionary<VariableReference, ValueSource> RunAllocator(DfirRoot function)
        {
            var cancellationToken = new CompileCancellationToken();
            RunCompilationUpToAutomaticNodeInsertion(function, cancellationToken);
            Dictionary<VariableReference, ValueSource> valueSources = VariableReference.CreateDictionaryWithUniqueVariableKeys<ValueSource>();
            var additionalSources = new Dictionary<object, ValueSource>();
            var allocator = new Allocator(valueSources, additionalSources);
            allocator.Execute(function, cancellationToken);
            return valueSources;
        }
    }
}
