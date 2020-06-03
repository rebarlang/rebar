using System;
using System.Linq;
using LLVMSharp;
using NationalInstruments.DataTypes;
using Rebar.Common;
using Rebar.RebarTarget.LLVM.CodeGen;

namespace Rebar.RebarTarget.LLVM
{
    internal partial class FunctionCompiler : ICodeGenElementVisitor<bool>, ICodeGenValueStorage
    {
        internal static void BuildCreateYieldPromiseFunction(FunctionModuleContext moduleContext, NIType signature, LLVMValueRef createYieldPromiseFunction)
        {
            LLVMTypeRef valueType = moduleContext.LLVMContext.AsLLVMType(signature.GetGenericParameters().First()),
                valueReferenceType = LLVMTypeRef.PointerType(valueType, 0u),
                yieldPromiseType = moduleContext.LLVMContext.CreateLLVMYieldPromiseType(valueReferenceType);

            LLVMBasicBlockRef entryBlock = createYieldPromiseFunction.AppendBasicBlock("entry");
            var builder = moduleContext.LLVMContext.CreateIRBuilder();

            builder.PositionBuilderAtEnd(entryBlock);
            LLVMValueRef value = createYieldPromiseFunction.GetParam(0u),
                yieldPromise = builder.BuildStructValue(yieldPromiseType, new[] { value });
            builder.CreateStore(yieldPromise, createYieldPromiseFunction.GetParam(1u));
            builder.CreateRetVoid();
        }

        internal static void BuildYieldPromisePollFunction(FunctionModuleContext moduleContext, NIType signature, LLVMValueRef yieldPromisePollFunction)
        {
            LLVMTypeRef valueType = moduleContext.LLVMContext.AsLLVMType(signature.GetGenericParameters().ElementAt(1)),
                valueOptionType = moduleContext.LLVMContext.CreateLLVMOptionType(valueType);

            LLVMBasicBlockRef entryBlock = yieldPromisePollFunction.AppendBasicBlock("entry");
            var builder = moduleContext.LLVMContext.CreateIRBuilder();

            builder.PositionBuilderAtEnd(entryBlock);
            LLVMValueRef yieldPromisePtr = yieldPromisePollFunction.GetParam(0u),
                valuePtr = builder.CreateStructGEP(yieldPromisePtr, 0u, "valuePtr"),
                value = builder.CreateLoad(valuePtr, "value"),
                someValue = moduleContext.LLVMContext.BuildOptionValue(builder, valueOptionType, value);
            builder.CreateStore(someValue, yieldPromisePollFunction.GetParam(2u));
            builder.CreateRetVoid();
        }

        private readonly FunctionCompilerSharedData _sharedData;
        private readonly FunctionModuleBuilder _moduleBuilder;
        private readonly LLVMValueRef[] _codeGenValues;

        public FunctionCompiler(
            FunctionModuleBuilder moduleBuilder,
            FunctionCompilerSharedData sharedData,
            int codeGenValueIndices)
        {
            _moduleBuilder = moduleBuilder;
            _sharedData = sharedData;
            ModuleContext = new FunctionModuleContext(_sharedData.Context, _sharedData.Module, _sharedData.FunctionImporter);
            _codeGenValues = new LLVMValueRef[codeGenValueIndices];
        }

        private ContextWrapper Context => _sharedData.Context;

        private Module Module => _sharedData.Module;

        private FunctionCompilerState CurrentState => _sharedData.CurrentState;

        private LLVMValueRef CurrentFunction => CurrentState.Function;

        private IRBuilder Builder => CurrentState.Builder;

        private FunctionAllocationSet AllocationSet => _sharedData.AllocationSet;

        private FunctionModuleContext ModuleContext { get; }

        #region Private helpers

        private ValueSource GetValueSource(VariableReference variable)
        {
            return _sharedData.VariableStorage.GetValueSourceForVariable(variable);
        }

        private void InitializeIfNecessary(ValueSource toInitialize, Func<IRBuilder, LLVMValueRef> valueGetter)
        {
            var initializable = toInitialize as IInitializableValueSource;
            if (initializable != null)
            {
                initializable.InitializeValue(Builder, valueGetter(Builder));
            }
        }

        private void Update(ValueSource toUpdate, LLVMValueRef value)
        {
            var updateable = toUpdate as IUpdateableValueSource;
            if (updateable == null)
            {
                throw new ArgumentException("Trying to update non-updateable variable", nameof(toUpdate));
            }
            updateable.UpdateValue(Builder, value);
        }

        internal LLVMValueRef GetAddress(ValueSource valueSource, IRBuilder builder)
        {
            var addressable = valueSource as IAddressableValueSource;
            if (addressable == null)
            {
                throw new ArgumentException("Trying to get address of non-addressable variable", nameof(valueSource));
            }
            return addressable.GetAddress(builder);
        }

        #endregion

        #region Await and Panic

        internal static void GenerateWakerFromCurrentGroup(IRBuilder builder, ICodeGenValueStorage valueStorage, int outputIndex)
        {
            var functionCompiler = (FunctionCompiler)valueStorage;
            LLVMValueRef bitCastCurrentGroupFunction = builder.CreateBitCast(
                    functionCompiler._moduleBuilder.CurrentGroupData.Function,
                    LLVMTypeRef.PointerType(functionCompiler.Context.ScheduledTaskFunctionType(), 0u),
                    "bitCastCurrentGroupFunction"),
                bitCastStatePtr = builder.CreateBitCast(
                    functionCompiler.AllocationSet.StatePointer,
                    functionCompiler.Context.VoidPointerType(),
                    "bitCastStatePtr"),
                waker = builder.BuildStructValue(
                    functionCompiler.Context.AsLLVMType(DataTypes.WakerType),
                    new LLVMValueRef[] { bitCastCurrentGroupFunction, bitCastStatePtr },
                    "waker");
            valueStorage[outputIndex] = waker;
        }

        internal static void GeneratePromisePollAndBranch(IRBuilder builder, ICodeGenValueStorage valueStorage, int pollResultIsSomeIndex)
        {
            var functionCompiler = (FunctionCompiler)valueStorage;
            LLVMBasicBlockRef promiseNotDoneBlock = functionCompiler.CurrentFunction.AppendBasicBlock("promiseNotDone"),
                promiseDoneBlock = functionCompiler.CurrentFunction.AppendBasicBlock("promiseDone");
            LLVMValueRef pollResultIsSome = valueStorage[pollResultIsSomeIndex];
            builder.CreateCondBr(pollResultIsSome, promiseDoneBlock, promiseNotDoneBlock);

            builder.PositionBuilderAtEnd(promiseNotDoneBlock);
            builder.CreateRetVoid();

            builder.PositionBuilderAtEnd(promiseDoneBlock);
        }

        internal const uint MethodCallPromisePollFunctionPtrFieldIndex = 0u,
            MethodCallPromiseStatePtrFieldIndex = 1u,
            MethodCallPromiseOutputFieldIndex = 2u;

        internal static void BuildMethodCallPromisePollFunction(FunctionModuleContext moduleContext, NIType signature, LLVMValueRef methodCallPromisePollFunction)
        {
            NIType optionPromiseResultType = signature.GetParameters().ElementAt(2).GetDataType();
            NIType promiseResultType;
            optionPromiseResultType.TryDestructureOptionType(out promiseResultType);
            NIType resultType;
            bool mayPanic = promiseResultType.TryDestructurePanicResultType(out resultType);

            LLVMBasicBlockRef entryBlock = methodCallPromisePollFunction.AppendBasicBlock("entry"),
                targetDoneBlock = methodCallPromisePollFunction.AppendBasicBlock("targetDone"),
                targetNotDoneBlock = methodCallPromisePollFunction.AppendBasicBlock("targetNotDone");
            LLVMBasicBlockRef targetPanickedBlock = mayPanic
                ? methodCallPromisePollFunction.AppendBasicBlock("targetPanicked")
                : default(LLVMBasicBlockRef);
            var builder = moduleContext.LLVMContext.CreateIRBuilder();

            builder.PositionBuilderAtEnd(entryBlock);
            LLVMTypeRef stateType = moduleContext.LLVMContext.StructType(new[]
            {
                FunctionAllocationSet.FunctionCompletionStatusType(moduleContext.LLVMContext),
                moduleContext.LLVMContext.WakerType(),
            });

            LLVMValueRef promisePtr = methodCallPromisePollFunction.GetParam(0u),
                promise = builder.CreateLoad(promisePtr, "promise"),
                stateVoidPtr = builder.CreateExtractValue(promise, MethodCallPromiseStatePtrFieldIndex, "stateVoidPtr"),
                result = builder.CreateExtractValue(promise, MethodCallPromiseOutputFieldIndex, "result"),
                statePtr = builder.CreateBitCast(stateVoidPtr, LLVMTypeRef.PointerType(stateType, 0u), "statePtr"),
                state = builder.CreateLoad(statePtr, "state"),
                functionCompletionState = builder.CreateExtractValue(state, 0u, "functionCompletionState"),
                optionResultOutputPtr = methodCallPromisePollFunction.GetParam(2u);
            LLVMTypeRef optionResultOutputType = optionResultOutputPtr.TypeOf().GetElementType();

            uint switchCases = mayPanic ? 2u : 1u;
            LLVMValueRef completionStateSwitch = builder.CreateSwitch(functionCompletionState, targetNotDoneBlock, switchCases);
            completionStateSwitch.AddCase(moduleContext.LLVMContext.AsLLVMValue(RuntimeConstants.FunctionCompletedNormallyStatus), targetDoneBlock);
            if (mayPanic)
            {
                completionStateSwitch.AddCase(moduleContext.LLVMContext.AsLLVMValue(RuntimeConstants.FunctionPanickedStatus), targetPanickedBlock);
            }

            builder.PositionBuilderAtEnd(targetDoneBlock);
            builder.CreateFree(stateVoidPtr);
            LLVMValueRef finalResult = mayPanic ? builder.CreateInsertValue(result, moduleContext.LLVMContext.AsLLVMValue(true), 0u, "okResult") : result;
            LLVMValueRef someResult = moduleContext.LLVMContext.BuildOptionValue(builder, optionResultOutputType, finalResult);
            builder.CreateStore(someResult, optionResultOutputPtr);
            builder.CreateRetVoid();

            builder.PositionBuilderAtEnd(targetNotDoneBlock);
            LLVMValueRef waker = methodCallPromisePollFunction.GetParam(1u),
                wakerFunctionPtr = builder.CreateExtractValue(waker, 0u, "wakerFunctionPtr"),
                wakerStatePtr = builder.CreateExtractValue(waker, 1u, "wakerStatePtr"),
                pollFunctionPtr = builder.CreateExtractValue(promise, MethodCallPromisePollFunctionPtrFieldIndex, "pollFunctionPtr");
            builder.CreateCall(pollFunctionPtr, new LLVMValueRef[] { stateVoidPtr, wakerFunctionPtr, wakerStatePtr }, string.Empty);
            LLVMValueRef noneResult = moduleContext.LLVMContext.BuildOptionValue(builder, optionResultOutputType, null);
            builder.CreateStore(noneResult, optionResultOutputPtr);
            builder.CreateRetVoid();

            if (mayPanic)
            {
                builder.PositionBuilderAtEnd(targetPanickedBlock);
                LLVMValueRef panicResult = builder.CreateInsertValue(result, moduleContext.LLVMContext.AsLLVMValue(false), 0u, "panicResult"),
                    somePanicResult = moduleContext.LLVMContext.BuildOptionValue(builder, optionResultOutputType, panicResult);
                builder.CreateStore(somePanicResult, optionResultOutputPtr);
                builder.CreateRetVoid();
            }
        }

        internal static void GeneratePanicOrContinueBranch(IRBuilder builder, ICodeGenValueStorage valueSource, int panicOrContinueId, int shouldContinueIndex)
        {
            var functionCompiler = (FunctionCompiler)valueSource;
            LLVMBasicBlockRef continueBlock = functionCompiler.CurrentFunction.AppendBasicBlock($"continue_{panicOrContinueId}"),
                panicBlock = functionCompiler.CurrentFunction.AppendBasicBlock($"panic_{panicOrContinueId}");
            LLVMValueRef shouldContinue = valueSource[shouldContinueIndex];
            builder.CreateCondBr(shouldContinue, continueBlock, panicBlock);

            builder.PositionBuilderAtEnd(panicBlock);
            functionCompiler._moduleBuilder.GenerateStoreCompletionState(RuntimeConstants.FunctionPanickedStatus);
            builder.CreateBr(functionCompiler._moduleBuilder.CurrentGroupData.ExitBasicBlock);

            builder.PositionBuilderAtEnd(continueBlock);
        }

        #endregion

        private LLVMValueRef ReadCodeGenValue(int index)
        {
            LLVMValueRef value = _codeGenValues[index];
            value.ThrowIfNull();
            return value;
        }

        bool ICodeGenElementVisitor<bool>.VisitGetValue(GetValue getValue)
        {
            _codeGenValues[getValue.OutputIndex] = GetValueSource(getValue.Variable).GetValue(Builder);
            return true;
        }

        bool ICodeGenElementVisitor<bool>.VisitGetDereferencedValue(GetDereferencedValue getDereferencedValue)
        {
            _codeGenValues[getDereferencedValue.OutputIndex] = GetValueSource(getDereferencedValue.Variable).GetDereferencedValue(Builder);
            return true;
        }

        bool ICodeGenElementVisitor<bool>.VisitUpdateValue(UpdateValue updateValue)
        {
            Update(GetValueSource(updateValue.Variable), ReadCodeGenValue(updateValue.InputIndex));
            return true;
        }

        bool ICodeGenElementVisitor<bool>.VisitUpdateDereferencedValue(UpdateDereferencedValue updateDereferencedValue)
        {
            GetValueSource(updateDereferencedValue.Variable).UpdateDereferencedValue(Builder, ReadCodeGenValue(updateDereferencedValue.InputIndex));
            return true;
        }

        bool ICodeGenElementVisitor<bool>.VisitGetAddress(GetAddress getAddress)
        {
            _codeGenValues[getAddress.OutputIndex] = ((IAddressableValueSource)GetValueSource(getAddress.Variable)).GetAddress(Builder);
            return true;
        }

        bool ICodeGenElementVisitor<bool>.VisitInitializeValue(InitializeValue initializeValue)
        {
            InitializeIfNecessary(GetValueSource(initializeValue.Variable), builder => ReadCodeGenValue(initializeValue.InputIndex));
            return true;
        }

        bool ICodeGenElementVisitor<bool>.VisitInitializeAsReference(InitializeAsReference initializeAsReference)
        {
            InitializeIfNecessary(
                GetValueSource(initializeAsReference.InitializedVariable),
                builder => ((IAddressableValueSource)GetValueSource(initializeAsReference.ReferencedVariable)).GetAddress(builder));
            return true;
        }

        bool ICodeGenElementVisitor<bool>.VisitInitializeWithCopy(InitializeWithCopy initializeWithCopy)
        {
            InitializeIfNecessary(
                GetValueSource(initializeWithCopy.InitializedVariable),
                GetValueSource(initializeWithCopy.CopiedVariable).GetValue);
            return true;
        }

        bool ICodeGenElementVisitor<bool>.VisitBuildStruct(BuildStruct buildStruct)
        {
            LLVMValueRef[] fieldValues = buildStruct.InputIndices.Select(ReadCodeGenValue).ToArray();
            _codeGenValues[buildStruct.OutputIndex] = Builder.BuildStructValue(buildStruct.StructType, fieldValues, "tuple");
            return true;
        }

        bool ICodeGenElementVisitor<bool>.VisitGetStructFieldValue(GetStructFieldValue getStructFieldValue)
        {
            _codeGenValues[getStructFieldValue.OutputIndex] = Builder.CreateExtractValue(
                ReadCodeGenValue(getStructFieldValue.InputIndex),
                (uint)getStructFieldValue.FieldIndex,
                "field");
            return true;
        }

        bool ICodeGenElementVisitor<bool>.VisitGetStructFieldPointer(GetStructFieldPointer getStructFieldPointer)
        {
            _codeGenValues[getStructFieldPointer.OutputIndex] = Builder.CreateStructGEP(
                ReadCodeGenValue(getStructFieldPointer.InputIndex),
                (uint)getStructFieldPointer.FieldIndex,
                "fieldPtr");
            return true;
        }

        bool ICodeGenElementVisitor<bool>.VisitGetConstant(GetConstant getConstant)
        {
            _codeGenValues[getConstant.OutputIndex] = getConstant.ValueCreator(ModuleContext, Builder);
            return true;
        }

        bool ICodeGenElementVisitor<bool>.VisitCall(Call call)
        {
            LLVMValueRef[] arguments = call.ArgumentIndices.Select(ReadCodeGenValue).ToArray();
            Builder.CreateCall(call.Function, arguments, string.Empty);
            return true;
        }

        bool ICodeGenElementVisitor<bool>.VisitCallWithReturn(CallWithReturn callWithReturn)
        {
            LLVMValueRef[] arguments = callWithReturn.ArgumentIndices.Select(ReadCodeGenValue).ToArray();
            _codeGenValues[callWithReturn.ReturnValueIndex] = Builder.CreateCall(callWithReturn.Function, arguments, "retVal");
            return true;
        }

        bool ICodeGenElementVisitor<bool>.VisitOp(Op op)
        {
            op.GenerateOp(Builder, this);
            return true;
        }

        bool ICodeGenElementVisitor<bool>.VisitShareValue(ShareValue shareValue)
        {
            return true;
        }

        LLVMValueRef ICodeGenValueStorage.this[int index]
        {
            get { return ReadCodeGenValue(index); }
            set { _codeGenValues[index] = value; }
        }
    }

    internal abstract class FunctionCompilerState
    {
        protected FunctionCompilerState(LLVMValueRef function, IRBuilder builder)
        {
            Function = function;
            Builder = builder;
        }

        public LLVMValueRef Function { get; }

        public IRBuilder Builder { get; }

        public abstract LLVMValueRef StatePointer { get; }
    }

    internal class AsyncStateGroupCompilerState : FunctionCompilerState
    {
        public AsyncStateGroupCompilerState(LLVMValueRef function, IRBuilder builder)
            : base(function, builder)
        {
        }

        public override LLVMValueRef StatePointer => Function.GetParam(0u);
    }

    internal class OuterFunctionCompilerState : FunctionCompilerState
    {
        public OuterFunctionCompilerState(LLVMValueRef function, IRBuilder builder)
            : base(function, builder)
        {
        }

        public override LLVMValueRef StatePointer => StateMalloc;

        public LLVMValueRef StateMalloc { get; set; }
    }
}
