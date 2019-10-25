using NationalInstruments.Design;
using NationalInstruments.MocCommon.Design;
using NationalInstruments.MocCommon.SourceModel;
using NationalInstruments.Shell;
using NationalInstruments.SourceModel;
using NationalInstruments.VI.Design;
using NationalInstruments.VI.SourceModel;
using Rebar.SourceModel;

namespace Rebar.Design
{
    [ExportProvideViewModels(typeof(FunctionDiagramEditor))]
    public class FunctionViewModelProvider : ViewModelProvider
    {
        public FunctionViewModelProvider()
        {
            AddSupportedModel<DiagramLabel>(n => new DiagramLabelViewModel(n));
            AddSupportedModel<Wire>(w => new FunctionWireViewModel(w));
            AddSupportedModel<MocCommonMethodCall>(n => new MethodCallViewModel(n));
            AddSupportedModel<DataAccessor>(n => new DataAccessorEditor(n));

            AddSupportedModel<DropNode>(n => new BasicNodeViewModel(n, "Drop Value", @"Resources\Diagram\Nodes\Drop.png"));
            AddSupportedModel<ImmutablePassthroughNode>(n => new BasicNodeViewModel(n, "Immutable Passthrough"));
            AddSupportedModel<MutablePassthroughNode>(n => new BasicNodeViewModel(n, "Mutable Passthrough"));
            AddSupportedModel<TerminateLifetime>(n => new BasicNodeViewModel(n, "Terminate Lifetime", @"Resources\Diagram\Nodes\TerminateLifetime.png"));
            AddSupportedModel<SelectReferenceNode>(n => new BasicNodeViewModel(n, "Select Reference", @"Resources\Diagram\Nodes\SelectReference.png"));
            AddSupportedModel<CreateCopyNode>(n => new BasicNodeViewModel(n, "Create Copy", @"Resources\Diagram\Nodes\CreateCopy.png"));
            AddSupportedModel<AssignNode>(n => new BasicNodeViewModel(n, "Assign", @"Resources\Diagram\Nodes\Assign.png"));
            AddSupportedModel<ExchangeValues>(n => new BasicNodeViewModel(n, "Exchange Values", @"Resources\Diagram\Nodes\ExchangeValues.png"));
            AddSupportedModel<CreateLockingCell>(n => new BasicNodeViewModel(n, "Create Locking Cell"));
            AddSupportedModel<CreateNonLockingCell>(n => new BasicNodeViewModel(n, "Create Non-Locking Cell"));
            AddSupportedModel<ImmutableBorrowNode>(n => new BasicNodeViewModel(n, "Immutable Borrow", @"Resources\Diagram\Nodes\ImmutableBorrowNode.png"));
            AddSupportedModel<SomeConstructorNode>(n => new BasicNodeViewModel(n, "Some", @"Resources\Diagram\Nodes\Some.png"));
            AddSupportedModel<NoneConstructorNode>(n => new BasicNodeViewModel(n, "None", @"Resources\Diagram\Nodes\None.png"));
            AddSupportedModel<Range>(n => new BasicNodeViewModel(n, "Range", @"Resources\Diagram\Nodes\Range.png"));
            AddSupportedModel<Output>(n => new BasicNodeViewModel(n, "Output", @"Resources\Diagram\Nodes\Output.png"));

            AddSupportedModel<SourceModel.Add>(n => new BasicNodeViewModel(n, "Add", @"Resources\Diagram\Nodes\Add.png"));
            AddSupportedModel<SourceModel.Subtract>(n => new BasicNodeViewModel(n, "Subtract", @"Resources\Diagram\Nodes\Subtract.png"));
            AddSupportedModel<SourceModel.Multiply>(n => new BasicNodeViewModel(n, "Multiply", @"Resources\Diagram\Nodes\Multiply.png"));
            AddSupportedModel<SourceModel.Divide>(n => new BasicNodeViewModel(n, "Divide", @"Resources\Diagram\Nodes\Divide.png"));
            AddSupportedModel<Modulus>(n => new BasicNodeViewModel(n, "Modulus", @"Resources\Diagram\Nodes\Modulus.png"));
            AddSupportedModel<And>(n => new BasicNodeViewModel(n, "And", @"Resources\Diagram\Nodes\And.png"));
            AddSupportedModel<Or>(n => new BasicNodeViewModel(n, "Or", @"Resources\Diagram\Nodes\Or.png"));
            AddSupportedModel<Xor>(n => new BasicNodeViewModel(n, "Xor", @"Resources\Diagram\Nodes\Xor.png"));
            AddSupportedModel<SourceModel.Increment>(n => new BasicNodeViewModel(n, "Increment", @"Resources\Diagram\Nodes\Increment.png"));
            AddSupportedModel<SourceModel.Not>(n => new BasicNodeViewModel(n, "Not", @"Resources\Diagram\Nodes\Not.png"));
            AddSupportedModel<AccumulateAdd>(n => new BasicNodeViewModel(n, "Accumulate Add", @"Resources\Diagram\Nodes\AccumulateAdd.png"));
            AddSupportedModel<AccumulateSubtract>(n => new BasicNodeViewModel(n, "Accumulate Subtract", @"Resources\Diagram\Nodes\AccumulateSubtract.png"));
            AddSupportedModel<AccumulateMultiply>(n => new BasicNodeViewModel(n, "Accumulate Multiply", @"Resources\Diagram\Nodes\AccumulateMultiply.png"));
            AddSupportedModel<AccumulateDivide>(n => new BasicNodeViewModel(n, "Accumulate Divide", @"Resources\Diagram\Nodes\AccumulateDivide.png"));
            AddSupportedModel<AccumulateAnd>(n => new BasicNodeViewModel(n, "Accumulate And", @"Resources\Diagram\Nodes\AccumulateAnd.png"));
            AddSupportedModel<AccumulateOr>(n => new BasicNodeViewModel(n, "Accumulate Or", @"Resources\Diagram\Nodes\AccumulateOr.png"));
            AddSupportedModel<AccumulateXor>(n => new BasicNodeViewModel(n, "Accumulate Xor", @"Resources\Diagram\Nodes\AccumulateXor.png"));
            AddSupportedModel<AccumulateIncrement>(n => new BasicNodeViewModel(n, "Accumulate Increment", @"Resources\Diagram\Nodes\Increment.png"));
            AddSupportedModel<AccumulateNot>(n => new BasicNodeViewModel(n, "Accumulate Not", @"Resources\Diagram\Nodes\AccumulateNot.png"));
            AddSupportedModel<Equal>(n => new BasicNodeViewModel(n, "Equal", @"Resources\Diagram\Nodes\Equal.png"));
            AddSupportedModel<NotEqual>(n => new BasicNodeViewModel(n, "Not Equal", @"Resources\Diagram\Nodes\NotEqual.png"));
            AddSupportedModel<LessThan>(n => new BasicNodeViewModel(n, "Less Than", @"Resources\Diagram\Nodes\LessThan.png"));
            AddSupportedModel<LessEqual>(n => new BasicNodeViewModel(n, "Less Than Or Equal", @"Resources\Diagram\Nodes\LessEqual.png"));
            AddSupportedModel<GreaterThan>(n => new BasicNodeViewModel(n, "Greater Than", @"Resources\Diagram\Nodes\GreaterThan.png"));
            AddSupportedModel<GreaterEqual>(n => new BasicNodeViewModel(n, "Greater Than Or Equal", @"Resources\Diagram\Nodes\GreaterEqual.png"));

            AddSupportedModel<StringFromSlice>(n => new BasicNodeViewModel(n, "String From Slice"));
            AddSupportedModel<StringToSlice>(n => new BasicNodeViewModel(n, "String To Slice"));
            AddSupportedModel<StringConcat>(n => new BasicNodeViewModel(n, "Concatenate Strings"));
            AddSupportedModel<StringAppend>(n => new BasicNodeViewModel(n, "Append To String"));

            AddSupportedModel<VectorCreate>(n => new BasicNodeViewModel(n, "Create Vector"));
            AddSupportedModel<VectorInsert>(n => new BasicNodeViewModel(n, "Insert Into Vector"));

            AddSupportedModel<OpenFileHandle>(n => new BasicNodeViewModel(n, "Open File Handle"));
            AddSupportedModel<ReadLineFromFileHandle>(n => new BasicNodeViewModel(n, "Read Line From File Handle"));
            AddSupportedModel<WriteStringToFileHandle>(n => new BasicNodeViewModel(n, "Write To File Handle"));

            AddSupportedModel((SourceModel.FlatSequence s) => new FlatSequenceEditor(s));
            AddSupportedModel<FlatSequenceDiagram>(d => new FlatSequenceDiagramViewModel(d));
            AddSupportedModel<BorrowTunnel>(t => new BorrowTunnelViewModel(t));
            AddSupportedModel<LockTunnel>(t => new FlatSequenceSimpleBorderNodeViewModel(t, @"Resources\Diagram\Nodes\Lock.png"));
            AddSupportedModel<FlatSequenceTerminateLifetimeTunnel>(t => new FlatSequenceSimpleBorderNodeViewModel(t, @"Resources\Diagram\Nodes\TerminateLifetime.png"));
            AddSupportedModel<FlatSequenceSimpleTunnel>(t => new FlatSequenceTunnelViewModel(t));
            AddSupportedModel<UnwrapOptionTunnel>(t => new FlatSequenceSimpleBorderNodeViewModel(t, @"Resources\Diagram\Nodes\UnwrapOption.png"));
            AddSupportedModel<SourceModel.Loop>(l => new LoopViewModel(l));
            AddSupportedModel<SourceModel.LoopTunnel>(t => new BorderNodeViewModel(t));
            AddSupportedModel<LoopBorrowTunnel>(t => new LoopBorrowTunnelViewModel(t));
            AddSupportedModel<LoopConditionTunnel>(t => new LoopBorderNodeViewModel(t, @"Resources\Diagram\Nodes\LoopCondition.png"));
            AddSupportedModel<LoopIterateTunnel>(t => new LoopBorderNodeViewModel(t, @"Resources\Diagram\Nodes\Iterate.png"));
            AddSupportedModel<LoopTerminateLifetimeTunnel>(t => new LoopBorderNodeViewModel(t, @"Resources\Diagram\Nodes\TerminateLifetime.png"));

            AddSupportedModel<OptionPatternStructure>(s => new OptionPatternStructureEditor(s));
            AddSupportedModel<OptionPatternStructureDiagram>(d => new OptionPatternStructureDiagramViewModel(d));
            AddSupportedModel<OptionPatternStructureSelector>(t => new BorderNodeViewModel(t));
            AddSupportedModel<OptionPatternStructureTunnel>(t => new BorderNodeViewModel(t));
        }
    }
}
