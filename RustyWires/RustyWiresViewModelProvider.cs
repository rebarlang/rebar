using NationalInstruments.Design;
using NationalInstruments.Shell;
using NationalInstruments.SourceModel;
using NationalInstruments.VI.Design;
using NationalInstruments.VI.SourceModel;
using RustyWires.Design;
using RustyWires.SourceModel;

namespace RustyWires
{
    [ExportProvideViewModels(typeof(RustyWiresDiagramEditor))]
    public class RustyWiresViewModelProvider : ViewModelProvider
    {
        public RustyWiresViewModelProvider()
        {
            AddSupportedModel<DiagramLabel>(n => new DiagramLabelViewModel(n));

            AddSupportedModel<DropNode>(n => new BasicNodeViewModel(n, "Drop Value"));
            AddSupportedModel<ImmutablePassthroughNode>(n => new BasicNodeViewModel(n, "Immutable Passthrough"));
            AddSupportedModel<MutablePassthroughNode>(n => new BasicNodeViewModel(n, "Mutable Passthrough"));
            AddSupportedModel<TerminateLifetime>(n => new BasicNodeViewModel(n, "Terminate Lifetime"));
            AddSupportedModel<SelectReferenceNode>(n => new BasicNodeViewModel(n, "Select Reference"));
            AddSupportedModel<CreateCopyNode>(n => new BasicNodeViewModel(n, "Create Copy"));
            AddSupportedModel<AssignNode>(n => new BasicNodeViewModel(n, "Assign"));
            AddSupportedModel<ExchangeValues>(n => new BasicNodeViewModel(n, "Exchange Values"));
            AddSupportedModel<CreateCell>(n => new BasicNodeViewModel(n, "Create Cell"));
            AddSupportedModel<ImmutableBorrowNode>(n => new BasicNodeViewModel(n, "Immutable Borrow"));
            AddSupportedModel<SomeConstructorNode>(n => new BasicNodeViewModel(n, "Some"));

            AddSupportedModel<SourceModel.Add>(n => new BasicNodeViewModel(n, "Add"));
            AddSupportedModel<SourceModel.Subtract>(n => new BasicNodeViewModel(n, "Subtract"));
            AddSupportedModel<SourceModel.Multiply>(n => new BasicNodeViewModel(n, "Multiply"));
            AddSupportedModel<SourceModel.Divide>(n => new BasicNodeViewModel(n, "Divide"));
            AddSupportedModel<And>(n => new BasicNodeViewModel(n, "And"));
            AddSupportedModel<Or>(n => new BasicNodeViewModel(n, "Or"));
            AddSupportedModel<Xor>(n => new BasicNodeViewModel(n, "Xor"));
            AddSupportedModel<SourceModel.Increment>(n => new BasicNodeViewModel(n, "Increment"));
            AddSupportedModel<SourceModel.Not>(n => new BasicNodeViewModel(n, "Not"));
            AddSupportedModel<AccumulateAdd>(n => new BasicNodeViewModel(n, "Accumulate Add"));
            AddSupportedModel<AccumulateSubtract>(n => new BasicNodeViewModel(n, "Accumulate Subtract"));
            AddSupportedModel<AccumulateMultiply>(n => new BasicNodeViewModel(n, "Accumulate Multiply"));
            AddSupportedModel<AccumulateDivide>(n => new BasicNodeViewModel(n, "Accumulate Divide"));
            AddSupportedModel<AccumulateAnd>(n => new BasicNodeViewModel(n, "Accumulate And"));
            AddSupportedModel<AccumulateOr>(n => new BasicNodeViewModel(n, "Accumulate Or"));
            AddSupportedModel<AccumulateXor>(n => new BasicNodeViewModel(n, "Accumulate Xor"));
            AddSupportedModel<AccumulateIncrement>(n => new BasicNodeViewModel(n, "Accumulate Increment"));
            AddSupportedModel<AccumulateNot>(n => new BasicNodeViewModel(n, "Accumulate Not"));

            AddSupportedModel<BorrowTunnel>(t => new BorrowTunnelViewModel(t));
            AddSupportedModel<FlatSequenceTerminateLifetimeTunnel>(t => new TerminateScopeTunnelViewModel(t));
            AddSupportedModel<RustyWiresFlatSequence>(s => new RustyWiresFlatSequenceEditor(s));
            AddSupportedModel<FlatSequenceDiagram>(d => new FlatSequenceDiagramViewModel(d));
            AddSupportedModel<RustyWiresFlatSequenceSimpleTunnel>(t => new FlatSequenceTunnelViewModel(t));
            AddSupportedModel<UnwrapOptionTunnel>(t => new FlatSequenceTunnelViewModel(t));
            AddSupportedModel<SourceModel.Loop>(l => new LoopViewModel(l));
            AddSupportedModel<SourceModel.LoopTunnel>(t => new Design.LoopTunnelViewModel(t));
            AddSupportedModel<LoopBorrowTunnel>(t => new Design.LoopBorrowTunnelViewModel(t));
            AddSupportedModel<LoopConditionTunnel>(t => new Design.LoopTunnelViewModel(t));
            AddSupportedModel<LoopTerminateLifetimeTunnel>(t => new Design.LoopTunnelViewModel(t));
        }

        /// <inheritdoc />
        public override void OnAnyViewModelCreated(Element model, IElementViewModel viewModel)
        {
            base.OnAnyViewModelCreated(model, viewModel);
            if (model is Wire)
            {
                viewModel.AttachService(new WireMutabilityViewModelService());
            }
        }
    }
}
