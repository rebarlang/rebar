using NationalInstruments.Design;
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

            AddSupportedModel<DropNode>(n => new BasicNodeViewModel(n, "Drop Value", @"Resources\Diagram\Nodes\Drop.png"));
            AddSupportedModel<ImmutablePassthroughNode>(n => new BasicNodeViewModel(n, "Immutable Passthrough"));
            AddSupportedModel<MutablePassthroughNode>(n => new BasicNodeViewModel(n, "Mutable Passthrough"));
            AddSupportedModel<TerminateLifetime>(n => new BasicNodeViewModel(n, "Terminate Lifetime", @"Resources\Diagram\Nodes\TerminateLifetime.png"));
            AddSupportedModel<SelectReferenceNode>(n => new BasicNodeViewModel(n, "Select Reference", @"Resources\Diagram\Nodes\SelectReference.png"));
            AddSupportedModel<CreateCopyNode>(n => new BasicNodeViewModel(n, "Create Copy", @"Resources\Diagram\Nodes\CreateCopy.png"));
            AddSupportedModel<AssignNode>(n => new BasicNodeViewModel(n, "Assign", @"Resources\Diagram\Nodes\Assign.png"));
            AddSupportedModel<ExchangeValues>(n => new BasicNodeViewModel(n, "Exchange Values", @"Resources\Diagram\Nodes\ExchangeValues.png"));
            AddSupportedModel<CreateCell>(n => new BasicNodeViewModel(n, "Create Cell"));
            AddSupportedModel<ImmutableBorrowNode>(n => new BasicNodeViewModel(n, "Immutable Borrow", @"Resources\Diagram\Nodes\ImmutableBorrowNode.png"));
            AddSupportedModel<SomeConstructorNode>(n => new BasicNodeViewModel(n, "Some", @"Resources\Diagram\Nodes\Some.png"));
            AddSupportedModel<Range>(n => new BasicNodeViewModel(n, "Range", @"Resources\Diagram\Nodes\Range.png"));
            AddSupportedModel<Output>(n => new BasicNodeViewModel(n, "Output", @"Resources\Diagram\Nodes\Output.png"));

            AddSupportedModel<SourceModel.Add>(n => new BasicNodeViewModel(n, "Add", @"Resources\Diagram\Nodes\Add.png"));
            AddSupportedModel<SourceModel.Subtract>(n => new BasicNodeViewModel(n, "Subtract", @"Resources\Diagram\Nodes\Subtract.png"));
            AddSupportedModel<SourceModel.Multiply>(n => new BasicNodeViewModel(n, "Multiply", @"Resources\Diagram\Nodes\Multiply.png"));
            AddSupportedModel<SourceModel.Divide>(n => new BasicNodeViewModel(n, "Divide", @"Resources\Diagram\Nodes\Divide.png"));
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

            AddSupportedModel<VectorCreate>(n => new BasicNodeViewModel(n, "Create Vector"));

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
        }
    }
}
