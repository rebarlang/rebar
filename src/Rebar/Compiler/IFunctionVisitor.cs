using NationalInstruments.MocCommon.SourceModel;

namespace Rebar.Compiler
{
    public interface IFunctionVisitor : IDataflowFunctionDefinitionVisitor
    {
        void VisitFunction(SourceModel.Function function);

        void VisitFunctionalNode(SourceModel.FunctionalNode node);
        void VisitDropNode(SourceModel.DropNode node);

        void VisitTerminateLifetimeNode(SourceModel.TerminateLifetime node);

        void VisitSelectReferenceNode(SourceModel.SelectReferenceNode node);
        void VisitAssignNode(SourceModel.AssignNode node);
        void VisitExchangeValuesNode(SourceModel.ExchangeValues node);

        void VisitImmutableBorrowNode(SourceModel.ImmutableBorrowNode node);

        void VisitCreateCellNode(SourceModel.CreateCell node);

        void VisitSomeConstructorNode(SourceModel.SomeConstructorNode node);
    }
}
