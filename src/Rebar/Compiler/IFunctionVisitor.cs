using NationalInstruments.MocCommon.SourceModel;

namespace Rebar.Compiler
{
    public interface IFunctionVisitor : IDataflowFunctionDefinitionVisitor
    {
        void VisitConstructor(SourceModel.Constructor node);
        void VisitDropNode(SourceModel.DropNode node);
        void VisitFunction(SourceModel.Function function);
        void VisitFunctionalNode(SourceModel.FunctionalNode node);
        void VisitImmutableBorrowNode(SourceModel.ImmutableBorrowNode node);
        void VisitStructFieldAccessor(SourceModel.StructFieldAccessor node);
        void VisitTerminateLifetimeNode(SourceModel.TerminateLifetime node);
    }
}
