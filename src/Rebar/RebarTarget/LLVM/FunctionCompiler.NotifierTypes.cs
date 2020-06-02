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

        private static void BuildCreateNotifierPairFunction(FunctionModuleContext moduleContext, NIType signature, LLVMValueRef createNotifierPairFunction)
        {
            LLVMTypeRef valueType = moduleContext.LLVMContext.AsLLVMType(signature.GetGenericParameters().First()),
                notifierReaderType = moduleContext.LLVMContext.CreateLLVMNotifierReaderType(valueType),
                notifierWriterType = moduleContext.LLVMContext.CreateLLVMNotifierWriterType(valueType);

            LLVMBasicBlockRef entryBlock = createNotifierPairFunction.AppendBasicBlock("entry");
            var builder = moduleContext.LLVMContext.CreateIRBuilder();

            builder.PositionBuilderAtEnd(entryBlock);
            LLVMTypeRef sharedDataType = moduleContext.LLVMContext.CreateLLVMNotifierSharedDataType(valueType);
            LLVMTypeRef refCountType = moduleContext.LLVMContext.CreateLLVMRefCountType(sharedDataType);
            LLVMValueRef refCountAllocationPtr = moduleContext.CreateMalloc(builder, refCountType, "refCountAllocationPtr"),
                refCount = builder.BuildStructValue(
                    refCountType,
                    new LLVMValueRef[] { moduleContext.LLVMContext.AsLLVMValue(2), LLVMSharp.LLVM.ConstNull(sharedDataType) },
                    "refCount");
            builder.CreateStore(refCount, refCountAllocationPtr);

            LLVMValueRef notifierReader = builder.BuildStructValue(notifierReaderType, new[] { refCountAllocationPtr });
            builder.CreateStore(notifierReader, createNotifierPairFunction.GetParam(0u));
            LLVMValueRef notifierWriter = builder.BuildStructValue(notifierWriterType, new[] { refCountAllocationPtr });
            builder.CreateStore(notifierWriter, createNotifierPairFunction.GetParam(1u));
            builder.CreateRetVoid();
        }

        private static void BuildGetNotifierReaderPromiseFunction(FunctionModuleContext moduleContext, NIType signature, LLVMValueRef getNotifierReaderPromiseFunction)
        {
            LLVMTypeRef valueType = moduleContext.LLVMContext.AsLLVMType(signature.GetGenericParameters().First()),
                notifierReaderType = moduleContext.LLVMContext.CreateLLVMNotifierReaderType(valueType),
                notifierReaderPromiseType = moduleContext.LLVMContext.CreateLLVMNotifierReaderPromiseType(valueType);

            LLVMBasicBlockRef entryBlock = getNotifierReaderPromiseFunction.AppendBasicBlock("entry");
            var builder = moduleContext.LLVMContext.CreateIRBuilder();

            builder.PositionBuilderAtEnd(entryBlock);
            // Transfer the refCount pointer from the reader to the promise
            LLVMValueRef notifierReader = getNotifierReaderPromiseFunction.GetParam(0u),
                refCountPtr = builder.CreateExtractValue(notifierReader, 0u, "refCountPtr"),
                notifierReaderPromise = builder.BuildStructValue(notifierReaderPromiseType, new[] { refCountPtr });
            builder.CreateStore(notifierReaderPromise, getNotifierReaderPromiseFunction.GetParam(1u));
            builder.CreateRetVoid();
        }

        private static void BuildNotifierReaderPromisePollFunction(FunctionModuleContext moduleContext, NIType signature, LLVMValueRef notifierReaderPromisePollFunction)
        {
            NIType notifierReaderPromiseType = signature.GetGenericParameters().ElementAt(0),
                valueType = notifierReaderPromiseType.GetGenericParameters().ElementAt(0);
            LLVMTypeRef optionValueLLVMType = moduleContext.LLVMContext.AsLLVMType(valueType.CreateOption()),
                optionOptionValueLLVMType = moduleContext.LLVMContext.AsLLVMType(valueType.CreateOption().CreateOption());

            LLVMBasicBlockRef entryBlock = notifierReaderPromisePollFunction.AppendBasicBlock("entry"),
                writerReadyBlock = notifierReaderPromisePollFunction.AppendBasicBlock("writerReady"),
                writerDroppedWithoutValueBlock = notifierReaderPromisePollFunction.AppendBasicBlock("writerDroppedWithoutValue"),
                writerDroppedWithValueBlock = notifierReaderPromisePollFunction.AppendBasicBlock("writerDroppedWithValue"),
                endBlock = notifierReaderPromisePollFunction.AppendBasicBlock("end");
            var builder = moduleContext.LLVMContext.CreateIRBuilder();

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
                moduleContext.LLVMContext.AsLLVMValue(ReaderWaitingForValue),
                LLVMAtomicOrdering.LLVMAtomicOrderingAcquireRelease,
                false),
                oldWriterState = builder.CreateAnd(oldState, moduleContext.LLVMContext.AsLLVMValue(WriterStateMask), "oldWriterState");
            LLVMValueRef writerStateSwitch = builder.CreateSwitch(oldWriterState, writerReadyBlock, 2u);
            writerStateSwitch.AddCase(moduleContext.LLVMContext.AsLLVMValue(WriterDroppedWithoutValue), writerDroppedWithoutValueBlock);
            writerStateSwitch.AddCase(moduleContext.LLVMContext.AsLLVMValue(WriterDroppedWithValue), writerDroppedWithValueBlock);

            builder.PositionBuilderAtEnd(writerReadyBlock);
            // output None
            builder.CreateStore(LLVMTypeRef.ConstNull(optionOptionValueLLVMType), pollResultPtr);
            builder.CreateRetVoid();

            builder.PositionBuilderAtEnd(writerDroppedWithoutValueBlock);
            // output Some(None)
            builder.CreateStore(moduleContext.LLVMContext.BuildOptionValue(builder, optionOptionValueLLVMType, LLVMTypeRef.ConstNull(optionValueLLVMType)), pollResultPtr);
            builder.CreateBr(endBlock);

            builder.PositionBuilderAtEnd(writerDroppedWithValueBlock);
            // output Some(Some(value))
            LLVMValueRef valuePtr = builder.CreateStructGEP(sharedDataPtr, SharedDataValueFieldIndex, "valuePtr"),
                value = builder.CreateLoad(valuePtr, "value");
            builder.CreateStore(moduleContext.LLVMContext.BuildOptionValue(builder, optionOptionValueLLVMType, moduleContext.LLVMContext.BuildOptionValue(builder, optionValueLLVMType, value)), pollResultPtr);
            builder.CreateBr(endBlock);

            builder.PositionBuilderAtEnd(endBlock);
            LLVMValueRef decrementRefCountFunction = GetDecrementRefCountFunction(moduleContext, valueType.CreateNotifierSharedDataType());
            builder.CreateCall(decrementRefCountFunction, new LLVMValueRef[] { refCountPtr }, string.Empty);
            builder.CreateRetVoid();
        }

        internal static void BuildNotifierReaderDropFunction(FunctionModuleContext moduleContext, NIType signature, LLVMValueRef notifierReaderDropFunction)
        {
            NIType notifierReaderType = signature.GetGenericParameters().ElementAt(0),
                valueType = notifierReaderType.GetGenericParameters().ElementAt(0);

            LLVMBasicBlockRef entryBlock = notifierReaderDropFunction.AppendBasicBlock("entry"),
                writerWasDroppedWithValueBlock = notifierReaderDropFunction.AppendBasicBlock("writerWasDroppedWithValue"),
                endBlock = notifierReaderDropFunction.AppendBasicBlock("end");
            var builder = moduleContext.LLVMContext.CreateIRBuilder();

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
                moduleContext.LLVMContext.AsLLVMValue(ReaderDropped),
                LLVMAtomicOrdering.LLVMAtomicOrderingAcquire,
                false),
                oldWriterState = builder.CreateAnd(oldState, moduleContext.LLVMContext.AsLLVMValue(WriterStateMask), "oldWriterState"),
                writerWasDroppedWithValue = builder.CreateICmp(LLVMIntPredicate.LLVMIntEQ, oldWriterState, moduleContext.LLVMContext.AsLLVMValue(WriterDroppedWithValue), "writerWasDroppedWithValue");
            builder.CreateCondBr(writerWasDroppedWithValue, writerWasDroppedWithValueBlock, endBlock);

            builder.PositionBuilderAtEnd(writerWasDroppedWithValueBlock);
            moduleContext.CreateDropCallIfDropFunctionExists(builder, valueType, b => b.CreateStructGEP(sharedDataPtr, SharedDataValueFieldIndex, "valuePtr"));
            builder.CreateBr(endBlock);

            builder.PositionBuilderAtEnd(endBlock);
            LLVMValueRef decrementRefCountFunction = GetDecrementRefCountFunction(moduleContext, valueType.CreateNotifierSharedDataType());
            builder.CreateCall(decrementRefCountFunction, new LLVMValueRef[] { refCountPtr }, string.Empty);
            builder.CreateRetVoid();
        }

        private static void BuildSetNotifierValueFunction(FunctionModuleContext moduleContext, NIType signature, LLVMValueRef setNotifierValueFunction)
        {
            NIType valueType = signature.GetGenericParameters().First();
            LLVMTypeRef valueLLVMType = moduleContext.LLVMContext.AsLLVMType(valueType);

            LLVMBasicBlockRef entryBlock = setNotifierValueFunction.AppendBasicBlock("entry"),
                readerWasWaitingBlock = setNotifierValueFunction.AppendBasicBlock("readerWasWaiting"),
                readerWasDroppedBlock = setNotifierValueFunction.AppendBasicBlock("readerWasDropped"),
                exitBlock = setNotifierValueFunction.AppendBasicBlock("exit");
            var builder = moduleContext.LLVMContext.CreateIRBuilder();

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
                    moduleContext.LLVMContext.AsLLVMValue(WriterDroppedWithValue),
                    LLVMAtomicOrdering.LLVMAtomicOrderingAcquireRelease,
                    false);
            LLVMValueRef oldReaderState = builder.CreateAnd(oldState, moduleContext.LLVMContext.AsLLVMValue(ReaderStateMask), "oldReaderState"),
                readerStateSwitch = builder.CreateSwitch(oldReaderState, exitBlock, 2u);
            readerStateSwitch.AddCase(moduleContext.LLVMContext.AsLLVMValue(ReaderWaitingForValue), readerWasWaitingBlock);
            readerStateSwitch.AddCase(moduleContext.LLVMContext.AsLLVMValue(ReaderDropped), readerWasDroppedBlock);

            builder.PositionBuilderAtEnd(readerWasWaitingBlock);
            LLVMValueRef wakerPtr = builder.CreateStructGEP(sharedDataPtr, SharedDataWakerFieldIndex, "wakerPtr"),
                waker = builder.CreateLoad(wakerPtr, "waker");
            builder.CreateCall(moduleContext.FunctionImporter.GetImportedCommonFunction(CommonModules.InvokeName), new LLVMValueRef[] { waker }, string.Empty);
            builder.CreateBr(exitBlock);

            builder.PositionBuilderAtEnd(readerWasDroppedBlock);
            moduleContext.CreateDropCallIfDropFunctionExists(builder, valueType, _ => valuePtr);
            builder.CreateBr(exitBlock);

            builder.PositionBuilderAtEnd(exitBlock);
            LLVMValueRef decrementRefCountFunction = GetDecrementRefCountFunction(moduleContext, valueType.CreateNotifierSharedDataType());
            builder.CreateCall(decrementRefCountFunction, new LLVMValueRef[] { refCountPtr }, string.Empty);
            builder.CreateRetVoid();
        }

        internal static void BuildNotifierWriterDropFunction(FunctionModuleContext moduleContext, NIType signature, LLVMValueRef notifierValueDropFunction)
        {
            NIType notifierWriterType = signature.GetGenericParameters().First(),
                valueType = notifierWriterType.GetGenericParameters().First();

            LLVMBasicBlockRef entryBlock = notifierValueDropFunction.AppendBasicBlock("entry"),
                readerWasWaitingBlock = notifierValueDropFunction.AppendBasicBlock("readerWasWaiting"),
                exitBlock = notifierValueDropFunction.AppendBasicBlock("exit");
            var builder = moduleContext.LLVMContext.CreateIRBuilder();

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
                moduleContext.LLVMContext.AsLLVMValue(WriterDroppedWithoutValue),
                LLVMAtomicOrdering.LLVMAtomicOrderingAcquire,
                false);
            LLVMValueRef oldReaderState = builder.CreateAnd(oldState, moduleContext.LLVMContext.AsLLVMValue(ReaderStateMask), "oldReaderState"),
                readerWasWaiting = builder.CreateICmp(LLVMIntPredicate.LLVMIntEQ, oldReaderState, moduleContext.LLVMContext.AsLLVMValue(ReaderWaitingForValue), "readerWasWaiting");
            builder.CreateCondBr(readerWasWaiting, readerWasWaitingBlock, exitBlock);

            builder.PositionBuilderAtEnd(readerWasWaitingBlock);
            LLVMValueRef wakerPtr = builder.CreateStructGEP(sharedDataPtr, SharedDataWakerFieldIndex, "wakerPtr"),
                waker = builder.CreateLoad(wakerPtr, "waker");
            builder.CreateCall(moduleContext.FunctionImporter.GetImportedCommonFunction(CommonModules.InvokeName), new LLVMValueRef[] { waker }, string.Empty);
            builder.CreateBr(exitBlock);

            builder.PositionBuilderAtEnd(exitBlock);
            LLVMValueRef decrementRefCountFunction = GetDecrementRefCountFunction(moduleContext, valueType.CreateNotifierSharedDataType());
            builder.CreateCall(decrementRefCountFunction, new LLVMValueRef[] { refCountPtr }, string.Empty);
            builder.CreateRetVoid();
        }
    }
}
