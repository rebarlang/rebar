using NationalInstruments.MocCommon.SourceModel;

namespace RustyWires.Compiler
{
    public interface IRustyWiresFunctionVisitor : IDataflowFunctionDefinitionVisitor
    {
        void VisitRustyWiresFunction(SourceModel.RustyWiresFunction function);

        void VisitDropNode(SourceModel.DropNode node);
        void VisitImmutablePassthroughNode(SourceModel.ImmutablePassthroughNode node);
        void VisitMutablePassthroughNode(SourceModel.MutablePassthroughNode node);

        void VisitTerminateLifetimeNode(SourceModel.TerminateLifetime node);

        void VisitSelectReferenceNode(SourceModel.SelectReferenceNode node);
        void VisitCreateCopyNode(SourceModel.CreateCopyNode node);
        void VisitAssignNode(SourceModel.AssignNode node);
        void VisitExchangeValuesNode(SourceModel.ExchangeValues node);

        void VisitImmutableBorrowNode(SourceModel.ImmutableBorrowNode node);

        void VisitCreateCellNode(SourceModel.CreateCell node);

        void VisitSomeConstructorNode(SourceModel.SomeConstructorNode node);

        void VisitPureUnaryPrimitive(SourceModel.PureUnaryPrimitive node);
        void VisitPureBinaryPrimitive(SourceModel.PureBinaryPrimitive node);
        void VisitMutatingUnaryPrimitive(SourceModel.MutatingUnaryPrimitive node);
        void VisitMutatingBinaryPrimitive(SourceModel.MutatingBinaryPrimitive node);

        void VisitRange(SourceModel.Range node);
    }
}
