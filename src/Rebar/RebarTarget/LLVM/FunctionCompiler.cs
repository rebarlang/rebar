using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using LLVMSharp;
using NationalInstruments;
using NationalInstruments.Compiler;
using NationalInstruments.DataTypes;
using NationalInstruments.Dfir;
using Rebar.Common;
using Rebar.Compiler;
using Rebar.Compiler.Nodes;

namespace Rebar.RebarTarget.LLVM
{
    internal partial class FunctionCompiler : VisitorTransformBase, IDfirNodeVisitor<bool>, IDfirStructureVisitor<bool>
    {
        private static readonly Dictionary<string, Action<FunctionCompiler, FunctionalNode>> _functionalNodeCompilers;

        static FunctionCompiler()
        {
            _functionalNodeCompilers = new Dictionary<string, Action<FunctionCompiler, FunctionalNode>>();
            _functionalNodeCompilers["ImmutPass"] = CompileNothing;
            _functionalNodeCompilers["MutPass"] = CompileNothing;
            _functionalNodeCompilers["Inspect"] = CompileInspect;
            _functionalNodeCompilers["FakeDropCreate"] = CreateImportedCommonFunctionCompiler(CommonModules.FakeDropCreateName);
            _functionalNodeCompilers["Output"] = CompileOutput;

            _functionalNodeCompilers["Assign"] = CompileAssign;
            _functionalNodeCompilers["Exchange"] = CompileExchange;
            _functionalNodeCompilers["CreateCopy"] = CompileCreateCopy;
            _functionalNodeCompilers["SelectReference"] = CompileSelectReference;

            _functionalNodeCompilers["Add"] = CreatePureBinaryOperationCompiler((compiler, left, right) => compiler._builder.CreateAdd(left, right, "add"));
            _functionalNodeCompilers["Subtract"] = CreatePureBinaryOperationCompiler((compiler, left, right) => compiler._builder.CreateSub(left, right, "subtract"));
            _functionalNodeCompilers["Multiply"] = CreatePureBinaryOperationCompiler((compiler, left, right) => compiler._builder.CreateMul(left, right, "multiply"));
            _functionalNodeCompilers["Divide"] = CreatePureBinaryOperationCompiler((compiler, left, right) => compiler._builder.CreateSDiv(left, right, "divide"));
            _functionalNodeCompilers["Modulus"] = CreatePureBinaryOperationCompiler((compiler, left, right) => compiler._builder.CreateSRem(left, right, "modulus"));
            _functionalNodeCompilers["Increment"] = CreatePureUnaryOperationCompiler((compiler, value) => compiler._builder.CreateAdd(value, 1.AsLLVMValue(), "increment"));
            _functionalNodeCompilers["And"] = CreatePureBinaryOperationCompiler((compiler, left, right) => compiler._builder.CreateAnd(left, right, "and"));
            _functionalNodeCompilers["Or"] = CreatePureBinaryOperationCompiler((compiler, left, right) => compiler._builder.CreateOr(left, right, "or"));
            _functionalNodeCompilers["Xor"] = CreatePureBinaryOperationCompiler((compiler, left, right) => compiler._builder.CreateXor(left, right, "xor"));
            _functionalNodeCompilers["Not"] = CreatePureUnaryOperationCompiler((compiler, value) => compiler._builder.CreateNot(value, "not"));

            _functionalNodeCompilers["AccumulateAdd"] = CreateMutatingBinaryOperationCompiler((compiler, left, right) => compiler._builder.CreateAdd(left, right, "add"));
            _functionalNodeCompilers["AccumulateSubtract"] = CreateMutatingBinaryOperationCompiler((compiler, left, right) => compiler._builder.CreateSub(left, right, "subtract"));
            _functionalNodeCompilers["AccumulateMultiply"] = CreateMutatingBinaryOperationCompiler((compiler, left, right) => compiler._builder.CreateMul(left, right, "multiply"));
            _functionalNodeCompilers["AccumulateDivide"] = CreateMutatingBinaryOperationCompiler((compiler, left, right) => compiler._builder.CreateSDiv(left, right, "divide"));
            _functionalNodeCompilers["AccumulateIncrement"] = CreateMutatingUnaryOperationCompiler((compiler, value) => compiler._builder.CreateAdd(value, 1.AsLLVMValue(), "increment"));
            _functionalNodeCompilers["AccumulateAnd"] = CreateMutatingBinaryOperationCompiler((compiler, left, right) => compiler._builder.CreateAnd(left, right, "and"));
            _functionalNodeCompilers["AccumulateOr"] = CreateMutatingBinaryOperationCompiler((compiler, left, right) => compiler._builder.CreateOr(left, right, "or"));
            _functionalNodeCompilers["AccumulateXor"] = CreateMutatingBinaryOperationCompiler((compiler, left, right) => compiler._builder.CreateXor(left, right, "xor"));
            _functionalNodeCompilers["AccumulateNot"] = CreateMutatingUnaryOperationCompiler((compiler, value) => compiler._builder.CreateNot(value, "not"));

            _functionalNodeCompilers["Equal"] = CreatePureBinaryOperationCompiler((compiler, left, right) => compiler._builder.CreateICmp(LLVMIntPredicate.LLVMIntEQ, left, right, "eq"));
            _functionalNodeCompilers["NotEqual"] = CreatePureBinaryOperationCompiler((compiler, left, right) => compiler._builder.CreateICmp(LLVMIntPredicate.LLVMIntNE, left, right, "ne"));
            _functionalNodeCompilers["LessThan"] = CreatePureBinaryOperationCompiler((compiler, left, right) => compiler._builder.CreateICmp(LLVMIntPredicate.LLVMIntSLT, left, right, "lt"));
            _functionalNodeCompilers["LessEqual"] = CreatePureBinaryOperationCompiler((compiler, left, right) => compiler._builder.CreateICmp(LLVMIntPredicate.LLVMIntSLE, left, right, "le"));
            _functionalNodeCompilers["GreaterThan"] = CreatePureBinaryOperationCompiler((compiler, left, right) => compiler._builder.CreateICmp(LLVMIntPredicate.LLVMIntSGT, left, right, "gt"));
            _functionalNodeCompilers["GreaterEqual"] = CreatePureBinaryOperationCompiler((compiler, left, right) => compiler._builder.CreateICmp(LLVMIntPredicate.LLVMIntSGE, left, right, "ge"));

            _functionalNodeCompilers["Some"] = CompileSomeConstructor;
            _functionalNodeCompilers["None"] = CompileNoneConstructor;

            _functionalNodeCompilers["Range"] = CreateImportedCommonFunctionCompiler(CommonModules.CreateRangeIteratorName);

            _functionalNodeCompilers["StringFromSlice"] = CreateImportedCommonFunctionCompiler(CommonModules.StringFromSliceName);
            _functionalNodeCompilers["StringToSlice"] = CreateImportedCommonFunctionCompiler(CommonModules.StringToSliceName);
            _functionalNodeCompilers["StringAppend"] = CreateImportedCommonFunctionCompiler(CommonModules.StringAppendName);
            _functionalNodeCompilers["StringConcat"] = CreateImportedCommonFunctionCompiler(CommonModules.StringConcatName);

            _functionalNodeCompilers["VectorCreate"] = CompileVectorCreate;
            _functionalNodeCompilers["VectorInitialize"] = CompileVectorInitialize;
            _functionalNodeCompilers["VectorToSlice"] = CompileVectorToSlice;
            _functionalNodeCompilers["VectorAppend"] = CompileVectorAppend;
            _functionalNodeCompilers["VectorInsert"] = CompileNothing;
            _functionalNodeCompilers["VectorRemoveLast"] = CompileVectorRemoveLast;
            _functionalNodeCompilers["SliceIndex"] = CompileSliceIndex;

            _functionalNodeCompilers["CreateLockingCell"] = CompileNothing;

            _functionalNodeCompilers["SharedCreate"] = CompileSharedCreate;
            _functionalNodeCompilers["SharedGetValue"] = CompileSharedGetValue;

            _functionalNodeCompilers["OpenFileHandle"] = CreateImportedCommonFunctionCompiler(CommonModules.OpenFileHandleName);
            _functionalNodeCompilers["ReadLineFromFileHandle"] = CreateImportedCommonFunctionCompiler(CommonModules.ReadLineFromFileHandleName);
            _functionalNodeCompilers["WriteStringToFileHandle"] = CreateImportedCommonFunctionCompiler(CommonModules.WriteStringToFileHandleName);
        }

        #region Functional node compilers

        private static void CompileNothing(FunctionCompiler compiler, FunctionalNode noopNode)
        {
        }

        private static void CompileInspect(FunctionCompiler compiler, FunctionalNode inspectNode)
        {
            VariableReference input = inspectNode.InputTerminals[0].GetTrueVariable();

            // define global data in module for inspected value
            LLVMTypeRef globalType = input.Type.GetReferentType().AsLLVMType();
            string globalName = $"inspect_{inspectNode.UniqueId}";
            LLVMValueRef globalAddress = compiler.Module.AddGlobal(globalType, globalName);
            // Setting an initializer is necessary to distinguish this from an externally-defined global
            globalAddress.SetInitializer(LLVMSharp.LLVM.ConstNull(globalType));

            // load the input dereference value and store it in the global
            compiler._builder.CreateStore(compiler._variableValues[input].GetDeferencedValue(compiler._builder), globalAddress);
        }

        private static void CompileOutput(FunctionCompiler compiler, FunctionalNode outputNode)
        {
            ValueSource inputValueSource = compiler.GetTerminalValueSource(outputNode.InputTerminals[0]);
            VariableReference input = outputNode.InputTerminals[0].GetTrueVariable();
            NIType referentType = input.Type.GetReferentType();
            if (referentType.IsBoolean())
            {
                LLVMValueRef value = inputValueSource.GetDeferencedValue(compiler._builder);
                compiler._builder.CreateCall(compiler._commonExternalFunctions.OutputBoolFunction, new LLVMValueRef[] { value }, string.Empty);
                return;
            }
            if (referentType.IsInteger())
            {
                LLVMValueRef outputFunction;
                switch (referentType.GetKind())
                {
                    case NITypeKind.Int8:
                        outputFunction = compiler._commonExternalFunctions.OutputInt8Function;
                        break;
                    case NITypeKind.UInt8:
                        outputFunction = compiler._commonExternalFunctions.OutputUInt8Function;
                        break;
                    case NITypeKind.Int16:
                        outputFunction = compiler._commonExternalFunctions.OutputInt16Function;
                        break;
                    case NITypeKind.UInt16:
                        outputFunction = compiler._commonExternalFunctions.OutputUInt16Function;
                        break;
                    case NITypeKind.Int32:
                        outputFunction = compiler._commonExternalFunctions.OutputInt32Function;
                        break;
                    case NITypeKind.UInt32:
                        outputFunction = compiler._commonExternalFunctions.OutputUInt32Function;
                        break;
                    case NITypeKind.Int64:
                        outputFunction = compiler._commonExternalFunctions.OutputInt64Function;
                        break;
                    case NITypeKind.UInt64:
                        outputFunction = compiler._commonExternalFunctions.OutputUInt64Function;
                        break;
                    default:
                        throw new NotImplementedException($"Don't know how to display type {referentType} yet.");
                }
                LLVMValueRef value = inputValueSource.GetDeferencedValue(compiler._builder);
                compiler._builder.CreateCall(outputFunction, new LLVMValueRef[] { value }, string.Empty);
                return;
            }
            if (referentType.IsString())
            {
                // TODO: this should go away once auto-borrowing into string slices works
                // call output_string with string pointer and size
                LLVMValueRef stringPtr = inputValueSource.GetValue(compiler._builder),
                    stringSlice = compiler._builder.CreateCall(
                        compiler.GetImportedCommonFunction(CommonModules.StringToSliceRetName),
                        new LLVMValueRef[] { stringPtr },
                        "stringSlice");
                compiler._builder.CreateCall(
                    compiler.GetImportedCommonFunction(CommonModules.OutputStringSliceName),
                    new LLVMValueRef[] { stringSlice },
                    string.Empty);
                return;
            }
            if (referentType == DataTypes.StringSliceType)
            {
                compiler.CreateCallForFunctionalNode(compiler.GetImportedCommonFunction(CommonModules.OutputStringSliceName), outputNode, outputNode.Signature);
                return;
            }
            else
            {
                throw new NotImplementedException($"Don't know how to display type {referentType} yet.");
            }
        }

        private static void CompileAssign(FunctionCompiler compiler, FunctionalNode assignNode)
        {
            VariableReference assigneeVariable = assignNode.InputTerminals[0].GetTrueVariable();
            ValueSource assigneeSource = compiler.GetTerminalValueSource(assignNode.InputTerminals[0]),
                newValueSource = compiler.GetTerminalValueSource(assignNode.InputTerminals[1]);
            NIType assigneeType = assigneeVariable.Type.GetReferentType();
            compiler.CreateDropCallIfDropFunctionExists(compiler._builder, assigneeType, b => assigneeSource.GetValue(b));
            assigneeSource.UpdateDereferencedValue(compiler._builder, newValueSource.GetValue(compiler._builder));
        }

        private static void CompileExchange(FunctionCompiler compiler, FunctionalNode exchangeNode)
        {
            ValueSource valueSource1 = compiler.GetTerminalValueSource(exchangeNode.InputTerminals[0]),
                valueSource2 = compiler.GetTerminalValueSource(exchangeNode.InputTerminals[1]);
            LLVMValueRef valueRef1 = valueSource1.GetDeferencedValue(compiler._builder),
                valueRef2 = valueSource2.GetDeferencedValue(compiler._builder);
            valueSource1.UpdateDereferencedValue(compiler._builder, valueRef2);
            valueSource2.UpdateDereferencedValue(compiler._builder, valueRef1);
        }

        private static void CompileCreateCopy(FunctionCompiler compiler, FunctionalNode createCopyNode)
        {
            var copyValueSource = (LocalAllocationValueSource)compiler.GetTerminalValueSource(createCopyNode.OutputTerminals[1]);
            NIType valueType = copyValueSource.AllocationNIType;
            if (valueType.WireTypeMayFork())
            {
                ValueSource copyFromSource = compiler.GetTerminalValueSource(createCopyNode.InputTerminals[0]);
                copyValueSource.UpdateValue(compiler._builder, copyFromSource.GetDeferencedValue(compiler._builder));
                return;
            }

            LLVMValueRef cloneFunction;
            if (compiler.TryGetCloneFunction(valueType, out cloneFunction))
            {
                compiler.CreateCallForFunctionalNode(cloneFunction, createCopyNode);
                return;
            }

            throw new NotSupportedException("Don't know how to compile CreateCopy for type " + valueType);
        }

        private bool TryGetCloneFunction(NIType valueType, out LLVMValueRef cloneFunction)
        {
            cloneFunction = default(LLVMValueRef);
            NIType innerType;
            if (valueType == PFTypes.String)
            {
                cloneFunction = GetImportedCommonFunction(CommonModules.StringCloneName);
                return true;
            }
            if (valueType.TryDestructureSharedType(out innerType))
            {
                string specializedName = MonomorphizeFunctionName("shared_clone", innerType.ToEnumerable());
                cloneFunction = GetSpecializedFunction(
                    specializedName,
                    () => CreateSharedCloneFunction(Module, specializedName, innerType.AsLLVMType()));
                return true;
            }
            if (valueType.TryDestructureVectorType(out innerType))
            {
                string specializedName = MonomorphizeFunctionName("vector_clone", innerType.ToEnumerable());
                cloneFunction = GetSpecializedFunction(
                    specializedName,
                    () => CreateVectorCloneFunction(this, specializedName, innerType));
                return true;
            }

            if (valueType.TypeHasCloneTrait())
            {
                throw new NotSupportedException("Clone function not found for type: " + valueType);
            }
            return false;
        }

        private static void CompileSelectReference(FunctionCompiler compiler, FunctionalNode selectReferenceNode)
        {
            ValueSource selectorSource = compiler.GetTerminalValueSource(selectReferenceNode.InputTerminals[0]),
                trueValueSource = compiler.GetTerminalValueSource(selectReferenceNode.InputTerminals[1]),
                falseValueSource = compiler.GetTerminalValueSource(selectReferenceNode.InputTerminals[2]);
            LLVMValueRef selectedValue = compiler._builder.CreateSelect(
                selectorSource.GetDeferencedValue(compiler._builder),
                trueValueSource.GetValue(compiler._builder),
                falseValueSource.GetValue(compiler._builder),
                "select");
            ValueSource selectedValueSource = compiler.GetTerminalValueSource(selectReferenceNode.OutputTerminals[1]);
            selectedValueSource.UpdateValue(compiler._builder, selectedValue);
        }

        private static Action<FunctionCompiler, FunctionalNode> CreatePureUnaryOperationCompiler(Func<FunctionCompiler, LLVMValueRef, LLVMValueRef> generateOperation)
        {
            return (_, __) => CompileUnaryOperation(_, __, generateOperation, false);
        }

        private static Action<FunctionCompiler, FunctionalNode> CreateMutatingUnaryOperationCompiler(Func<FunctionCompiler, LLVMValueRef, LLVMValueRef> generateOperation)
        {
            return (_, __) => CompileUnaryOperation(_, __, generateOperation, true);
        }

        private static void CompileUnaryOperation(
            FunctionCompiler compiler, 
            FunctionalNode operationNode,
            Func<FunctionCompiler, LLVMValueRef, LLVMValueRef> generateOperation,
            bool mutating)
        {
            ValueSource inputValueSource = compiler.GetTerminalValueSource(operationNode.InputTerminals[0]);
            LLVMValueRef inputValue = inputValueSource.GetDeferencedValue(compiler._builder);
            LLVMValueRef resultValue = generateOperation(compiler, inputValue);

            if (!mutating)
            {
                ValueSource outputValueSource = compiler.GetTerminalValueSource(operationNode.OutputTerminals[1]);
                outputValueSource.UpdateValue(compiler._builder, resultValue);
            }
            else
            {
                inputValueSource.UpdateDereferencedValue(compiler._builder, resultValue);
            }
        }

        private static Action<FunctionCompiler, FunctionalNode> CreatePureBinaryOperationCompiler(Func<FunctionCompiler, LLVMValueRef, LLVMValueRef, LLVMValueRef> generateOperation)
        {
            return (_, __) => CompileBinaryOperation(_, __, generateOperation, false);
        }

        private static Action<FunctionCompiler, FunctionalNode> CreateMutatingBinaryOperationCompiler(Func<FunctionCompiler, LLVMValueRef, LLVMValueRef, LLVMValueRef> generateOperation)
        {
            return (_, __) => CompileBinaryOperation(_, __, generateOperation, true);
        }

        private static void CompileBinaryOperation(
            FunctionCompiler compiler,
            FunctionalNode operationNode,
            Func<FunctionCompiler, LLVMValueRef, LLVMValueRef, LLVMValueRef> generateOperation,
            bool mutating)
        {
            ValueSource leftValueSource = compiler.GetTerminalValueSource(operationNode.InputTerminals[0]),
                rightValueSource = compiler.GetTerminalValueSource(operationNode.InputTerminals[1]);
            LLVMValueRef leftValue = leftValueSource.GetDeferencedValue(compiler._builder),
                rightValue = rightValueSource.GetDeferencedValue(compiler._builder);
            LLVMValueRef resultValue = generateOperation(compiler, leftValue, rightValue);
            if (!mutating)
            {
                ValueSource outputValueSource = compiler.GetTerminalValueSource(operationNode.OutputTerminals[2]);
                outputValueSource.UpdateValue(compiler._builder, resultValue);
            }
            else
            {
                leftValueSource.UpdateDereferencedValue(compiler._builder, resultValue);
            }
        }

        private static void CompileSomeConstructor(FunctionCompiler compiler, FunctionalNode someConstructorNode)
        {
            ValueSource inputSource = compiler.GetTerminalValueSource(someConstructorNode.InputTerminals[0]),
                outputSource = compiler.GetTerminalValueSource(someConstructorNode.OutputTerminals[0]);
            // CreateSomeValueStruct creates a const struct, which isn't allowed since
            // inputSource.GetValue isn't always a constant.

            LLVMValueRef[] someFieldValues = new[]
            {
                true.AsLLVMValue(),
                inputSource.GetValue(compiler._builder)
            };
            ((LocalAllocationValueSource)outputSource).UpdateStructValue(compiler._builder, someFieldValues);
        }

        private static void CompileNoneConstructor(FunctionCompiler compiler, FunctionalNode noneConstructorNode)
        {
            ValueSource outputSource = compiler.GetTerminalValueSource(noneConstructorNode.OutputTerminals[0]);
            LLVMTypeRef outputType = noneConstructorNode
                .OutputTerminals[0].GetTrueVariable()
                .Type.AsLLVMType();
            outputSource.UpdateValue(compiler._builder, LLVMSharp.LLVM.ConstNull(outputType));
        }

        private static LLVMValueRef CreateOptionDropFunction(Module module, FunctionCompiler compiler, string functionName, NIType innerType)
        {
            LLVMTypeRef optionDropFunctionType = LLVMTypeRef.FunctionType(
                LLVMSharp.LLVM.VoidType(),
                new LLVMTypeRef[]
                {
                    LLVMTypeRef.PointerType(innerType.AsLLVMType().CreateLLVMOptionType(), 0u)
                },
                false);

            LLVMValueRef optionDropFunction = module.AddFunction(functionName, optionDropFunctionType);
            LLVMBasicBlockRef entryBlock = optionDropFunction.AppendBasicBlock("entry"),
                isSomeBlock = optionDropFunction.AppendBasicBlock("isSome"),
                endBlock = optionDropFunction.AppendBasicBlock("end");
            var builder = new IRBuilder();

            builder.PositionBuilderAtEnd(entryBlock);
            LLVMValueRef optionPtr = optionDropFunction.GetParam(0u),
                isSomePtr = builder.CreateStructGEP(optionPtr, 0u, "isSomePtr"),
                isSome = builder.CreateLoad(isSomePtr, "isSome");
            builder.CreateCondBr(isSome, isSomeBlock, endBlock);

            builder.PositionBuilderAtEnd(isSomeBlock);
            compiler.CreateDropCallIfDropFunctionExists(builder, innerType, b => b.CreateStructGEP(optionPtr, 1u, "innerValuePtr"));
            builder.CreateBr(endBlock);

            builder.PositionBuilderAtEnd(endBlock);
            builder.CreateRetVoid();
            return optionDropFunction;
        }

        private static Action<FunctionCompiler, FunctionalNode> CreateImportedCommonFunctionCompiler(string functionName)
        {
            return (compiler, functionalNode) =>
                compiler.CreateCallForFunctionalNode(compiler.GetImportedCommonFunction(functionName), functionalNode, functionalNode.Signature);
        }

        private static void CompileSliceIndex(FunctionCompiler compiler, FunctionalNode sliceIndexNode)
        {
            var sliceReferenceSource = (LocalAllocationValueSource)compiler.GetTerminalValueSource(sliceIndexNode.InputTerminals[1]);
            NIType elementType;
            sliceReferenceSource.AllocationNIType.GetReferentType().TryDestructureSliceType(out elementType);
            string specializedName = MonomorphizeFunctionName("slice_index", elementType.ToEnumerable());
            LLVMValueRef sliceIndexFunction = compiler.GetSpecializedFunction(
                specializedName,
                () => CreateSliceIndexFunction(compiler.Module, specializedName, elementType.AsLLVMType()));
            compiler.CreateCallForFunctionalNode(sliceIndexFunction, sliceIndexNode);
        }

        private static LLVMValueRef CreateSliceIndexFunction(Module module, string functionName, LLVMTypeRef elementType)
        {
            LLVMTypeRef sliceReferenceType = elementType.CreateLLVMSliceReferenceType(),
                elementPtrOptionType = LLVMTypeRef.PointerType(elementType, 0u).CreateLLVMOptionType();
            LLVMTypeRef sliceIndexFunctionType = LLVMTypeRef.FunctionType(
                LLVMSharp.LLVM.VoidType(),
                new LLVMTypeRef[]
                {
                    LLVMTypeRef.PointerType(LLVMTypeRef.Int32Type(), 0u),
                    sliceReferenceType,
                    LLVMTypeRef.PointerType(elementPtrOptionType, 0u)
                },
                false);

            LLVMValueRef sliceIndexFunction = module.AddFunction(functionName, sliceIndexFunctionType);
            LLVMBasicBlockRef entryBlock = sliceIndexFunction.AppendBasicBlock("entry"),
                validIndexBlock = sliceIndexFunction.AppendBasicBlock("validIndex"),
                invalidIndexBlock = sliceIndexFunction.AppendBasicBlock("invalidIndex");
            var builder = new IRBuilder();

            builder.PositionBuilderAtEnd(entryBlock);
            LLVMValueRef indexPtr = sliceIndexFunction.GetParam(0u),
                index = builder.CreateLoad(indexPtr, "index"),
                sliceRef = sliceIndexFunction.GetParam(1u),
                sliceLength = builder.CreateExtractValue(sliceRef, 1u, "sliceLength"),
                indexLessThanSliceLength = builder.CreateICmp(LLVMIntPredicate.LLVMIntSLT, index, sliceLength, "indexLTSliceLength"),
                indexNonNegative = builder.CreateICmp(LLVMIntPredicate.LLVMIntSGE, index, 0.AsLLVMValue(), "indexNonNegative"),
                indexInBounds = builder.CreateAnd(indexLessThanSliceLength, indexNonNegative, "indexInBounds"),
                elementPtrOptionPtr = sliceIndexFunction.GetParam(2u);
            builder.CreateCondBr(indexInBounds, validIndexBlock, invalidIndexBlock);

            builder.PositionBuilderAtEnd(validIndexBlock);
            LLVMValueRef sliceBufferPtr = builder.CreateExtractValue(sliceRef, 0u, "sliceBufferPtr"),
                elementPtr = builder.CreateGEP(sliceBufferPtr, new LLVMValueRef[] { index }, "elementPtr"),
                someElementPtr = builder.BuildOptionValue(elementPtrOptionType, elementPtr);
            builder.CreateStore(someElementPtr, elementPtrOptionPtr);
            builder.CreateRetVoid();

            builder.PositionBuilderAtEnd(invalidIndexBlock);
            LLVMValueRef noneElementPtr = builder.BuildOptionValue(elementPtrOptionType, null);
            builder.CreateStore(noneElementPtr, elementPtrOptionPtr);
            builder.CreateRetVoid();

            return sliceIndexFunction;
        }

        private static string MonomorphizeFunctionName(string functionName, IEnumerable<NIType> typeArguments)
        {
            var nameBuilder = new StringBuilder(functionName);
            foreach (NIType typeArgument in typeArguments)
            {
                nameBuilder.Append("_");
                nameBuilder.Append(StringifyType(typeArgument));
            }
            return nameBuilder.ToString();
        }

        private static string StringifyType(NIType type)
        {
            switch (type.GetKind())
            {
                case NITypeKind.UInt8:
                    return "u8";
                case NITypeKind.Int8:
                    return "i8";
                case NITypeKind.UInt16:
                    return "u16";
                case NITypeKind.Int16:
                    return "i16";
                case NITypeKind.UInt32:
                    return "u32";
                case NITypeKind.Int32:
                    return "i32";
                case NITypeKind.UInt64:
                    return "u64";
                case NITypeKind.Int64:
                    return "i64";
                case NITypeKind.Boolean:
                    return "bool";
                case NITypeKind.String:
                    return "string";
                default:
                {
                    if (type.IsRebarReferenceType())
                    {
                        NIType referentType = type.GetReferentType();
                        if (referentType == DataTypes.StringSliceType)
                        {
                            return "str";
                        }
                        NIType sliceElementType;
                        if (referentType.TryDestructureSliceType(out sliceElementType))
                        {
                            return $"slice[{StringifyType(sliceElementType)}]";
                        }
                        return $"ref[{StringifyType(sliceElementType)}]";
                    }
                    if (type == DataTypes.FileHandleType)
                    {
                        return "filehandle";
                    }
                    if (type == DataTypes.FakeDropType)
                    {
                        return "fakedrop";
                    }
                    NIType innerType;
                    if (type.TryDestructureOptionType(out innerType))
                    {
                        return $"option[{StringifyType(innerType)}]";
                    }
                    if (type.TryDestructureVectorType(out innerType))
                    {
                        return $"vec[{StringifyType(innerType)}]";
                    }
                    if (type.TryDestructureSharedType(out innerType))
                    {
                        return $"shared[{StringifyType(innerType)}]";
                    }
                    if (type == DataTypes.RangeIteratorType)
                    {
                        return "rangeiterator";
                    }
                    throw new NotSupportedException("Unsupported type: " + type);
                }
        }
    }

    #endregion

        private readonly IRBuilder _builder;
        private readonly LLVMValueRef _topLevelFunction;
        private readonly Dictionary<VariableReference, ValueSource> _variableValues;
        private readonly CommonExternalFunctions _commonExternalFunctions;
        private readonly Dictionary<string, LLVMValueRef> _importedFunctions = new Dictionary<string, LLVMValueRef>();
        private readonly Dictionary<DataItem, uint> _dataItemParameterIndices = new Dictionary<DataItem, uint>();

        public FunctionCompiler(Module module, string functionName, DataItem[] parameterDataItems, Dictionary<VariableReference, ValueSource> variableValues)
        {
            Module = module;
            _variableValues = variableValues;

            var parameterLLVMTypes = new List<LLVMTypeRef>();
            foreach (var dataItem in parameterDataItems.OrderBy(d => d.ConnectorPaneIndex))
            {
                if (dataItem.ConnectorPaneInputPassingRule == NIParameterPassingRule.Required
                    && dataItem.ConnectorPaneOutputPassingRule == NIParameterPassingRule.NotAllowed)
                {
                    parameterLLVMTypes.Add(dataItem.DataType.AsLLVMType());
                }
                else if (dataItem.ConnectorPaneInputPassingRule == NIParameterPassingRule.NotAllowed
                    && dataItem.ConnectorPaneOutputPassingRule == NIParameterPassingRule.Optional)
                {
                    parameterLLVMTypes.Add(LLVMTypeRef.PointerType(dataItem.DataType.AsLLVMType(), 0u));
                }
                else
                {
                    throw new NotImplementedException("Can only handle in and out parameters");
                }
            }

            LLVMTypeRef functionType = LLVMSharp.LLVM.FunctionType(LLVMSharp.LLVM.VoidType(), parameterLLVMTypes.ToArray(), false);
            _topLevelFunction = Module.AddFunction(functionName, functionType);
            LLVMBasicBlockRef entryBlock = _topLevelFunction.AppendBasicBlock("entry");
            _builder = new IRBuilder();
            _builder.PositionBuilderAtEnd(entryBlock);

            _commonExternalFunctions = new CommonExternalFunctions(module);
            InitializeLocalAllocations();
        }

        public Module Module { get; }

        private DfirRoot TargetDfir { get; set; }

        private void InitializeLocalAllocations()
        {
            foreach (var localAllocation in _variableValues.Values.OfType<LocalAllocationValueSource>())
            {
                LLVMTypeRef allocationType = localAllocation.AllocationNIType.AsLLVMType();
                LLVMValueRef allocationPointer = _builder.CreateAlloca(allocationType, localAllocation.AllocationName);
                localAllocation.AllocationPointer = allocationPointer;
            }
        }

        private LLVMValueRef GetImportedCommonFunction(string functionName)
        {
            LLVMValueRef function;
            if (!_importedFunctions.TryGetValue(functionName, out function))
            {
                function = Module.AddFunction(functionName, CommonModules.CommonModuleSignatures[functionName]);
                function.SetLinkage(LLVMLinkage.LLVMExternalLinkage);
                _importedFunctions[functionName] = function;
            }
            return function;
        }

        private LLVMValueRef GetImportedFunction(MethodCallNode methodCallNode)
        {
            string targetFunctionName = FunctionCompileHandler.FunctionLLVMName(new SpecAndQName(TargetDfir.BuildSpec, methodCallNode.TargetName));
            LLVMValueRef function;
            if (!_importedFunctions.TryGetValue(targetFunctionName, out function))
            {
                LLVMTypeRef[] parameterTypes = methodCallNode.Signature.GetParameters().Select(TranslateParameterType).ToArray();
                LLVMTypeRef targetFunctionType = LLVMSharp.LLVM.FunctionType(LLVMSharp.LLVM.VoidType(), parameterTypes, false);
                function = Module.AddFunction(targetFunctionName, targetFunctionType);
                _importedFunctions[targetFunctionName] = function;
            }
            return function;
        }

        private LLVMValueRef GetSpecializedFunction(string specializedFunctionName, Func<LLVMValueRef> createFunction)
        {
            LLVMValueRef function;
            if (!_importedFunctions.TryGetValue(specializedFunctionName, out function))
            {
                function = createFunction();
                _importedFunctions[specializedFunctionName] = function;
            }
            return function;
        }

        private void CreateCallForFunctionalNode(LLVMValueRef function, FunctionalNode functionalNode)
        {
            CreateCallForFunctionalNode(function, functionalNode, functionalNode.Signature);
        }

        private void CreateCallForFunctionalNode(LLVMValueRef function, Node node, NIType nodeFunctionSignature)
        {
            var arguments = new List<LLVMValueRef>();
            foreach (Terminal inputTerminal in node.InputTerminals)
            {
                arguments.Add(GetTerminalValueSource(inputTerminal).GetValue(_builder));
            }
            Signature nodeSignature = Signatures.GetSignatureForNIType(nodeFunctionSignature);
            foreach (var outputPair in node.OutputTerminals.Zip(nodeSignature.Outputs))
            {
                if (outputPair.Value.IsPassthrough)
                {
                    continue;
                }
                var allocationSource = (LocalAllocationValueSource)GetTerminalValueSource(outputPair.Key);
                arguments.Add(allocationSource.AllocationPointer);
            }
            _builder.CreateCall(function, arguments.ToArray(), string.Empty);
        }

        #region VisitorTransformBase overrides

        protected override void VisitDfirRoot(DfirRoot dfirRoot)
        {
            TargetDfir = dfirRoot;
            uint parameterIndex = 0;
            foreach (DataItem dataItem in dfirRoot.DataItems.OrderBy(d => d.ConnectorPaneIndex))
            {
                _dataItemParameterIndices[dataItem] = parameterIndex;
                ++parameterIndex;
            }

            base.VisitDfirRoot(dfirRoot);
        }

        protected override void PostVisitDiagram(Diagram diagram)
        {
            base.PostVisitDiagram(diagram);
            if (diagram == diagram.DfirRoot.BlockDiagram)
            {
                _builder.CreateRetVoid();
            }
        }

        protected override void VisitBorderNode(NationalInstruments.Dfir.BorderNode borderNode)
        {
            this.VisitRebarNode(borderNode);
        }

        protected override void VisitNode(Node node)
        {
            this.VisitRebarNode(node);
        }

        protected override void VisitWire(Wire wire)
        {
            if (wire.SinkTerminals.HasMoreThan(1))
            {
                VariableReference sourceVariable = wire.SourceTerminal.GetTrueVariable();
                if (!(_variableValues[sourceVariable] is LocalAllocationValueSource))
                {
                    // if the source is not a local allocation, then presumably the sinks aren't either and
                    // there's nothing to copy
                    // TODO: this may change later if it becomes possible to have different sink branches 
                    // have different mutability settings
                    return;
                }
                VariableReference[] sinkVariables = wire.SinkTerminals.Skip(1).Select(VariableExtensions.GetTrueVariable).ToArray();
                NIType variableType = sourceVariable.Type;
                foreach (var sinkVariable in sinkVariables)
                {
                    CopyValueToValue(sinkVariable, sourceVariable, variableType);
                }
            }
        }

        protected override void VisitStructure(Structure structure, StructureTraversalPoint traversalPoint, Diagram nestedDiagram)
        {
            base.VisitStructure(structure, traversalPoint, nestedDiagram);

            if (traversalPoint == StructureTraversalPoint.BeforeLeftBorderNodes)
            {
                foreach (Tunnel tunnel in structure.BorderNodes.OfType<Tunnel>())
                {
                    _tunnelInfos[tunnel] = new TunnelInfo();
                }
            }

            this.VisitRebarStructure(structure, traversalPoint, nestedDiagram);
        }

        #endregion

        #region Private helpers

        private ValueSource GetTerminalValueSource(Terminal terminal)
        {
            return _variableValues[terminal.GetTrueVariable()];
        }

        private void CopyValueToValue(VariableReference destinationValue, VariableReference copyFromValue, NIType valueType)
        {
            _variableValues[destinationValue].UpdateValue(_builder, _variableValues[copyFromValue].GetValue(_builder));
        }

        private void BorrowFromVariableIntoVariable(VariableReference from, VariableReference into)
        {
            LocalAllocationValueSource intoAllocation = _variableValues[into] as LocalAllocationValueSource;
            if (intoAllocation != null)
            {
                LocalAllocationValueSource fromAllocation = (LocalAllocationValueSource)_variableValues[from];
                LLVMValueRef fromAddress = fromAllocation.AllocationPointer;
                intoAllocation.UpdateValue(_builder, fromAddress);
            }
        }

#endregion

        public bool VisitBorrowTunnel(BorrowTunnel borrowTunnel)
        {
            VariableReference output = borrowTunnel.OutputTerminals[0].GetTrueVariable();
            if (_variableValues[output] is LocalAllocationValueSource)
            {
                VariableReference input = borrowTunnel.InputTerminals[0].GetTrueVariable();
                BorrowFromVariableIntoVariable(input, output);
            }
            return true;
        }

        public bool VisitConstant(Constant constant)
        {
            ValueSource outputAllocation = GetTerminalValueSource(constant.OutputTerminal);
            if (constant.DataType.IsInteger())
            {
                LLVMValueRef constantValueRef;
                switch (constant.DataType.GetKind())
                {
                    case NITypeKind.Int8:
                        constantValueRef = ((sbyte)constant.Value).AsLLVMValue();
                        break;
                    case NITypeKind.UInt8:
                        constantValueRef = ((byte)constant.Value).AsLLVMValue();
                        break;
                    case NITypeKind.Int16:
                        constantValueRef = ((short)constant.Value).AsLLVMValue();
                        break;
                    case NITypeKind.UInt16:
                        constantValueRef = ((ushort)constant.Value).AsLLVMValue();
                        break;
                    case NITypeKind.Int32:
                        constantValueRef = ((int)constant.Value).AsLLVMValue();
                        break;
                    case NITypeKind.UInt32:
                        constantValueRef = ((uint)constant.Value).AsLLVMValue();
                        break;
                    case NITypeKind.Int64:
                        constantValueRef = ((long)constant.Value).AsLLVMValue();
                        break;
                    case NITypeKind.UInt64:
                        constantValueRef = ((ulong)constant.Value).AsLLVMValue();
                        break;
                    default:
                        throw new NotSupportedException("Unsupported numeric constant type: " + constant.DataType);
                }
                outputAllocation.UpdateValue(_builder, constantValueRef);
            }
            else if (constant.Value is bool)
            {
                LLVMValueRef constantValueRef = ((bool)constant.Value).AsLLVMValue();
                outputAllocation.UpdateValue(_builder, constantValueRef);
            }
            else if (constant.Value is string)
            {
                VariableReference output = constant.OutputTerminal.GetTrueVariable();
                if (output.Type.IsRebarReferenceType() && output.Type.GetReferentType() == DataTypes.StringSliceType)
                {
                    string stringValue = (string)constant.Value;
                    int length = Encoding.UTF8.GetByteCount(stringValue);
                    LLVMValueRef stringValueConstant = LLVMSharp.LLVM.ConstString(stringValue, (uint)length, true);
                    LLVMValueRef stringConstantPtr = Module.AddGlobal(stringValueConstant.TypeOf(), $"string{constant.UniqueId}");
                    stringConstantPtr.SetInitializer(stringValueConstant);

                    LLVMValueRef castPointer = _builder.CreateBitCast(
                        stringConstantPtr,
                        LLVMTypeRef.PointerType(LLVMTypeRef.Int8Type(), 0),
                        "ptrCast");
                    LLVMValueRef[] stringSliceFields = new LLVMValueRef[]
                    {
                        castPointer,
                        length.AsLLVMValue()
                    };
                    LLVMValueRef stringSliceValue = LLVMValueRef.ConstStruct(stringSliceFields, false);
                    outputAllocation.UpdateValue(_builder, stringSliceValue);
                }
            }
            else
            {
                throw new NotImplementedException();
            }
            return true;
        }

        public bool VisitDataAccessor(DataAccessor dataAccessor)
        {
            if (dataAccessor.Terminal.Direction == Direction.Output)
            {
                // TODO: distinguish inout from in parameters?
                LLVMValueRef parameterValue = _topLevelFunction.GetParam(_dataItemParameterIndices[dataAccessor.DataItem]);
                _variableValues[dataAccessor.Terminal.GetTrueVariable()].UpdateValue(_builder, parameterValue);
            }
            else if (dataAccessor.Terminal.Direction == Direction.Input)
            {
                // assume that the function parameter is a pointer to where we need to store the value
                LLVMValueRef value = _variableValues[dataAccessor.Terminal.GetTrueVariable()].GetValue(_builder),
                    addressParameter = _topLevelFunction.GetParam(_dataItemParameterIndices[dataAccessor.DataItem]);
                _builder.CreateStore(value, addressParameter);
            }
            return true;
        }

        public bool VisitDropNode(DropNode dropNode)
        {
            VariableReference input = dropNode.InputTerminals[0].GetTrueVariable();
            var inputAllocation = (LocalAllocationValueSource)_variableValues[input];
            CreateDropCallIfDropFunctionExists(_builder, input.Type, _ => inputAllocation.AllocationPointer);
            return true;
        }

        private bool TryGetDropFunction(NIType droppedValueType, out LLVMValueRef dropFunction)
        {
            dropFunction = default(LLVMValueRef);
            NIType innerType;
            if (droppedValueType == PFTypes.String)
            {
                dropFunction = GetImportedCommonFunction(CommonModules.DropStringName);
                return true;
            }
            if (droppedValueType == DataTypes.FileHandleType)
            {
                dropFunction = GetImportedCommonFunction(CommonModules.DropFileHandleName);
                return true;
            }
            if (droppedValueType == DataTypes.FakeDropType)
            {
                dropFunction = GetImportedCommonFunction(CommonModules.FakeDropDropName);
                return true;
            }
            if (droppedValueType.TryDestructureVectorType(out innerType))
            {
                string specializedFunctionName = MonomorphizeFunctionName("vector_drop", innerType.ToEnumerable());
                dropFunction = GetSpecializedFunction(
                    specializedFunctionName,
                    () => CreateVectorDropFunction(this, specializedFunctionName, innerType));
                return true;
            }
            if (droppedValueType.TryDestructureOptionType(out innerType) && TryGetDropFunction(innerType, out dropFunction))
            {
                string specializedFunctionName = MonomorphizeFunctionName("option_drop", innerType.ToEnumerable());
                dropFunction = GetSpecializedFunction(
                    specializedFunctionName,
                    () => CreateOptionDropFunction(Module, this, specializedFunctionName, innerType));
                return true;
            }
            if (droppedValueType.TryDestructureSharedType(out innerType))
            {
                string specializedName = MonomorphizeFunctionName("shared_drop", innerType.ToEnumerable());
                dropFunction = GetSpecializedFunction(
                    specializedName,
                    () => CreateSharedDropFunction(this, Module, specializedName, innerType));
                return true;
            }

            if (droppedValueType.TypeHasDropTrait())
            {
                throw new NotSupportedException("Drop function not found for type: " + droppedValueType);
            }
            return false;
        }

        private void CreateDropCallIfDropFunctionExists(IRBuilder builder, NIType droppedValueType, Func<IRBuilder, LLVMValueRef> getDroppedValuePtr)
        {
            LLVMValueRef dropFunction;
            if (TryGetDropFunction(droppedValueType, out dropFunction))
            {
                LLVMValueRef droppedValuePtr = getDroppedValuePtr(builder);
                builder.CreateCall(dropFunction, new LLVMValueRef[] { droppedValuePtr }, string.Empty);
            }
        }

        public bool VisitExplicitBorrowNode(ExplicitBorrowNode explicitBorrowNode)
        {
            VariableReference input = explicitBorrowNode.InputTerminals[0].GetTrueVariable(),
                output = explicitBorrowNode.OutputTerminals[0].GetTrueVariable();
            BorrowFromVariableIntoVariable(input, output);
            return true;
        }

        public bool VisitFunctionalNode(FunctionalNode functionalNode)
        {
            Action<FunctionCompiler, FunctionalNode> compiler;
            string functionName = functionalNode.Signature.GetName();
            if (_functionalNodeCompilers.TryGetValue(functionName, out compiler))
            {
                compiler(this, functionalNode);
                return true;
            }
            throw new NotImplementedException("Missing compiler for function " + functionName);
        }

        public bool VisitLockTunnel(LockTunnel lockTunnel)
        {
            throw new NotImplementedException();
        }

        public bool VisitMethodCallNode(MethodCallNode methodCallNode)
        {
            LLVMValueRef targetFunction = GetImportedFunction(methodCallNode);
            CreateCallForFunctionalNode(targetFunction, methodCallNode, methodCallNode.Signature);
            return true;
        }

        private LLVMTypeRef TranslateParameterType(NIType parameterType)
        {
            // TODO: this should probably share code with how we compute the top function LLVM type above
            bool isInput = parameterType.GetInputParameterPassingRule() != NIParameterPassingRule.NotAllowed,
                isOutput = parameterType.GetOutputParameterPassingRule() != NIParameterPassingRule.NotAllowed;
            if (isInput)
            {
                if (isOutput)
                {
                    // Don't handle inouts yet
                    throw new NotImplementedException();
                }
                return parameterType.GetDataType().AsLLVMType();
            }
            if (isOutput)
            {
                return LLVMTypeRef.PointerType(parameterType.GetDataType().AsLLVMType(), 0u);
            }
            throw new NotImplementedException("Parameter direction is wrong");
        }

        public bool VisitTerminateLifetimeNode(TerminateLifetimeNode terminateLifetimeNode)
        {
            return true;
        }

        public bool VisitTerminateLifetimeTunnel(TerminateLifetimeTunnel terminateLifetimeTunnel)
        {
            return true;
        }

        private class TunnelInfo
        {
            private readonly Dictionary<Diagram, LLVMBasicBlockRef> _tunnelDiagramFinalBasicBlocks = new Dictionary<Diagram, LLVMBasicBlockRef>();

            public void AddDiagramFinalBasicBlock(Diagram diagram, LLVMBasicBlockRef finalBasicBlock)
            {
                _tunnelDiagramFinalBasicBlocks[diagram] = finalBasicBlock;
            }

            public LLVMBasicBlockRef GetDiagramFinalBasicBlock(Diagram diagram) => _tunnelDiagramFinalBasicBlocks[diagram];
        }

        private readonly Dictionary<Tunnel, TunnelInfo> _tunnelInfos = new Dictionary<Tunnel, TunnelInfo>();

        public bool VisitTunnel(Tunnel tunnel)
        {
            if (tunnel.Terminals.HasExactly(2))
            {
                VariableReference input = tunnel.InputTerminals[0].GetTrueVariable(),
                    output = tunnel.OutputTerminals[0].GetTrueVariable();
                ValueSource inputValueSource = _variableValues[input],
                    outputValueSource = _variableValues[output];
                if (output.Type == input.Type.CreateOption())
                {
                    LLVMValueRef[] someFieldValues = new[]
                    {
                        true.AsLLVMValue(),
                        inputValueSource.GetValue(_builder)
                    };
                    ((LocalAllocationValueSource)outputValueSource).UpdateStructValue(_builder, someFieldValues);
                    return true;
                }

                if (inputValueSource != outputValueSource)
                {
                    // For now assume that the allocator will always make the input and output the same ValueSource.
                    throw new NotImplementedException();
                }
            }
            else
            {
                if (tunnel.InputTerminals.HasMoreThan(1))
                {
                    var inputVariables = tunnel.InputTerminals.Select(VariableExtensions.GetTrueVariable);
                    var inputValuePtrs = new List<LLVMValueRef>();
                    var inputBasicBlocks = new List<LLVMBasicBlockRef>();
                    TunnelInfo tunnelInfo = _tunnelInfos[tunnel];
                    foreach (var inputTerminal in tunnel.InputTerminals)
                    {
                        var inputAllocation = (LocalAllocationValueSource)GetTerminalValueSource(inputTerminal);
                        inputValuePtrs.Add(inputAllocation.AllocationPointer);
                        LLVMBasicBlockRef inputBasicBlock = tunnelInfo.GetDiagramFinalBasicBlock(inputTerminal.ParentDiagram);
                        inputBasicBlocks.Add(inputBasicBlock);
                    }

                    var outputAllocation = (LocalAllocationValueSource)GetTerminalValueSource(tunnel.OutputTerminals[0]);
                    LLVMValueRef tunnelValuePtr = _builder.CreatePhi(outputAllocation.AllocationPointer.TypeOf(), "tunnelValuePtr");
                    tunnelValuePtr.AddIncoming(inputValuePtrs.ToArray(), inputBasicBlocks.ToArray(), (uint)inputValuePtrs.Count);
                    LLVMValueRef tunnelValue = _builder.CreateLoad(tunnelValuePtr, nameof(tunnelValue));
                    outputAllocation.UpdateValue(_builder, tunnelValue);
                }
                else
                {
                    throw new NotImplementedException();
                }
            }
            return true;
        }

#region Frame

        private struct FrameData
        {
            public FrameData(Frame frame, FunctionCompiler functionCompiler)
            {
                InteriorBlock = functionCompiler._topLevelFunction.AppendBasicBlock($"frame{frame.UniqueId}_interior");
                UnwrapFailedBlock = frame.DoesStructureExecuteConditionally()
                    ? functionCompiler._topLevelFunction.AppendBasicBlock($"frame{frame.UniqueId}_unwrapFailed")
                    : default(LLVMBasicBlockRef);
                EndBlock = functionCompiler._topLevelFunction.AppendBasicBlock($"frame{frame.UniqueId}_end");
            }

            public LLVMBasicBlockRef InteriorBlock { get; }

            public LLVMBasicBlockRef UnwrapFailedBlock { get; }

            public LLVMBasicBlockRef EndBlock { get; }
        }

        private readonly Dictionary<Frame, FrameData> _frameData = new Dictionary<Frame, FrameData>();

        public bool VisitFrame(Frame frame, StructureTraversalPoint traversalPoint)
        {
            switch (traversalPoint)
            {
                case StructureTraversalPoint.BeforeLeftBorderNodes:
                    VisitFrameBeforeLeftBorderNodes(frame);
                    break;
                case StructureTraversalPoint.AfterLeftBorderNodesAndBeforeDiagram:
                    VisitFrameAfterLeftBorderNodes(frame);
                    break;
                case StructureTraversalPoint.AfterRightBorderNodes:
                    VisitFrameAfterRightBorderNodes(frame);
                    break;
            }
            return true;
        }

        private void VisitFrameBeforeLeftBorderNodes(Frame frame)
        {
            _frameData[frame] = new FrameData(frame, this);
        }

        private void VisitFrameAfterLeftBorderNodes(Frame frame)
        {
            LLVMBasicBlockRef interiorBlock = _frameData[frame].InteriorBlock;
            _builder.CreateBr(interiorBlock);
            _builder.PositionBuilderAtEnd(interiorBlock);
        }

        private void VisitFrameAfterRightBorderNodes(Frame frame)
        {
            FrameData frameData = _frameData[frame];
            _builder.CreateBr(frameData.EndBlock);
            if (frame.DoesStructureExecuteConditionally())
            {
                _builder.PositionBuilderAtEnd(frameData.UnwrapFailedBlock);
                foreach (Tunnel tunnel in frame.BorderNodes.OfType<Tunnel>().Where(t => t.Direction == Direction.Output))
                {
                    // Store a None value for the tunnel
                    VariableReference outputVariable = tunnel.OutputTerminals[0].GetTrueVariable();
                    ValueSource outputSource = _variableValues[outputVariable];
                    LLVMTypeRef outputType = outputVariable.Type.AsLLVMType();
                    outputSource.UpdateValue(_builder, LLVMSharp.LLVM.ConstNull(outputType));
                }
                _builder.CreateBr(frameData.EndBlock);
            }
            _builder.PositionBuilderAtEnd(frameData.EndBlock);
        }

        public bool VisitUnwrapOptionTunnel(UnwrapOptionTunnel unwrapOptionTunnel)
        {
            FrameData frameData = _frameData[(Frame)unwrapOptionTunnel.ParentStructure];
            var tunnelInputAllocationSource = (LocalAllocationValueSource)GetTerminalValueSource(unwrapOptionTunnel.InputTerminals[0]);
            LLVMValueRef isSomePtr = _builder.CreateStructGEP(tunnelInputAllocationSource.AllocationPointer, 0, "isSomePtr");
            LLVMValueRef isSome = _builder.CreateLoad(isSomePtr, "isSome");
            LLVMBasicBlockRef someBlock = _topLevelFunction.AppendBasicBlock($"unwrapOption{unwrapOptionTunnel.UniqueId}_some");
            _builder.CreateCondBr(isSome, someBlock, frameData.UnwrapFailedBlock);

            _builder.PositionBuilderAtEnd(someBlock);
            LLVMValueRef valuePtr = _builder.CreateStructGEP(tunnelInputAllocationSource.AllocationPointer, 1, "valuePtr");
            LLVMValueRef value = _builder.CreateLoad(valuePtr, "value");
            ValueSource tunnelOutputSource = GetTerminalValueSource(unwrapOptionTunnel.OutputTerminals[0]);
            tunnelOutputSource.UpdateValue(_builder, value);
            return true;
        }

#endregion

#region Loop

        private struct LoopData
        {
            public LoopData(
                LocalAllocationValueSource conditionAllocationSource,
                LLVMBasicBlockRef startBlock,
                LLVMBasicBlockRef interiorBlock,
                LLVMBasicBlockRef endBlock)
            {
                ConditionAllocationSource = conditionAllocationSource;
                StartBlock = startBlock;
                InteriorBlock = interiorBlock;
                EndBlock = endBlock;
            }

            public LoopData(
                Compiler.Nodes.Loop loop,
                FunctionCompiler functionCompiler)
            {
                var function = functionCompiler._topLevelFunction;
                StartBlock = function.AppendBasicBlock($"loop{loop.UniqueId}_start");
                InteriorBlock = function.AppendBasicBlock($"loop{loop.UniqueId}_interior");
                EndBlock = function.AppendBasicBlock($"loop{loop.UniqueId}_end");
                LoopConditionTunnel loopCondition = loop.BorderNodes.OfType<LoopConditionTunnel>().First();
                Terminal loopConditionInput = loopCondition.InputTerminals[0];
                ConditionAllocationSource = (LocalAllocationValueSource)functionCompiler.GetTerminalValueSource(loopConditionInput);
            }

            public LocalAllocationValueSource ConditionAllocationSource { get; }

            public LLVMBasicBlockRef StartBlock { get; }

            public LLVMBasicBlockRef InteriorBlock { get; }

            public LLVMBasicBlockRef EndBlock { get; }
        }

        private Dictionary<Compiler.Nodes.Loop, LoopData> _loopData = new Dictionary<Compiler.Nodes.Loop, LoopData>();

        public bool VisitLoop(Compiler.Nodes.Loop loop, StructureTraversalPoint traversalPoint)
        {
            // generate code for each left-side border node;
            // each border node that can affect condition should &&= the LoopCondition
            // variable with whether it allows loop to proceed
            switch (traversalPoint)
            {
                case StructureTraversalPoint.BeforeLeftBorderNodes:
                    VisitLoopBeforeLeftBorderNodes(loop);
                    break;
                case StructureTraversalPoint.AfterLeftBorderNodesAndBeforeDiagram:
                    VisitLoopAfterLeftBorderNodes(loop);
                    break;
                case StructureTraversalPoint.AfterRightBorderNodes:
                    VisitLoopAfterRightBorderNodes(loop);
                    break;
            }
            return true;
        }

        private void VisitLoopBeforeLeftBorderNodes(Compiler.Nodes.Loop loop)
        {
            LoopConditionTunnel loopCondition = loop.BorderNodes.OfType<LoopConditionTunnel>().First();
            Terminal loopConditionInput = loopCondition.InputTerminals[0];
            LoopData loopData = new LoopData(loop, this);
            _loopData[loop] = loopData;

            if (!loopConditionInput.IsConnected)
            {
                // if loop condition was unwired, initialize it to true
                loopData.ConditionAllocationSource.UpdateValue(_builder, true.AsLLVMValue());
            }

            // initialize all output tunnels with None values, in case the loop interior does not execute
            foreach (Tunnel outputTunnel in loop.BorderNodes.OfType<Tunnel>().Where(tunnel => tunnel.Direction == Direction.Output))
            {
                VariableReference tunnelOutputVariable = outputTunnel.OutputTerminals[0].GetTrueVariable();
                ValueSource tunnelOutputSource = _variableValues[tunnelOutputVariable];
                LLVMTypeRef tunnelOutputType = tunnelOutputVariable.Type.AsLLVMType();
                tunnelOutputSource.UpdateValue(_builder, LLVMSharp.LLVM.ConstNull(tunnelOutputType));
            }

            _builder.CreateBr(loopData.StartBlock);
            _builder.PositionBuilderAtEnd(loopData.StartBlock);
        }

        private void VisitLoopAfterLeftBorderNodes(Compiler.Nodes.Loop loop)
        {
            LoopData loopData = _loopData[loop];
            LLVMValueRef condition = loopData.ConditionAllocationSource.GetValue(_builder);
            _builder.CreateCondBr(condition, loopData.InteriorBlock, loopData.EndBlock);
            _builder.PositionBuilderAtEnd(loopData.InteriorBlock);
        }

        private void VisitLoopAfterRightBorderNodes(Compiler.Nodes.Loop loop)
        {
            LoopData loopData = _loopData[loop];
            _builder.CreateBr(loopData.StartBlock);
            _builder.PositionBuilderAtEnd(loopData.EndBlock);
        }

        public bool VisitLoopConditionTunnel(LoopConditionTunnel loopConditionTunnel)
        {
            return true;
        }

        public bool VisitIterateTunnel(IterateTunnel iterateTunnel)
        {
            ValueSource iteratorSource = GetTerminalValueSource(iterateTunnel.InputTerminals[0]),
                itemSource = GetTerminalValueSource(iterateTunnel.OutputTerminals[0]);

            // call the Iterator::next function on the iterator reference
            // TODO: create an alloca'd Option<Item> variable, so that we can pass its address to Iterator::next
            LLVMValueRef itemOption = _builder.CreateCall(
                GetImportedCommonFunction(CommonModules.RangeIteratorNextName), // TODO: determine the name of this function from the input type
                new LLVMValueRef[]
                {
                    iteratorSource.GetValue(_builder)
                },
                "itemOption");
            LLVMValueRef isSome = _builder.CreateExtractValue(itemOption, 0u, "isSome"),
                item = _builder.CreateExtractValue(itemOption, 1u, "item");

            // &&= the loop condition with the isSome value
            var loop = (Compiler.Nodes.Loop)iterateTunnel.ParentStructure;
            LocalAllocationValueSource loopConditionAllocationSource = _loopData[loop].ConditionAllocationSource;
            LLVMValueRef condition = loopConditionAllocationSource.GetValue(_builder);
            LLVMValueRef conditionAndIsSome = _builder.CreateAnd(condition, isSome, "conditionAndIsSome");
            loopConditionAllocationSource.UpdateValue(_builder, conditionAndIsSome);

            // bind the inner value to the output tunnel
            itemSource.UpdateValue(_builder, item);
            return true;
        }

        #endregion

        #region Option Pattern Structure

        private struct OptionPatternStructureData
        {
            public OptionPatternStructureData(OptionPatternStructure optionPatternStructure, LLVMValueRef function)
            {
                SomeDiagramEntryBlock = function.AppendBasicBlock($"optionPatternStructure{optionPatternStructure.UniqueId}_someEntry");
                NoneDiagramEntryBlock = function.AppendBasicBlock($"optionPatternStructure{optionPatternStructure.UniqueId}_noneEntry");
                EndBlock = function.AppendBasicBlock($"optionPatternStructure{optionPatternStructure.UniqueId}_end");
            }

            public LLVMBasicBlockRef SomeDiagramEntryBlock { get; }
            public LLVMBasicBlockRef NoneDiagramEntryBlock { get; }
            public LLVMBasicBlockRef EndBlock { get; }
        }

        private readonly Dictionary<OptionPatternStructure, OptionPatternStructureData> _optionPatternStructureData = new Dictionary<OptionPatternStructure, OptionPatternStructureData>();

        public bool VisitOptionPatternStructure(OptionPatternStructure optionPatternStructure, StructureTraversalPoint traversalPoint, Diagram nestedDiagram)
        {
            switch (traversalPoint)
            {
                case StructureTraversalPoint.BeforeLeftBorderNodes:
                    VisitOptionPatternStructureBeforeLeftBorderNodes(optionPatternStructure);
                    break;
                case StructureTraversalPoint.AfterLeftBorderNodesAndBeforeDiagram:
                    VisitOptionPatternStructureBeforeDiagram(optionPatternStructure, nestedDiagram);
                    break;
                case StructureTraversalPoint.AfterDiagram:
                    VisitOptionPatternStructureAfterDiagram(optionPatternStructure, nestedDiagram);
                    break;
                case StructureTraversalPoint.AfterAllDiagramsAndBeforeRightBorderNodes:
                    VisitOptionPatternStructureAfterAllDiagramsAndBeforeRightBorderNodes(optionPatternStructure);
                    break;
            }
            return true;
        }

        private void VisitOptionPatternStructureBeforeLeftBorderNodes(OptionPatternStructure optionPatternStructure)
        {
            OptionPatternStructureData data = new OptionPatternStructureData(optionPatternStructure, _topLevelFunction);
            _optionPatternStructureData[optionPatternStructure] = data;

            OptionPatternStructureSelector selector = optionPatternStructure.Selector;
            var selectorInputAllocationSource = (LocalAllocationValueSource)GetTerminalValueSource(selector.InputTerminals[0]);
            LLVMValueRef isSomePtr = _builder.CreateStructGEP(selectorInputAllocationSource.AllocationPointer, 0, "isSomePtr");
            LLVMValueRef isSome = _builder.CreateLoad(isSomePtr, "isSome");
            _builder.CreateCondBr(isSome, data.SomeDiagramEntryBlock, data.NoneDiagramEntryBlock);
        }

        private void VisitOptionPatternStructureBeforeDiagram(OptionPatternStructure optionPatternStructure, Diagram diagram)
        {
            OptionPatternStructureData data = _optionPatternStructureData[optionPatternStructure];
            LLVMBasicBlockRef block = diagram == optionPatternStructure.Diagrams[0] ? data.SomeDiagramEntryBlock : data.NoneDiagramEntryBlock;
            _builder.PositionBuilderAtEnd(block);
        }
        
        private void VisitOptionPatternStructureAfterDiagram(OptionPatternStructure optionPatternStructure, Diagram diagram)
        {
            LLVMBasicBlockRef currentBlock = _builder.GetInsertBlock();
            foreach (Tunnel outputTunnel in optionPatternStructure.Tunnels.Where(tunnel => tunnel.Direction == Direction.Output))
            {
                _tunnelInfos[outputTunnel].AddDiagramFinalBasicBlock(diagram, currentBlock);
            }

            OptionPatternStructureData data = _optionPatternStructureData[optionPatternStructure];
            _builder.CreateBr(data.EndBlock);
        }

        private void VisitOptionPatternStructureAfterAllDiagramsAndBeforeRightBorderNodes(OptionPatternStructure optionPatternStructure)
        {
            OptionPatternStructureData data = _optionPatternStructureData[optionPatternStructure];
            _builder.PositionBuilderAtEnd(data.EndBlock);
        }

        public bool VisitOptionPatternStructureSelector(OptionPatternStructureSelector optionPatternStructureSelector)
        {
            LLVMBasicBlockRef currentBlock = _builder.GetInsertBlock();
            var optionPatternStructure = (OptionPatternStructure)optionPatternStructureSelector.ParentStructure;
            OptionPatternStructureData data = _optionPatternStructureData[optionPatternStructure];

            _builder.PositionBuilderAtEnd(data.SomeDiagramEntryBlock);
            LocalAllocationValueSource selectorInputAllocationSource = (LocalAllocationValueSource)GetTerminalValueSource(optionPatternStructureSelector.InputTerminals[0]),
                selectorOutputAllocationSource = (LocalAllocationValueSource)GetTerminalValueSource(optionPatternStructureSelector.OutputTerminals[0]);
            LLVMValueRef innerValuePtr = _builder.CreateStructGEP(selectorInputAllocationSource.AllocationPointer, 1, "innerValuePtr");
            LLVMValueRef innerValue = _builder.CreateLoad(innerValuePtr, "innerValue");
            selectorOutputAllocationSource.UpdateValue(_builder, innerValue);

            _builder.PositionBuilderAtEnd(currentBlock);
            return true;
        }

        #endregion
    }
}
