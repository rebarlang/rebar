using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using LLVMSharp;
using NationalInstruments;
using NationalInstruments.DataTypes;
using NationalInstruments.Dfir;
using Rebar.Common;
using Rebar.Compiler;
using Rebar.Compiler.Nodes;

namespace Rebar.RebarTarget.LLVM
{
    internal class FunctionCompiler : VisitorTransformBase, IDfirNodeVisitor<bool>, IDfirStructureVisitor<bool>
    {
        private static readonly Dictionary<string, Action<FunctionCompiler, FunctionalNode>> _functionalNodeCompilers;

        static FunctionCompiler()
        {
            _functionalNodeCompilers = new Dictionary<string, Action<FunctionCompiler, FunctionalNode>>();
            _functionalNodeCompilers["ImmutPass"] = CompileNothing;
            _functionalNodeCompilers["MutPass"] = CompileNothing;
            _functionalNodeCompilers["Inspect"] = CompileInspect;
            _functionalNodeCompilers["Output"] = CompileOutput;

            _functionalNodeCompilers["Assign"] = CompileAssign;
            _functionalNodeCompilers["Exchange"] = CompileExchange;
            _functionalNodeCompilers["CreateCopy"] = CompileCreateCopy;
            _functionalNodeCompilers["SelectReference"] = CompileSelectReference;

            _functionalNodeCompilers["Add"] = CreatePureBinaryOperationCompiler((compiler, left, right) => compiler._builder.CreateAdd(left, right, "add"));
            _functionalNodeCompilers["Subtract"] = CreatePureBinaryOperationCompiler((compiler, left, right) => compiler._builder.CreateSub(left, right, "subtract"));
            _functionalNodeCompilers["Multiply"] = CreatePureBinaryOperationCompiler((compiler, left, right) => compiler._builder.CreateMul(left, right, "multiply"));
            _functionalNodeCompilers["Divide"] = CreatePureBinaryOperationCompiler((compiler, left, right) => compiler._builder.CreateSDiv(left, right, "divide"));
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

            _functionalNodeCompilers["VectorCreate"] = CompileNothing;
            _functionalNodeCompilers["VectorInsert"] = CompileNothing;

            _functionalNodeCompilers["CreateLockingCell"] = CompileNothing;
            _functionalNodeCompilers["CreateNonLockingCell"] = CompileNothing;
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
                compiler.CreateCallForFunctionalNode(compiler.GetImportedCommonFunction(CommonModules.OutputStringSliceName), outputNode);
                return;
            }
            else
            {
                throw new NotImplementedException($"Don't know how to display type {referentType} yet.");
            }
        }

        private static void CompileAssign(FunctionCompiler compiler, FunctionalNode assignNode)
        {
            ValueSource assigneeSource = compiler.GetTerminalValueSource(assignNode.InputTerminals[0]),
                newValueSource = compiler.GetTerminalValueSource(assignNode.InputTerminals[1]);
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
            // TODO: handle deep-copyable types
            ValueSource copyFromSource = compiler.GetTerminalValueSource(createCopyNode.InputTerminals[0]),
                copySource = compiler.GetTerminalValueSource(createCopyNode.OutputTerminals[1]);
            copySource.UpdateValue(compiler._builder, copyFromSource.GetDeferencedValue(compiler._builder));
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

        private static Action<FunctionCompiler, FunctionalNode> CreateImportedCommonFunctionCompiler(string functionName)
        {
            return (compiler, functionalNode) =>
                compiler.CreateCallForFunctionalNode(compiler.GetImportedCommonFunction(functionName), functionalNode);
        }

        #endregion

        private readonly IRBuilder _builder;
        private readonly LLVMValueRef _topLevelFunction;
        private readonly Dictionary<VariableReference, ValueSource> _variableValues;
        private readonly CommonExternalFunctions _commonExternalFunctions;
        private readonly Dictionary<string, LLVMValueRef> _importedFunctions = new Dictionary<string, LLVMValueRef>();

        public FunctionCompiler(Module module, string functionName, Dictionary<VariableReference, ValueSource> variableValues)
        {
            Module = module;
            _variableValues = variableValues;

            LLVMTypeRef functionType = LLVMSharp.LLVM.FunctionType(LLVMSharp.LLVM.VoidType(), new LLVMTypeRef[] { }, false);
            _topLevelFunction = Module.AddFunction(functionName, functionType);
            LLVMBasicBlockRef entryBlock = _topLevelFunction.AppendBasicBlock("entry");
            _builder = new IRBuilder();
            _builder.PositionBuilderAtEnd(entryBlock);

            _commonExternalFunctions = new CommonExternalFunctions(module);
            InitializeLocalAllocations();
        }

        public Module Module { get; }

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

        private void CreateCallForFunctionalNode(LLVMValueRef function, FunctionalNode node)
        {
            var arguments = new List<LLVMValueRef>();
            foreach (Terminal inputTerminal in node.InputTerminals)
            {
                arguments.Add(GetTerminalValueSource(inputTerminal).GetValue(_builder));
            }
            Signature nodeSignature = Signatures.GetSignatureForNIType(node.Signature);
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

        protected override void VisitStructure(Structure structure, StructureTraversalPoint traversalPoint)
        {
            base.VisitStructure(structure, traversalPoint);
            this.VisitRebarStructure(structure, traversalPoint);
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

        public bool VisitDropNode(DropNode dropNode)
        {
            VariableReference input = dropNode.InputTerminals[0].GetTrueVariable();
            var inputAllocation = (LocalAllocationValueSource)_variableValues[input];
            NIType inputType = input.Type;
            if (inputType.TypeHasDropTrait())
            {
                CreateDropCall(inputType, inputAllocation.AllocationPointer);
                return true;
            }

            NIType innerType;
            if (inputType.TryDestructureVectorType(out innerType))
            {
                // TODO
                return true;
            }
            if (inputType.TryDestructureOptionType(out innerType) && innerType.TypeHasDropTrait())
            {
                // TODO: turn this into a monomorphized generic function call
                LLVMValueRef isSomePtr = _builder.CreateStructGEP(inputAllocation.AllocationPointer, 0u, "isSomePtr"),
                    isSome = _builder.CreateLoad(isSomePtr, "isSome");
                LLVMBasicBlockRef optionDropIsSomeBlock = _topLevelFunction.AppendBasicBlock("optionDropIsSome"),
                    optionDropEndBlock = _topLevelFunction.AppendBasicBlock("optionDropEnd");
                _builder.CreateCondBr(isSome, optionDropIsSomeBlock, optionDropEndBlock);

                _builder.PositionBuilderAtEnd(optionDropIsSomeBlock);
                LLVMValueRef innerValuePtr = _builder.CreateStructGEP(inputAllocation.AllocationPointer, 1u, "innerValuePtr");
                CreateDropCall(innerType, innerValuePtr);
                _builder.CreateBr(optionDropEndBlock);

                _builder.PositionBuilderAtEnd(optionDropEndBlock);
                return true;
            }
            return true;
        }

        private void CreateDropCall(NIType droppedValueType, LLVMValueRef droppedValuePtr)
        {
            LLVMValueRef dropFunction;
            if (droppedValueType == PFTypes.String)
            {
                dropFunction = GetImportedCommonFunction(CommonModules.DropStringName);                
            }
            else
            {
                throw new InvalidOperationException("Drop function not found for type: " + droppedValueType);
            }
            _builder.CreateCall(dropFunction, new LLVMValueRef[] { droppedValuePtr }, string.Empty);
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

        public bool VisitTerminateLifetimeNode(TerminateLifetimeNode terminateLifetimeNode)
        {
            return true;
        }

        public bool VisitTerminateLifetimeTunnel(TerminateLifetimeTunnel terminateLifetimeTunnel)
        {
            return true;
        }

        public bool VisitTunnel(Tunnel tunnel)
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
            return true;
        }

#region Frame

        private struct FrameData
        {
            public FrameData(
                LLVMBasicBlockRef interiorBlock,
                LLVMBasicBlockRef unwrapFailedBlock, 
                LLVMBasicBlockRef endBlock)
            {
                InteriorBlock = interiorBlock;
                UnwrapFailedBlock = unwrapFailedBlock;
                EndBlock = endBlock;
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
            LLVMBasicBlockRef interiorBlock = _topLevelFunction.AppendBasicBlock($"frame{frame.UniqueId}_interior"),
                unwrapFailedBlock = _topLevelFunction.AppendBasicBlock($"frame{frame.UniqueId}_unwrapFailed"),
                endBlock = _topLevelFunction.AppendBasicBlock($"frame{frame.UniqueId}_end");
            _frameData[frame] = new FrameData(interiorBlock, unwrapFailedBlock, endBlock);
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
            LLVMBasicBlockRef startBlock = _topLevelFunction.AppendBasicBlock($"loop{loop.UniqueId}_start"),
                interiorBlock = _topLevelFunction.AppendBasicBlock($"loop{loop.UniqueId}_interior"),
                endBlock = _topLevelFunction.AppendBasicBlock($"loop{loop.UniqueId}_end");
            LoopConditionTunnel loopCondition = loop.BorderNodes.OfType<LoopConditionTunnel>().First();
            Terminal loopConditionInput = loopCondition.InputTerminals[0];
            var conditionAllocationSource = (LocalAllocationValueSource)GetTerminalValueSource(loopConditionInput);
            _loopData[loop] = new LoopData(conditionAllocationSource, startBlock, interiorBlock, endBlock);

            if (!loopConditionInput.IsConnected)
            {
                // if loop condition was unwired, initialize it to true
                conditionAllocationSource.UpdateValue(_builder, true.AsLLVMValue());
            }

            // initialize all output tunnels with None values, in case the loop interior does not execute
            foreach (Tunnel outputTunnel in loop.BorderNodes.OfType<Tunnel>().Where(tunnel => tunnel.Direction == Direction.Output))
            {
                VariableReference tunnelOutputVariable = outputTunnel.OutputTerminals[0].GetTrueVariable();
                ValueSource tunnelOutputSource = _variableValues[tunnelOutputVariable];
                LLVMTypeRef tunnelOutputType = tunnelOutputVariable.Type.AsLLVMType();
                tunnelOutputSource.UpdateValue(_builder, LLVMSharp.LLVM.ConstNull(tunnelOutputType));
            }

            _builder.CreateBr(startBlock);
            _builder.PositionBuilderAtEnd(startBlock);
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
    }
}
