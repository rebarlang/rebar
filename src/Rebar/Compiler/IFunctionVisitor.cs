using NationalInstruments.MocCommon.SourceModel;

namespace Rebar.Compiler
{
    public interface IFunctionVisitor : IDataflowFunctionDefinitionVisitor
    {
        void VisitDropNode(SourceModel.DropNode node);
        void VisitFunction(SourceModel.Function function);
        void VisitFunctionalNode(SourceModel.FunctionalNode node);
        void VisitImmutableBorrowNode(SourceModel.ImmutableBorrowNode node);
        void VisitTerminateLifetimeNode(SourceModel.TerminateLifetime node);
    }
}
