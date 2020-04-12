using System.Linq;
using LLVMSharp;
using NationalInstruments.DataTypes;
using Rebar.Common;

namespace Rebar.RebarTarget.LLVM
{
    internal partial class FunctionCompiler
    {
        private const int WriterReady = 0x0,
            WriterDroppedWithoutValue = 0x2,
            WriterDroppedWithValue = 0x3,
            WriterStateMask = 0x3,
            ReaderReady = 0x0,
            ReaderWaitingForValue = 0x4,
            ReaderDropped = 0xC,
            ReaderStateMask = 0xC;

        private const uint SharedDataWakerFieldIndex = 0u,
            SharedDataValueFieldIndex = 1u,
            SharedDataStateFieldIndex = 2u;

        private static void BuildCreateNotifierPairFunction(FunctionCompiler compiler, NIType signature, LLVMValueRef createNotifierPairFunction)
        {
            LLVMTypeRef valueType = compiler.Context.AsLLVMType(signature.GetGenericParameters().First()),
                notifierReaderType = compiler.Context.CreateLLVMNotifierReaderType(valueType),
                notifierWriterType = compiler.Context.CreateLLVMNotifierWriterType(valueType);

            LLVMBasicBlockRef entryBlock = createNotifierPairFunction.AppendBasicBlock("entry");
            var builder = compiler.Context.CreateIRBuilder();

            builder.PositionBuilderAtEnd(entryBlock);
            LLVMTypeRef sharedDataType = compiler.Context.CreateLLVMNotifierSharedDataType(valueType);
            LLVMTypeRef refCountType = compiler.Context.CreateLLVMRefCountType(sharedDataType);
            LLVMValueRef refCountAllocationPtr = builder.CreateMalloc(refCountType, "refCountAllocationPtr"),
                refCount = builder.BuildStructValue(
                    refCountType,
                    new LLVMValueRef[] { compiler.Context.AsLLVMValue(2), LLVMSharp.LLVM.ConstNull(sharedDataType) },
                    "refCount");
            builder.CreateStore(refCount, refCountAllocationPtr);

            LLVMValueRef notifierReader = builder.BuildStructValue(notifierReaderType, new[] { refCountAllocationPtr });
            builder.CreateStore(notifierReader, createNotifierPairFunction.GetParam(0u));
            LLVMValueRef notifierWriter = builder.BuildStructValue(notifierWriterType, new[] { refCountAllocationPtr });
            builder.CreateStore(notifierWriter, createNotifierPairFunction.GetParam(1u));
            builder.CreateRetVoid();
        }

        private static void BuildGetNotifierReaderPromiseFunction(FunctionCompiler compiler, NIType signature, LLVMValueRef getNotifierReaderPromiseFunction)
        {
            LLVMTypeRef valueType = compiler.Context.AsLLVMType(signature.GetGenericParameters().First()),
                notifierReaderType = compiler.Context.CreateLLVMNotifierReaderType(valueType),
                notifierReaderPromiseType = compiler.Context.CreateLLVMNotifierReaderPromiseType(valueType);

            LLVMBasicBlockRef entryBlock = getNotifierReaderPromiseFunction.AppendBasicBlock("entry");
            var builder = compiler.Context.CreateIRBuilder();

            builder.PositionBuilderAtEnd(entryBlock);
            // Transfer the refCount pointer from the reader to the promise
            LLVMValueRef notifierReader = getNotifierReaderPromiseFunction.GetParam(0u),
                refCountPtr = builder.CreateExtractValue(notifierReader, 0u, "refCountPtr"),
                notifierReaderPromise = builder.BuildStructValue(notifierReaderPromiseType, new[] { refCountPtr });
            builder.CreateStore(notifierReaderPromise, getNotifierReaderPromiseFunction.GetParam(1u));
            builder.CreateRetVoid();
        }

        private static void BuildNotifierReaderPromisePollFunction(FunctionCompiler compiler, NIType signature, LLVMValueRef notifierReaderPromisePollFunction)
        {
            NIType notifierReaderPromiseType = signature.GetGenericParameters().ElementAt(0),
                valueType = notifierReaderPromiseType.GetGenericParameters().ElementAt(0);
            LLVMTypeRef optionValueLLVMType = compiler.Context.AsLLVMType(valueType.CreateOption()),
                optionOptionValueLLVMType = compiler.Context.AsLLVMType(valueType.CreateOption().CreateOption());

            LLVMBasicBlockRef entryBlock = notifierReaderPromisePollFunction.AppendBasicBlock("entry"),
                writerReadyBlock = notifierReaderPromisePollFunction.AppendBasicBlock("writerReady"),
                writerDroppedWithoutValueBlock = notifierReaderPromisePollFunction.AppendBasicBlock("writerDroppedWithoutValue"),
                writerDroppedWithValueBlock = notifierReaderPromisePollFunction.AppendBasicBlock("writerDroppedWithValue"),
                endBlock = notifierReaderPromisePollFunction.AppendBasicBlock("end");
            var builder = compiler.Context.CreateIRBuilder();

            builder.PositionBuilderAtEnd(entryBlock);
            LLVMValueRef notifierReaderPromisePtr = notifierReaderPromisePollFunction.GetParam(0u),
                waker = notifierReaderPromisePollFunction.GetParam(1u),
                pollResultPtr = notifierReaderPromisePollFunction.GetParam(2u),
                notifierReaderPromise = builder.CreateLoad(notifierReaderPromisePtr, "notifierReaderPromise"),
                refCountPtr = builder.CreateExtractValue(notifierReaderPromise, 0u, "refCountPtr"),
                sharedDataPtr = builder.CreateStructGEP(refCountPtr, 1u, "sharedDataPtr"),
                wakerPtr = builder.CreateStructGEP(sharedDataPtr, SharedDataWakerFieldIndex, "wakerPtr"),
                statePtr = builder.CreateStructGEP(sharedDataPtr, SharedDataStateFieldIndex, "statePtr");
            builder.CreateStore(waker, wakerPtr);
            // This is Release because it needs the writer thread to observe the waker store,
            // and Acquire because it may need to observe the writer thread's value store.
            LLVMValueRef oldState = builder.CreateAtomicRMW(
                LLVMAtomicRMWBinOp.LLVMAtomicRMWBinOpOr,
                statePtr,
                compiler.Context.AsLLVMValue(ReaderWaitingForValue),
                LLVMAtomicOrdering.LLVMAtomicOrderingAcquireRelease,
                false),
                oldWriterState = builder.CreateAnd(oldState, compiler.Context.AsLLVMValue(WriterStateMask), "oldWriterState");
            LLVMValueRef writerStateSwitch = builder.CreateSwitch(oldWriterState, writerReadyBlock, 2u);
            writerStateSwitch.AddCase(compiler.Context.AsLLVMValue(WriterDroppedWithoutValue), writerDroppedWithoutValueBlock);
            writerStateSwitch.AddCase(compiler.Context.AsLLVMValue(WriterDroppedWithValue), writerDroppedWithValueBlock);

            builder.PositionBuilderAtEnd(writerReadyBlock);
            // output None
            builder.CreateStore(LLVMTypeRef.ConstNull(optionOptionValueLLVMType), pollResultPtr);
            builder.CreateRetVoid();

            builder.PositionBuilderAtEnd(writerDroppedWithoutValueBlock);
            // output Some(None)
            builder.CreateStore(compiler.Context.BuildOptionValue(builder, optionOptionValueLLVMType, LLVMTypeRef.ConstNull(optionValueLLVMType)), pollResultPtr);
            builder.CreateBr(endBlock);

            builder.PositionBuilderAtEnd(writerDroppedWithValueBlock);
            // output Some(Some(value))
            LLVMValueRef valuePtr = builder.CreateStructGEP(sharedDataPtr, SharedDataValueFieldIndex, "valuePtr"),
                value = builder.CreateLoad(valuePtr, "value");
            builder.CreateStore(compiler.Context.BuildOptionValue(builder, optionOptionValueLLVMType, compiler.Context.BuildOptionValue(builder, optionValueLLVMType, value)), pollResultPtr);
            builder.CreateBr(endBlock);

            builder.PositionBuilderAtEnd(endBlock);
            LLVMValueRef decrementRefCountFunction = compiler.GetDecrementRefCountFunction(valueType.CreateNotifierSharedDataType());
            builder.CreateCall(decrementRefCountFunction, new LLVMValueRef[] { refCountPtr }, string.Empty);
            builder.CreateRetVoid();
        }

        internal static void BuildNotifierReaderDropFunction(FunctionCompiler compiler, NIType signature, LLVMValueRef notifierReaderDropFunction)
        {
            NIType notifierReaderType = signature.GetGenericParameters().ElementAt(0),
                valueType = notifierReaderType.GetGenericParameters().ElementAt(0);

            LLVMBasicBlockRef entryBlock = notifierReaderDropFunction.AppendBasicBlock("entry"),
                writerWasDroppedWithValueBlock = notifierReaderDropFunction.AppendBasicBlock("writerWasDroppedWithValue"),
                endBlock = notifierReaderDropFunction.AppendBasicBlock("end");
            var builder = compiler.Context.CreateIRBuilder();

            builder.PositionBuilderAtEnd(entryBlock);
            LLVMValueRef notifierReaderPtr = notifierReaderDropFunction.GetParam(0u),
                notifierReader = builder.CreateLoad(notifierReaderPtr, "notifierReaderPromise"),
                refCountPtr = builder.CreateExtractValue(notifierReader, 0u, "refCountPtr"),
                sharedDataPtr = builder.CreateStructGEP(refCountPtr, 1u, "sharedDataPtr"),
                statePtr = builder.CreateStructGEP(sharedDataPtr, SharedDataStateFieldIndex, "statePtr");
            // This is Acquire because it may need to observe the writer's value store.
            LLVMValueRef oldState = builder.CreateAtomicRMW(
                LLVMAtomicRMWBinOp.LLVMAtomicRMWBinOpOr,
                statePtr,
                compiler.Context.AsLLVMValue(ReaderDropped),
                LLVMAtomicOrdering.LLVMAtomicOrderingAcquire,
                false),
                oldWriterState = builder.CreateAnd(oldState, compiler.Context.AsLLVMValue(WriterStateMask), "oldWriterState"),
                writerWasDroppedWithValue = builder.CreateICmp(LLVMIntPredicate.LLVMIntEQ, oldWriterState, compiler.Context.AsLLVMValue(WriterDroppedWithValue), "writerWasDroppedWithValue");
            builder.CreateCondBr(writerWasDroppedWithValue, writerWasDroppedWithValueBlock, endBlock);

            builder.PositionBuilderAtEnd(writerWasDroppedWithValueBlock);
            compiler.CreateDropCallIfDropFunctionExists(builder, valueType, b => b.CreateStructGEP(sharedDataPtr, SharedDataValueFieldIndex, "valuePtr"));
            builder.CreateBr(endBlock);

            builder.PositionBuilderAtEnd(endBlock);
            LLVMValueRef decrementRefCountFunction = compiler.GetDecrementRefCountFunction(valueType.CreateNotifierSharedDataType());
            builder.CreateCall(decrementRefCountFunction, new LLVMValueRef[] { refCountPtr }, string.Empty);
            builder.CreateRetVoid();
        }

        private static void BuildSetNotifierValueFunction(FunctionCompiler compiler, NIType signature, LLVMValueRef setNotifierValueFunction)
        {
            NIType valueType = signature.GetGenericParameters().First();
            LLVMTypeRef valueLLVMType = compiler.Context.AsLLVMType(valueType);

            LLVMBasicBlockRef entryBlock = setNotifierValueFunction.AppendBasicBlock("entry"),
                readerWasWaitingBlock = setNotifierValueFunction.AppendBasicBlock("readerWasWaiting"),
                readerWasDroppedBlock = setNotifierValueFunction.AppendBasicBlock("readerWasDropped"),
                exitBlock = setNotifierValueFunction.AppendBasicBlock("exit");
            var builder = compiler.Context.CreateIRBuilder();

            builder.PositionBuilderAtEnd(entryBlock);
            LLVMValueRef notifierWriter = setNotifierValueFunction.GetParam(0u),
                value = setNotifierValueFunction.GetParam(1u),
                refCountPtr = builder.CreateExtractValue(notifierWriter, 0u, "refCountPtr"),
                sharedDataPtr = builder.CreateStructGEP(refCountPtr, 1u, "sharedDataPtr"),
                valuePtr = builder.CreateStructGEP(sharedDataPtr, SharedDataValueFieldIndex, "valuePtr"),
                statePtr = builder.CreateStructGEP(sharedDataPtr, SharedDataStateFieldIndex, "statePtr");
            builder.CreateStore(value, valuePtr);
            // This is Release because it needs the reader thread to observe the value store above,
            // and Acquire because it may need to observe the reader thread's waker store below.
            LLVMValueRef oldState = builder.CreateAtomicRMW(
                    LLVMAtomicRMWBinOp.LLVMAtomicRMWBinOpOr,
                    statePtr,
                    compiler.Context.AsLLVMValue(WriterDroppedWithValue),
                    LLVMAtomicOrdering.LLVMAtomicOrderingAcquireRelease,
                    false);
            LLVMValueRef oldReaderState = builder.CreateAnd(oldState, compiler.Context.AsLLVMValue(ReaderStateMask), "oldReaderState"),
                readerStateSwitch = builder.CreateSwitch(oldReaderState, exitBlock, 2u);
            readerStateSwitch.AddCase(compiler.Context.AsLLVMValue(ReaderWaitingForValue), readerWasWaitingBlock);
            readerStateSwitch.AddCase(compiler.Context.AsLLVMValue(ReaderDropped), readerWasDroppedBlock);

            builder.PositionBuilderAtEnd(readerWasWaitingBlock);
            LLVMValueRef wakerPtr = builder.CreateStructGEP(sharedDataPtr, SharedDataWakerFieldIndex, "wakerPtr"),
                waker = builder.CreateLoad(wakerPtr, "waker");
            builder.CreateCall(compiler.GetImportedCommonFunction(CommonModules.InvokeName), new LLVMValueRef[] { waker }, string.Empty);
            builder.CreateBr(exitBlock);

            builder.PositionBuilderAtEnd(readerWasDroppedBlock);
            compiler.CreateDropCallIfDropFunctionExists(builder, valueType, _ => valuePtr);
            builder.CreateBr(exitBlock);

            builder.PositionBuilderAtEnd(exitBlock);
            LLVMValueRef decrementRefCountFunction = compiler.GetDecrementRefCountFunction(valueType.CreateNotifierSharedDataType());
            builder.CreateCall(decrementRefCountFunction, new LLVMValueRef[] { refCountPtr }, string.Empty);
            builder.CreateRetVoid();
        }

        internal static void BuildNotifierWriterDropFunction(FunctionCompiler compiler, NIType signature, LLVMValueRef notifierValueDropFunction)
        {
            NIType notifierWriterType = signature.GetGenericParameters().First(),
                valueType = notifierWriterType.GetGenericParameters().First();

            LLVMBasicBlockRef entryBlock = notifierValueDropFunction.AppendBasicBlock("entry"),
                readerWasWaitingBlock = notifierValueDropFunction.AppendBasicBlock("readerWasWaiting"),
                exitBlock = notifierValueDropFunction.AppendBasicBlock("exit");
            var builder = compiler.Context.CreateIRBuilder();

            builder.PositionBuilderAtEnd(entryBlock);
            LLVMValueRef notifierWriterPtr = notifierValueDropFunction.GetParam(0u),
                notifierWriter = builder.CreateLoad(notifierWriterPtr, "notifierWriter"),
                refCountPtr = builder.CreateExtractValue(notifierWriter, 0u, "refCountPtr"),
                sharedDataPtr = builder.CreateStructGEP(refCountPtr, 1u, "sharedDataPtr"),
                valuePtr = builder.CreateStructGEP(sharedDataPtr, SharedDataValueFieldIndex, "valuePtr"),
                statePtr = builder.CreateStructGEP(sharedDataPtr, SharedDataStateFieldIndex, "statePtr");
            // This is Acquire because it may need to observe the reader thread's waker store below.
            LLVMValueRef oldState = builder.CreateAtomicRMW(
                LLVMAtomicRMWBinOp.LLVMAtomicRMWBinOpOr,
                statePtr,
                compiler.Context.AsLLVMValue(WriterDroppedWithoutValue),
                LLVMAtomicOrdering.LLVMAtomicOrderingAcquire,
                false);
            LLVMValueRef oldReaderState = builder.CreateAnd(oldState, compiler.Context.AsLLVMValue(ReaderStateMask), "oldReaderState"),
                readerWasWaiting = builder.CreateICmp(LLVMIntPredicate.LLVMIntEQ, oldReaderState, compiler.Context.AsLLVMValue(ReaderWaitingForValue), "readerWasWaiting");
            builder.CreateCondBr(readerWasWaiting, readerWasWaitingBlock, exitBlock);

            builder.PositionBuilderAtEnd(readerWasWaitingBlock);
            LLVMValueRef wakerPtr = builder.CreateStructGEP(sharedDataPtr, SharedDataWakerFieldIndex, "wakerPtr"),
                waker = builder.CreateLoad(wakerPtr, "waker");
            builder.CreateCall(compiler.GetImportedCommonFunction(CommonModules.InvokeName), new LLVMValueRef[] { waker }, string.Empty);
            builder.CreateBr(exitBlock);

            builder.PositionBuilderAtEnd(exitBlock);
            LLVMValueRef decrementRefCountFunction = compiler.GetDecrementRefCountFunction(valueType.CreateNotifierSharedDataType());
            builder.CreateCall(decrementRefCountFunction, new LLVMValueRef[] { refCountPtr }, string.Empty);
            builder.CreateRetVoid();
        }
    }
}
