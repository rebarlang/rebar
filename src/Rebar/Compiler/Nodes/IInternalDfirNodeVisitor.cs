namespace Rebar.Compiler.Nodes
{
    internal interface IInternalDfirNodeVisitor<T>
    {
        T VisitAwaitNode(AwaitNode awaitNode);
        T VisitCreateMethodCallPromise(CreateMethodCallPromise createMethodCallPromise);
        T VisitDecomposeStructNode(DecomposeStructNode decomposeStructNode);
        T VisitPanicOrContinueNode(PanicOrContinueNode panicOrContinueNode);
    }
}
