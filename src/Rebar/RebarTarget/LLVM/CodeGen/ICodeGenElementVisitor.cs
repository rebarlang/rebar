namespace Rebar.RebarTarget.LLVM.CodeGen
{
    /// <summary>
    /// Visitor interface for subclasses of <see cref="CodeGenElement"/>.
    /// </summary>
    /// <typeparam name="T">The return type of each visitor method.</typeparam>
    internal interface ICodeGenElementVisitor<T>
    {
        T VisitBuildStruct(BuildStruct buildStruct);
        T VisitCall(Call call);
        T VisitCallWithReturn(CallWithReturn callWithReturn);
        T VisitGetAddress(GetAddress getAddress);
        T VisitGetConstant(GetConstant getConstant);
        T VisitGetDereferencedValue(GetDereferencedValue getDereferencedValue);
        T VisitGetStructFieldPointer(GetStructFieldPointer getStructFieldPointer);
        T VisitGetStructFieldValue(GetStructFieldValue getStructFieldValue);
        T VisitGetValue(GetValue getValue);
        T VisitInitializeAsReference(InitializeAsReference initializeAsReference);
        T VisitInitializeValue(InitializeValue initializeValue);
        T VisitInitializeWithCopy(InitializeWithCopy initializeWithCopy);
        T VisitOp(Op op);
        T VisitShareValue(ShareValue shareValue);
        T VisitUpdateValue(UpdateValue updateValue);
        T VisitUpdateDereferencedValue(UpdateDereferencedValue updateDereferencedValue);
    }
}
