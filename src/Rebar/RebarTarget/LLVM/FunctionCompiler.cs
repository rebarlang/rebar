using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using LLVMSharp;
using NationalInstruments;
using NationalInstruments.CommonModel;
using NationalInstruments.Core;
using NationalInstruments.DataTypes;
using NationalInstruments.Dfir;
using NationalInstruments.ExecutionFramework;
using Rebar.Common;
using Rebar.Compiler;
using Rebar.Compiler.Nodes;

namespace Rebar.RebarTarget.LLVM
{
    internal partial class FunctionCompiler : VisitorTransformBase, IVisitationHandler<bool>, IInternalDfirNodeVisitor<bool>
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

            _functionalNodeCompilers["Add"] = CreatePureBinaryOperationCompiler((compiler, left, right) => compiler.Builder.CreateAdd(left, right, "add"));
            _functionalNodeCompilers["Subtract"] = CreatePureBinaryOperationCompiler((compiler, left, right) => compiler.Builder.CreateSub(left, right, "subtract"));
            _functionalNodeCompilers["Multiply"] = CreatePureBinaryOperationCompiler((compiler, left, right) => compiler.Builder.CreateMul(left, right, "multiply"));
            _functionalNodeCompilers["Divide"] = CreatePureBinaryOperationCompiler((compiler, left, right) => compiler.Builder.CreateSDiv(left, right, "divide"));
            _functionalNodeCompilers["Modulus"] = CreatePureBinaryOperationCompiler((compiler, left, right) => compiler.Builder.CreateSRem(left, right, "modulus"));
            _functionalNodeCompilers["Increment"] = CreatePureUnaryOperationCompiler((compiler, value) => compiler.Builder.CreateAdd(value, compiler.Context.AsLLVMValue(1), "increment"));
            _functionalNodeCompilers["And"] = CreatePureBinaryOperationCompiler((compiler, left, right) => compiler.Builder.CreateAnd(left, right, "and"));
            _functionalNodeCompilers["Or"] = CreatePureBinaryOperationCompiler((compiler, left, right) => compiler.Builder.CreateOr(left, right, "or"));
            _functionalNodeCompilers["Xor"] = CreatePureBinaryOperationCompiler((compiler, left, right) => compiler.Builder.CreateXor(left, right, "xor"));
            _functionalNodeCompilers["Not"] = CreatePureUnaryOperationCompiler((compiler, value) => compiler.Builder.CreateNot(value, "not"));

            _functionalNodeCompilers["AccumulateAdd"] = CreateMutatingBinaryOperationCompiler((compiler, left, right) => compiler.Builder.CreateAdd(left, right, "add"));
            _functionalNodeCompilers["AccumulateSubtract"] = CreateMutatingBinaryOperationCompiler((compiler, left, right) => compiler.Builder.CreateSub(left, right, "subtract"));
            _functionalNodeCompilers["AccumulateMultiply"] = CreateMutatingBinaryOperationCompiler((compiler, left, right) => compiler.Builder.CreateMul(left, right, "multiply"));
            _functionalNodeCompilers["AccumulateDivide"] = CreateMutatingBinaryOperationCompiler((compiler, left, right) => compiler.Builder.CreateSDiv(left, right, "divide"));
            _functionalNodeCompilers["AccumulateIncrement"] = CreateMutatingUnaryOperationCompiler((compiler, value) => compiler.Builder.CreateAdd(value, compiler.Context.AsLLVMValue(1), "increment"));
            _functionalNodeCompilers["AccumulateAnd"] = CreateMutatingBinaryOperationCompiler((compiler, left, right) => compiler.Builder.CreateAnd(left, right, "and"));
            _functionalNodeCompilers["AccumulateOr"] = CreateMutatingBinaryOperationCompiler((compiler, left, right) => compiler.Builder.CreateOr(left, right, "or"));
            _functionalNodeCompilers["AccumulateXor"] = CreateMutatingBinaryOperationCompiler((compiler, left, right) => compiler.Builder.CreateXor(left, right, "xor"));
            _functionalNodeCompilers["AccumulateNot"] = CreateMutatingUnaryOperationCompiler((compiler, value) => compiler.Builder.CreateNot(value, "not"));

            _functionalNodeCompilers["Equal"] = CreatePureBinaryOperationCompiler((compiler, left, right) => compiler.Builder.CreateICmp(LLVMIntPredicate.LLVMIntEQ, left, right, "eq"));
            _functionalNodeCompilers["NotEqual"] = CreatePureBinaryOperationCompiler((compiler, left, right) => compiler.Builder.CreateICmp(LLVMIntPredicate.LLVMIntNE, left, right, "ne"));
            _functionalNodeCompilers["LessThan"] = CreatePureBinaryOperationCompiler((compiler, left, right) => compiler.Builder.CreateICmp(LLVMIntPredicate.LLVMIntSLT, left, right, "lt"));
            _functionalNodeCompilers["LessEqual"] = CreatePureBinaryOperationCompiler((compiler, left, right) => compiler.Builder.CreateICmp(LLVMIntPredicate.LLVMIntSLE, left, right, "le"));
            _functionalNodeCompilers["GreaterThan"] = CreatePureBinaryOperationCompiler((compiler, left, right) => compiler.Builder.CreateICmp(LLVMIntPredicate.LLVMIntSGT, left, right, "gt"));
            _functionalNodeCompilers["GreaterEqual"] = CreatePureBinaryOperationCompiler((compiler, left, right) => compiler.Builder.CreateICmp(LLVMIntPredicate.LLVMIntSGE, left, right, "ge"));

            _functionalNodeCompilers["Some"] = CompileSomeConstructor;
            _functionalNodeCompilers["None"] = CompileNoneConstructor;
            _functionalNodeCompilers["OptionToPanicResult"] = CreateSpecializedFunctionCallCompiler(BuildOptionToPanicResultFunction);

            _functionalNodeCompilers["Range"] = CreateImportedCommonFunctionCompiler(CommonModules.CreateRangeIteratorName);

            _functionalNodeCompilers["StringFromSlice"] = CreateImportedCommonFunctionCompiler(CommonModules.StringFromSliceName);
            _functionalNodeCompilers["StringToSlice"] = CreateImportedCommonFunctionCompiler(CommonModules.StringToSliceName);
            _functionalNodeCompilers["StringAppend"] = CreateImportedCommonFunctionCompiler(CommonModules.StringAppendName);
            _functionalNodeCompilers["StringConcat"] = CreateImportedCommonFunctionCompiler(CommonModules.StringConcatName);
            _functionalNodeCompilers["StringSliceToStringSplitIterator"] = CreateImportedCommonFunctionCompiler(CommonModules.StringSliceToStringSplitIteratorName);

            _functionalNodeCompilers["VectorCreate"] = CreateSpecializedFunctionCallCompiler(BuildVectorCreateFunction);
            _functionalNodeCompilers["VectorInitialize"] = CreateSpecializedFunctionCallCompiler(BuildVectorInitializeFunction);
            _functionalNodeCompilers["VectorToSlice"] = CreateSpecializedFunctionCallCompiler(BuildVectorToSliceFunction);
            _functionalNodeCompilers["VectorAppend"] = CreateSpecializedFunctionCallCompiler(BuildVectorAppendFunction);
            _functionalNodeCompilers["VectorInsert"] = CompileNothing;
            _functionalNodeCompilers["VectorRemoveLast"] = CreateSpecializedFunctionCallCompiler(BuildVectorRemoveLastFunction);
            _functionalNodeCompilers["SliceIndex"] = CreateSpecializedFunctionCallCompiler(CreateSliceIndexFunction);

            _functionalNodeCompilers["CreateLockingCell"] = CompileNothing;

            _functionalNodeCompilers["SharedCreate"] = CreateSpecializedFunctionCallCompiler(BuildSharedCreateFunction);
            _functionalNodeCompilers["SharedGetValue"] = CreateSpecializedFunctionCallCompiler(BuildSharedGetValueFunction);

            _functionalNodeCompilers["OpenFileHandle"] = CreateImportedCommonFunctionCompiler(CommonModules.OpenFileHandleName);
            _functionalNodeCompilers["ReadLineFromFileHandle"] = CreateImportedCommonFunctionCompiler(CommonModules.ReadLineFromFileHandleName);
            _functionalNodeCompilers["WriteStringToFileHandle"] = CreateImportedCommonFunctionCompiler(CommonModules.WriteStringToFileHandleName);

            _functionalNodeCompilers["CreateYieldPromise"] = CreateSpecializedFunctionCallCompiler(BuildCreateYieldPromiseFunction);

            _functionalNodeCompilers["CreateNotifierPair"] = CreateSpecializedFunctionCallCompiler(BuildCreateNotifierPairFunction);
            _functionalNodeCompilers["GetReaderPromise"] = CreateSpecializedFunctionCallCompiler(BuildGetNotifierReaderPromiseFunction);
            _functionalNodeCompilers["SetNotifierValue"] = CreateSpecializedFunctionCallCompiler(BuildSetNotifierValueFunction);
        }

        #region Functional node compilers

        private static void CompileNothing(FunctionCompiler compiler, FunctionalNode noopNode)
        {
        }

        private static void CompileInspect(FunctionCompiler compiler, FunctionalNode inspectNode)
        {
            Terminal inputTerminal = inspectNode.InputTerminals[0];
            VariableReference input = inputTerminal.GetTrueVariable();

            // define global data in module for inspected value
            LLVMTypeRef globalType = compiler.Context.AsLLVMType(input.Type.GetReferentType());
            string globalName = $"inspect_{inspectNode.UniqueId}";
            LLVMValueRef globalAddress = compiler.Module.AddGlobal(globalType, globalName);
            // Setting an initializer is necessary to distinguish this from an externally-defined global
            globalAddress.SetInitializer(LLVMSharp.LLVM.ConstNull(globalType));

            // load the input dereference value and store it in the global
            compiler.Builder.CreateStore(compiler.GetTerminalValueSource(inputTerminal).GetDereferencedValue(compiler.Builder), globalAddress);
        }

        private static void CompileOutput(FunctionCompiler compiler, FunctionalNode outputNode)
        {
            ValueSource inputValueSource = compiler.GetTerminalValueSource(outputNode.InputTerminals[0]);
            VariableReference input = outputNode.InputTerminals[0].GetTrueVariable();
            NIType referentType = input.Type.GetReferentType();
            if (referentType.IsBoolean())
            {
                LLVMValueRef value = inputValueSource.GetDereferencedValue(compiler.Builder);
                compiler.Builder.CreateCall(compiler.GetImportedCommonFunction(CommonModules.OutputBoolName), new LLVMValueRef[] { value }, string.Empty);
                return;
            }
            if (referentType.IsInteger())
            {
                LLVMValueRef outputFunction;
                switch (referentType.GetKind())
                {
                    case NITypeKind.Int8:
                        outputFunction = compiler.GetImportedCommonFunction(CommonModules.OutputInt8Name);
                        break;
                    case NITypeKind.UInt8:
                        outputFunction = compiler.GetImportedCommonFunction(CommonModules.OutputUInt8Name);
                        break;
                    case NITypeKind.Int16:
                        outputFunction = compiler.GetImportedCommonFunction(CommonModules.OutputInt16Name);
                        break;
                    case NITypeKind.UInt16:
                        outputFunction = compiler.GetImportedCommonFunction(CommonModules.OutputUInt16Name);
                        break;
                    case NITypeKind.Int32:
                        outputFunction = compiler.GetImportedCommonFunction(CommonModules.OutputInt32Name);
                        break;
                    case NITypeKind.UInt32:
                        outputFunction = compiler.GetImportedCommonFunction(CommonModules.OutputUInt32Name);
                        break;
                    case NITypeKind.Int64:
                        outputFunction = compiler.GetImportedCommonFunction(CommonModules.OutputInt64Name);
                        break;
                    case NITypeKind.UInt64:
                        outputFunction = compiler.GetImportedCommonFunction(CommonModules.OutputUInt64Name);
                        break;
                    default:
                        throw new NotImplementedException($"Don't know how to display type {referentType} yet.");
                }
                LLVMValueRef value = inputValueSource.GetDereferencedValue(compiler.Builder);
                compiler.Builder.CreateCall(outputFunction, new LLVMValueRef[] { value }, string.Empty);
                return;
            }
            if (referentType.IsString())
            {
                // TODO: this should go away once auto-borrowing into string slices works
                // call output_string with string pointer and size
                LLVMValueRef stringPtr = inputValueSource.GetValue(compiler.Builder),
                    stringSlice = compiler.Builder.CreateCall(
                        compiler.GetImportedCommonFunction(CommonModules.StringToSliceRetName),
                        new LLVMValueRef[] { stringPtr },
                        "stringSlice");
                compiler.Builder.CreateCall(
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
            compiler.ModuleContext.CreateDropCallIfDropFunctionExists(compiler.Builder, assigneeType, b => assigneeSource.GetValue(b));
            assigneeSource.UpdateDereferencedValue(compiler.Builder, newValueSource.GetValue(compiler.Builder));
        }

        private static void CompileExchange(FunctionCompiler compiler, FunctionalNode exchangeNode)
        {
            ValueSource valueSource1 = compiler.GetTerminalValueSource(exchangeNode.InputTerminals[0]),
                valueSource2 = compiler.GetTerminalValueSource(exchangeNode.InputTerminals[1]);
            LLVMValueRef valueRef1 = valueSource1.GetDereferencedValue(compiler.Builder),
                valueRef2 = valueSource2.GetDereferencedValue(compiler.Builder);
            valueSource1.UpdateDereferencedValue(compiler.Builder, valueRef2);
            valueSource2.UpdateDereferencedValue(compiler.Builder, valueRef1);
        }

        private static void CompileCreateCopy(FunctionCompiler compiler, FunctionalNode createCopyNode)
        {
            NIType valueType = createCopyNode.OutputTerminals[1].GetTrueVariable().Type;
            if (valueType.WireTypeMayFork())
            {
                ValueSource copyFromSource = compiler.GetTerminalValueSource(createCopyNode.InputTerminals[0]);
                ValueSource copySource = compiler.GetTerminalValueSource(createCopyNode.OutputTerminals[1]);
                compiler.InitializeIfNecessary(copySource, copyFromSource.GetDereferencedValue);
                return;
            }

            LLVMValueRef cloneFunction;
            if (compiler.ModuleContext.TryGetCloneFunction(valueType, out cloneFunction))
            {
                compiler.CreateCallForFunctionalNode(cloneFunction, createCopyNode);
                return;
            }

            throw new NotSupportedException("Don't know how to compile CreateCopy for type " + valueType);
        }

        private static void CompileSelectReference(FunctionCompiler compiler, FunctionalNode selectReferenceNode)
        {
            ValueSource selectorSource = compiler.GetTerminalValueSource(selectReferenceNode.InputTerminals[0]),
                trueValueSource = compiler.GetTerminalValueSource(selectReferenceNode.InputTerminals[1]),
                falseValueSource = compiler.GetTerminalValueSource(selectReferenceNode.InputTerminals[2]);
            LLVMValueRef selectedValue = compiler.Builder.CreateSelect(
                selectorSource.GetDereferencedValue(compiler.Builder),
                trueValueSource.GetValue(compiler.Builder),
                falseValueSource.GetValue(compiler.Builder),
                "select");
            ValueSource selectedValueSource = compiler.GetTerminalValueSource(selectReferenceNode.OutputTerminals[1]);
            compiler.Initialize(selectedValueSource, selectedValue);
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
            LLVMValueRef inputValue = inputValueSource.GetDereferencedValue(compiler.Builder);
            if (!mutating)
            {
                ValueSource outputValueSource = compiler.GetTerminalValueSource(operationNode.OutputTerminals[1]);
                compiler.InitializeIfNecessary(outputValueSource, builder => generateOperation(compiler, inputValue));
            }
            else
            {
                inputValueSource.UpdateDereferencedValue(compiler.Builder, generateOperation(compiler, inputValue));
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
            LLVMValueRef leftValue = leftValueSource.GetDereferencedValue(compiler.Builder),
                rightValue = rightValueSource.GetDereferencedValue(compiler.Builder);
            if (!mutating)
            {
                ValueSource outputValueSource = compiler.GetTerminalValueSource(operationNode.OutputTerminals[2]);
                compiler.InitializeIfNecessary(outputValueSource, builder => generateOperation(compiler, leftValue, rightValue));
            }
            else
            {
                leftValueSource.UpdateDereferencedValue(compiler.Builder, generateOperation(compiler, leftValue, rightValue));
            }
        }

        private static Action<FunctionCompiler, FunctionalNode> CreateImportedCommonFunctionCompiler(string functionName)
        {
            return (compiler, functionalNode) =>
                compiler.CreateCallForFunctionalNode(compiler.GetImportedCommonFunction(functionName), functionalNode, functionalNode.Signature);
        }

        private static void BuildCreateYieldPromiseFunction(FunctionModuleContext moduleContext, NIType signature, LLVMValueRef createYieldPromiseFunction)
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

        private static void BuildYieldPromisePollFunction(FunctionModuleContext moduleContext, NIType signature, LLVMValueRef yieldPromisePollFunction)
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

        #endregion

        private readonly FunctionCompilerSharedData _sharedData;
        private readonly FunctionModuleBuilder _moduleBuilder;
        private readonly Dictionary<CompilableDefinitionName, bool> _calleesMayPanic;

        public FunctionCompiler(
            DfirRoot targetDfir,
            FunctionModuleBuilder moduleBuilder,
            FunctionCompilerSharedData sharedData,
            Dictionary<CompilableDefinitionName, bool> calleesMayPanic)
        {
            TargetDfir = targetDfir;
            _moduleBuilder = moduleBuilder;
            _sharedData = sharedData;
            _calleesMayPanic = calleesMayPanic;
            ModuleContext = new FunctionModuleContext(_sharedData.Context, _moduleBuilder.Module, _sharedData.FunctionImporter);
        }

        private ContextWrapper Context => _sharedData.Context;

        private Module Module => _moduleBuilder.Module;

        private FunctionCompilerState CurrentState => _sharedData.CurrentState;

        private LLVMValueRef CurrentFunction => CurrentState.Function;

        private IRBuilder Builder => CurrentState.Builder;

        private DfirRoot TargetDfir { get; }

        private FunctionAllocationSet AllocationSet => _sharedData.AllocationSet;

        private FunctionModuleContext ModuleContext { get; }

        internal LLVMValueRef GetImportedCommonFunction(string functionName)
        {
            return _sharedData.FunctionImporter.GetImportedCommonFunction(functionName);
        }

        private LLVMValueRef GetImportedSynchronousFunction(MethodCallNode methodCallNode)
        {
            string targetFunctionName = FunctionCompileHandler.FunctionLLVMName(methodCallNode.TargetName);
            return GetImportedFunction(
                FunctionNames.GetSynchronousFunctionName(targetFunctionName),
                () => Context.TranslateFunctionType(methodCallNode.Signature));
        }

        private LLVMValueRef GetImportedInitializeStateFunction(CreateMethodCallPromise createMethodCallPromise)
        {
            string targetFunctionName = FunctionCompileHandler.FunctionLLVMName(createMethodCallPromise.TargetName);
            return GetImportedFunction(
                FunctionNames.GetInitializeStateFunctionName(targetFunctionName),
                () => TranslateInitializeFunctionType(createMethodCallPromise.Signature));
        }

        private LLVMValueRef GetImportedPollFunction(CreateMethodCallPromise createMethodCallPromise)
        {
            string targetFunctionName = FunctionCompileHandler.FunctionLLVMName(createMethodCallPromise.TargetName);
            return GetImportedFunction(FunctionNames.GetPollFunctionName(targetFunctionName), () => AsynchronousFunctionModuleBuilder.PollFunctionType(Context));
        }

        private LLVMValueRef GetImportedFunction(string functionName, Func<LLVMTypeRef> getFunctionType)
        {
            return _sharedData.FunctionImporter.GetCachedFunction(functionName, () => Module.AddFunction(functionName, getFunctionType()));
        }

        private static Action<FunctionCompiler, FunctionalNode> CreateSpecializedFunctionCallCompiler(Action<FunctionModuleContext, NIType, LLVMValueRef> functionCreator)
        {
            return (compiler, functionalNode) =>
            {
                compiler.CreateCallForFunctionalNode(
                    compiler.ModuleContext.GetSpecializedFunctionWithSignature(functionalNode.FunctionType.FunctionNIType, functionCreator),
                    functionalNode);
            };
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
                arguments.Add(GetTerminalValueSource(inputTerminal).GetValue(Builder));
            }
            Signature nodeSignature = Signatures.GetSignatureForNIType(nodeFunctionSignature);
            foreach (var outputPair in node.OutputTerminals.Zip(nodeSignature.Outputs))
            {
                if (outputPair.Value.IsPassthrough)
                {
                    continue;
                }
                arguments.Add(GetAddress(GetTerminalValueSource(outputPair.Key), Builder));
            }
            Builder.CreateCall(function, arguments.ToArray(), string.Empty);
        }

        #region VisitorTransformBase overrides

        protected override void PostVisitDiagram(Diagram diagram)
        {
            base.PostVisitDiagram(diagram);
            if (diagram == diagram.DfirRoot.BlockDiagram)
            {
                Builder.CreateRetVoid();
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

        bool IDfirNodeVisitor<bool>.VisitWire(Wire wire)
        {
            VisitWire(wire);
            return true;
        }

        protected override void VisitWire(Wire wire)
        {
            if (wire.SinkTerminals.HasMoreThan(1))
            {
                Func<IRBuilder, LLVMValueRef> valueGetter = GetTerminalValueSource(wire.SourceTerminal).GetValue;
                foreach (var sinkTerminal in wire.SinkTerminals.Skip(1))
                {
                    InitializeIfNecessary(GetTerminalValueSource(sinkTerminal), valueGetter);
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

        private ValueSource GetValueSource(VariableReference variable)
        {
            return _sharedData.VariableStorage.GetValueSourceForVariable(variable);
        }

        private ValueSource GetTerminalValueSource(Terminal terminal)
        {
            return _sharedData.VariableStorage.GetValueSourceForVariable(terminal.GetTrueVariable());
        }

        private void BorrowFromVariableIntoVariable(VariableReference from, VariableReference into)
        {
            InitializeIfNecessary(GetValueSource(into), builder => GetAddress(GetValueSource(from), builder));
        }

        private void Initialize(ValueSource toInitialize, LLVMValueRef value)
        {
            var initializable = toInitialize as IInitializableValueSource;
            if (initializable == null)
            {
                throw new ArgumentException("Trying to initialize non-initializable variable", nameof(toInitialize));
            }
            initializable.InitializeValue(Builder, value);
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

        private LLVMValueRef GetPromisePollFunction(NIType type)
        {
            NIType innerType;
            if (type.TryDestructureYieldPromiseType(out innerType))
            {
                NIType signature = Signatures.PromisePollType.ReplaceGenericParameters(type, innerType, NIType.Unset);
                return ModuleContext.GetSpecializedFunctionWithSignature(signature, BuildYieldPromisePollFunction);
            }
            if (type.TryDestructureMethodCallPromiseType(out innerType))
            {
                NIType signature = Signatures.PromisePollType.ReplaceGenericParameters(type, innerType, NIType.Unset);
                return ModuleContext.GetSpecializedFunctionWithSignature(signature, BuildMethodCallPromisePollFunction);
            }
            if (type.TryDestructureNotifierReaderPromiseType(out innerType))
            {
                NIType signature = Signatures.PromisePollType.ReplaceGenericParameters(type, innerType.CreateOption(), NIType.Unset);
                return ModuleContext.GetSpecializedFunctionWithSignature(signature, BuildNotifierReaderPromisePollFunction);
            }
            throw new NotSupportedException("Cannot find poll function for type " + type);
        }

        public bool VisitBorrowTunnel(BorrowTunnel borrowTunnel)
        {
            VariableReference output = borrowTunnel.OutputTerminals[0].GetTrueVariable();
            VariableReference input = borrowTunnel.InputTerminals[0].GetTrueVariable();
            BorrowFromVariableIntoVariable(input, output);
            return true;
        }

        public bool VisitBuildTupleNode(BuildTupleNode buildTupleNode)
        {
            LLVMValueRef[] fieldValues = buildTupleNode
                .InputTerminals
                .Select(input => GetTerminalValueSource(input).GetValue(Builder))
                .ToArray();
            Terminal outputTerminal = buildTupleNode.OutputTerminals[0];
            ValueSource outputAllocationSource = GetTerminalValueSource(outputTerminal);
            LLVMTypeRef outputLLVMType = Context.AsLLVMType(outputTerminal.GetTrueVariable().Type);
            LLVMValueRef tuple = Builder.BuildStructValue(
                outputLLVMType,
                fieldValues,
                "tuple");
            Initialize(outputAllocationSource, tuple);
            return true;
        }

        public bool VisitConstant(Constant constant)
        {
            ValueSource outputValueSource = GetTerminalValueSource(constant.OutputTerminal);
            if (constant.DataType.IsInteger())
            {
                InitializeIfNecessary(outputValueSource, builder => Context.GetIntegerValue(constant.Value, constant.DataType));
            }
            else if (constant.DataType.IsBoolean())
            {
                InitializeIfNecessary(outputValueSource, builder => Context.AsLLVMValue((bool)constant.Value));
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

                    LLVMValueRef castPointer = Builder.CreateBitCast(
                        stringConstantPtr,
                        Context.BytePointerType(),
                        "ptrCast");
                    LLVMValueRef[] stringSliceFields = new LLVMValueRef[]
                    {
                        castPointer,
                        Context.AsLLVMValue(length)
                    };
                    LLVMValueRef stringSliceValue = LLVMValueRef.ConstStruct(stringSliceFields, false);
                    Initialize(outputValueSource, stringSliceValue);
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
            ValueSource terminalValueSource = GetTerminalValueSource(dataAccessor.Terminal);
            ValueSource dataItemValueSource = GetValueSource(dataAccessor.DataItem.GetVariable());
            if (dataAccessor.Terminal.Direction == Direction.Output)
            {
                // TODO: distinguish inout from in parameters?
                LLVMValueRef parameterValue = dataItemValueSource.GetValue(Builder);
                Initialize(terminalValueSource, parameterValue);
            }
            else if (dataAccessor.Terminal.Direction == Direction.Input)
            {
                // assume that the function parameter is a pointer to where we need to store the value
                LLVMValueRef value = terminalValueSource.GetValue(Builder);
                Update(dataItemValueSource, value);
            }
            return true;
        }

        public bool VisitDecomposeTupleNode(DecomposeTupleNode decomposeTupleNode)
        {
            ValueSource tupleRefSource = GetTerminalValueSource(decomposeTupleNode.InputTerminals[0]);
            LLVMValueRef tupleValue = tupleRefSource.GetValue(Builder);
            uint fieldIndex = 0;
            foreach (Terminal outputTerminal in decomposeTupleNode.OutputTerminals)
            {
                // TODO: for DecomposeMode.Borrow, it would be better to be able to extract elements from the dereferenced
                // input value without needing to take an address
                LLVMValueRef tupleElementValue = decomposeTupleNode.DecomposeMode == DecomposeMode.Borrow
                    ? Builder.CreateStructGEP(tupleValue, fieldIndex, "tupleElement")
                    : Builder.CreateExtractValue(tupleValue, fieldIndex, "tupleElement");
                Initialize(GetTerminalValueSource(outputTerminal), tupleElementValue);
                ++fieldIndex;
            }
            return true;
        }

        public bool VisitDropNode(DropNode dropNode)
        {
            Terminal inputTerminal = dropNode.InputTerminals[0];
            VariableReference input = inputTerminal.GetTrueVariable();
            var inputValueSource = GetTerminalValueSource(inputTerminal);
            ModuleContext.CreateDropCallIfDropFunctionExists(Builder, input.Type, builder => GetAddress(inputValueSource, builder));
            return true;
        }

        public bool VisitExplicitBorrowNode(ExplicitBorrowNode explicitBorrowNode)
        {
            VariableReference input = explicitBorrowNode.InputTerminals[0].GetTrueVariable(),
                output = explicitBorrowNode.OutputTerminals[0].GetTrueVariable();
            ValueSource inputSource = GetValueSource(input),
                outputSource = GetValueSource(output);
            if (inputSource != outputSource)
            {
                BorrowFromVariableIntoVariable(input, output);
            }
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
            LLVMValueRef targetFunction = GetImportedSynchronousFunction(methodCallNode);
            CreateCallForFunctionalNode(targetFunction, methodCallNode, methodCallNode.Signature);
            return true;
        }

        private LLVMTypeRef TranslateInitializeFunctionType(NIType functionType)
        {
            LLVMTypeRef[] parameterTypes = functionType.GetParameters().Select(Context.TranslateParameterType).ToArray();
            return LLVMSharp.LLVM.FunctionType(Context.VoidPointerType(), parameterTypes, false);
        }

        public bool VisitStructConstructorNode(StructConstructorNode structConstructorNode)
        {
            LLVMValueRef[] fieldValues = structConstructorNode.InputTerminals
                .Select(t => GetTerminalValueSource(t).GetValue(Builder))
                .ToArray();
            Terminal outputTerminal = structConstructorNode.OutputTerminals[0];
            LLVMValueRef structValue = Builder.BuildStructValue(
                Context.AsLLVMType(outputTerminal.GetTrueVariable().Type),
                fieldValues,
                "struct");
            Initialize(GetTerminalValueSource(outputTerminal), structValue);
            return true;
        }

        public bool VisitStructFieldAccessorNode(StructFieldAccessorNode structFieldAccessorNode)
        {
            NIType structType = structFieldAccessorNode.StructInputTerminal.GetTrueVariable().Type.GetReferentType();
            string[] structFieldNames = structType.GetFields().Select(f => f.GetName()).ToArray();
            LLVMValueRef structPtr = GetTerminalValueSource(structFieldAccessorNode.StructInputTerminal).GetValue(Builder);
            foreach (var pair in structFieldAccessorNode.FieldNames.Zip(structFieldAccessorNode.OutputTerminals))
            {
                string accessedFieldName = pair.Key;
                int accessedFieldIndex = structFieldNames.IndexOf(accessedFieldName);
                if (accessedFieldIndex == -1)
                {
                    throw new InvalidStateException("Field name not found in struct type: " + accessedFieldName);
                }
                LLVMValueRef structFieldPtr = Builder.CreateStructGEP(structPtr, (uint)accessedFieldIndex, accessedFieldName + "Ptr");

                Terminal outputTerminal = pair.Value;
                Initialize(GetTerminalValueSource(outputTerminal), structFieldPtr);
            }
            return true;
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
            private readonly Dictionary<Diagram, Tuple<LLVMBasicBlockRef, LLVMValueRef>> _tunnelDiagramInformation = new Dictionary<Diagram, Tuple<LLVMBasicBlockRef, LLVMValueRef>>();

            public void AddDiagramInformation(Diagram diagram, LLVMBasicBlockRef finalBasicBlock, LLVMValueRef finalValueAddress)
            {
                _tunnelDiagramInformation[diagram] = new Tuple<LLVMBasicBlockRef, LLVMValueRef>(finalBasicBlock, finalValueAddress);
            }

            public LLVMBasicBlockRef GetDiagramFinalBasicBlock(Diagram diagram) => _tunnelDiagramInformation[diagram].Item1;

            public LLVMValueRef GetDiagramFinalValueAddress(Diagram diagram) => _tunnelDiagramInformation[diagram].Item2;
        }

        private readonly Dictionary<Tunnel, TunnelInfo> _tunnelInfos = new Dictionary<Tunnel, TunnelInfo>();

        public bool VisitTunnel(Tunnel tunnel)
        {
            if (tunnel.Direction == Direction.Input)
            {
                // do nothing, because the input and outputs share ValueSources
            }
            else
            {
                if (tunnel.Terminals.HasExactly(2))
                {
                    // TODO: this currently happens in the structure's output BN group;
                    // instead it should happen in the inner diagram's terminal group.
                    VariableReference input = tunnel.InputTerminals[0].GetTrueVariable(),
                        output = tunnel.OutputTerminals[0].GetTrueVariable();
                    ValueSource inputValueSource = GetValueSource(input),
                        outputValueSource = GetValueSource(output);
                    if (output.Type == input.Type.CreateOption())
                    {
                        LLVMValueRef innerValue = inputValueSource.GetValue(Builder);
                        Initialize(outputValueSource, Context.BuildOptionValue(Builder, Context.AsLLVMType(output.Type), innerValue));
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
#if FALSE
                    var inputVariables = tunnel.InputTerminals.Select(VariableExtensions.GetTrueVariable);
                    var inputValuePtrs = new List<LLVMValueRef>();
                    var inputBasicBlocks = new List<LLVMBasicBlockRef>();
                    TunnelInfo tunnelInfo = _tunnelInfos[tunnel];
                    foreach (var inputTerminal in tunnel.InputTerminals)
                    {
                        inputValuePtrs.Add(tunnelInfo.GetDiagramFinalValueAddress(inputTerminal.ParentDiagram));
                        inputBasicBlocks.Add(tunnelInfo.GetDiagramFinalBasicBlock(inputTerminal.ParentDiagram));
                    }

                    var outputAllocation = (IUpdateableValueSource)GetTerminalValueSource(tunnel.OutputTerminals[0]);
                    LLVMValueRef tunnelValuePtr = Builder.CreatePhi(((IAddressableValueSource)outputAllocation).AddressType, "tunnelValuePtr");
                    tunnelValuePtr.AddIncoming(inputValuePtrs.ToArray(), inputBasicBlocks.ToArray(), (uint)inputValuePtrs.Count);
                    LLVMValueRef tunnelValue = Builder.CreateLoad(tunnelValuePtr, nameof(tunnelValue));
                    outputAllocation.UpdateValue(Builder, tunnelValue);
#endif
                }
            }
            return true;
        }

        bool IDfirNodeVisitor<bool>.VisitVariantConstructorNode(VariantConstructorNode variantConstructorNode)
        {
            var fieldIndex = (byte)variantConstructorNode.SelectedFieldIndex;
            LLVMTypeRef llvmVariantType = Context.AsLLVMType(variantConstructorNode.OutputTerminals[0].GetTrueVariable().Type);

            // TODO: if we have to allocate, we should allocate directly into the ValueSource. There should at least be a
            // ValueSource subclass that knows how to handle variant values.
            // Another idea is to generate one function per variant field that takes the field type and returns the constructed variant value.
            LLVMValueRef variantAlloca = Builder.CreateAlloca(llvmVariantType, "variantAlloca"),
                variantAllocaTagFieldPtr = Builder.CreateStructGEP(variantAlloca, 0u, "variantAllocaTagFieldPtr"),
                variantAllocaDataFieldPtr = Builder.CreateStructGEP(variantAlloca, 1u, "variantAllocaDataFieldPtr"),
                inputValue = GetValueSource(variantConstructorNode.InputTerminals[0].GetTrueVariable()).GetValue(Builder),
                bitCastAllocaFieldPtr = Builder.CreateBitCast(variantAllocaDataFieldPtr, LLVMTypeRef.PointerType(inputValue.TypeOf(), 0u), "bitCastAllocaFieldPtr");
            Builder.CreateStore(Context.AsLLVMValue(fieldIndex), variantAllocaTagFieldPtr);
            Builder.CreateStore(inputValue, bitCastAllocaFieldPtr);

            LLVMValueRef loadedVariant = Builder.CreateLoad(variantAlloca, "loadedVariant");
            Initialize(GetValueSource(variantConstructorNode.OutputTerminals[0].GetTrueVariable()), loadedVariant);
            return true;
        }

#region IInternalDfirNodeVisitor implementation

        bool IInternalDfirNodeVisitor<bool>.VisitAwaitNode(AwaitNode awaitNode)
        {
            // We can assume that an AwaitNode will be the first node in its AsyncStateGroup.
            // We will poll the promise; if it returns Some(T), we will drop the promise if necessary
            // and continue with the rest of the group, and otherwise return early.
            NIType promiseType = awaitNode.InputTerminal.GetTrueVariable().Type;
            NIType valueType = awaitNode.OutputTerminal.GetTrueVariable().Type;
            LLVMValueRef promisePollFunction = GetPromisePollFunction(promiseType);
            var promiseValueSource = (IAddressableValueSource)GetTerminalValueSource(awaitNode.InputTerminal);
            // TODO: create an additional LocalAllocationValueSource for this
            LLVMValueRef pollResultPtr = Builder.CreateAlloca(Context.AsLLVMType(valueType.CreateOption()), "pollResultPtr");

            LLVMValueRef bitCastCurrentGroupFunction = Builder.CreateBitCast(
                    _moduleBuilder.CurrentGroupData.Function,
                    LLVMTypeRef.PointerType(Context.ScheduledTaskFunctionType(), 0u),
                    "bitCastCurrentGroupFunction"),
                bitCastStatePtr = Builder.CreateBitCast(AllocationSet.StatePointer, Context.VoidPointerType(), "bitCastStatePtr"),
                waker = Builder.BuildStructValue(
                    Context.AsLLVMType(DataTypes.WakerType),
                    new LLVMValueRef[] { bitCastCurrentGroupFunction, bitCastStatePtr },
                    "waker");
            Builder.CreateCall(
                promisePollFunction,
                new LLVMValueRef[] { promiseValueSource.GetAddress(Builder), waker, pollResultPtr },
                string.Empty);
            LLVMValueRef pollResult = Builder.CreateLoad(pollResultPtr, "pollResult"),
                pollResultIsSome = Builder.CreateExtractValue(pollResult, 0u, "pollResultIsSome");
            LLVMBasicBlockRef promiseNotDoneBlock = CurrentFunction.AppendBasicBlock("promiseNotDone"),
                promiseDoneBlock = CurrentFunction.AppendBasicBlock("promiseDone");
            Builder.CreateCondBr(pollResultIsSome, promiseDoneBlock, promiseNotDoneBlock);

            Builder.PositionBuilderAtEnd(promiseNotDoneBlock);
            Builder.CreateRetVoid();

            Builder.PositionBuilderAtEnd(promiseDoneBlock);
            ModuleContext.CreateDropCallIfDropFunctionExists(Builder, promiseType, promiseValueSource.GetAddress);
            ValueSource outputValueSource = GetTerminalValueSource(awaitNode.OutputTerminal);
            var updateableOutputSource = outputValueSource as IUpdateableValueSource;
            var initializableOutputSource = outputValueSource as IInitializableValueSource;
            if (updateableOutputSource != null)
            {
                LLVMValueRef pollResultInnerValue = Builder.CreateExtractValue(pollResult, 1u, "pollResultInnerValue");
                updateableOutputSource.UpdateValue(Builder, pollResultInnerValue);
            }
            else if (initializableOutputSource != null)
            {
                LLVMValueRef pollResultInnerValue = Builder.CreateExtractValue(pollResult, 1u, "pollResultInnerValue");
                initializableOutputSource.InitializeValue(Builder, pollResultInnerValue);
            }

            return true;
        }

        private const uint MethodCallPromisePollFunctionPtrFieldIndex = 0u,
            MethodCallPromiseStatePtrFieldIndex = 1u,
            MethodCallPromiseOutputFieldIndex = 2u;

        bool IInternalDfirNodeVisitor<bool>.VisitCreateMethodCallPromise(CreateMethodCallPromise createMethodCallPromise)
        {
            LLVMValueRef promisePtr = GetAddress(GetTerminalValueSource(createMethodCallPromise.PromiseTerminal), Builder);
            // if the target may panic, then the promise's output field is a PanicResult whose first field will receive the actual output values.
            LLVMValueRef outputPtr = _calleesMayPanic[createMethodCallPromise.TargetName]
                ? Builder.CreateStructGEP(
                    Builder.CreateStructGEP(promisePtr, MethodCallPromiseOutputFieldIndex, "panicResultPtr"),
                    1u,
                    "outputPtr")
                : Builder.CreateStructGEP(promisePtr, MethodCallPromiseOutputFieldIndex, "outputPtr");

            var initializeParameters = new List<LLVMValueRef>();
            foreach (Terminal inputTerminal in createMethodCallPromise.InputTerminals)
            {
                initializeParameters.Add(GetTerminalValueSource(inputTerminal).GetValue(Builder));
            }

            NIType[] outputParameters = createMethodCallPromise.Signature.GetParameters()
                .Where(p => p.GetOutputParameterPassingRule() != NIParameterPassingRule.NotAllowed)
                .ToArray();
            switch (outputParameters.Length)
            {
                case 0:
                    break;
                case 1:
                    initializeParameters.Add(outputPtr);
                    break;
                default:
                    // for >1 parameters, the output field is a tuple of the output values
                    for (int i = 0; i < outputParameters.Length; ++i)
                    {
                        LLVMValueRef outputFieldPtr = Builder.CreateStructGEP(outputPtr, (uint)i, "outputFieldPtr");
                        initializeParameters.Add(outputFieldPtr);
                    }
                    break;
            }

            LLVMValueRef initializeStateFunction = GetImportedInitializeStateFunction(createMethodCallPromise),
                statePtr = Builder.CreateCall(initializeStateFunction, initializeParameters.ToArray(), "statePtr"),
                pollFunction = GetImportedPollFunction(createMethodCallPromise),
                promisePollStatePtr = Builder.CreateStructGEP(promisePtr, MethodCallPromiseStatePtrFieldIndex, "promisePollStatePtr"),
                promisePollFunctionPtr = Builder.CreateStructGEP(promisePtr, MethodCallPromisePollFunctionPtrFieldIndex, "promisePollFunctionPtr");
            Builder.CreateStore(pollFunction, promisePollFunctionPtr);
            Builder.CreateStore(statePtr, promisePollStatePtr);
            return true;
        }

        private static void BuildMethodCallPromisePollFunction(FunctionModuleContext moduleContext, NIType signature, LLVMValueRef methodCallPromisePollFunction)
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

        bool IInternalDfirNodeVisitor<bool>.VisitDecomposeStructNode(DecomposeStructNode decomposeStructNode)
        {
            ValueSource structSource = GetTerminalValueSource(decomposeStructNode.InputTerminals[0]);
            LLVMValueRef structValue = structSource.GetValue(Builder);
            uint fieldIndex = 0;
            foreach (Terminal outputTerminal in decomposeStructNode.OutputTerminals)
            {
                LLVMValueRef structFieldValue = Builder.CreateExtractValue(structValue, fieldIndex, $"structField_{fieldIndex}");
                Initialize(GetTerminalValueSource(outputTerminal), structFieldValue);
                ++fieldIndex;
            }
            return true;
        }
        
        bool IInternalDfirNodeVisitor<bool>.VisitPanicOrContinueNode(PanicOrContinueNode panicOrContinueNode)
        {
            LLVMValueRef panicResult = GetTerminalValueSource(panicOrContinueNode.InputTerminal).GetValue(Builder),
                shouldContinue = Builder.CreateExtractValue(panicResult, 0u, "shouldContinue");
            LLVMBasicBlockRef continueBlock = CurrentFunction.AppendBasicBlock($"continue_{panicOrContinueNode.UniqueId}"),
                panicBlock = CurrentFunction.AppendBasicBlock($"panic_{panicOrContinueNode.UniqueId}");
            Builder.CreateCondBr(shouldContinue, continueBlock, panicBlock);

            Builder.PositionBuilderAtEnd(panicBlock);
            _moduleBuilder.GenerateStoreCompletionState(RuntimeConstants.FunctionPanickedStatus);
            Builder.CreateBr(_moduleBuilder.CurrentGroupData.ExitBasicBlock);

            Builder.PositionBuilderAtEnd(continueBlock);
            LLVMValueRef result = Builder.CreateExtractValue(panicResult, 1u, "result");
            Initialize(GetTerminalValueSource(panicOrContinueNode.OutputTerminal), result);
            return true;
        }

#endregion

#region Frame

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
            }
            return true;
        }

        private void VisitFrameBeforeLeftBorderNodes(Frame frame)
        {
            if (frame.DoesStructureExecuteConditionally())
            {
                InitializeIfNecessary(GetValueSource(frame.GetConditionVariable()), builder => Context.AsLLVMValue(true));
            }
        }

        private void VisitFrameAfterLeftBorderNodes(Frame frame)
        {
            if (frame.DoesStructureExecuteConditionally())
            {
                LLVMValueRef condition = GetValueSource(frame.GetConditionVariable()).GetValue(Builder);
                Update(
                    _sharedData.VariableStorage.GetContinuationConditionVariable(_moduleBuilder.CurrentGroupData.AsyncStateGroup),
                    condition);
            }
        }

        public bool VisitUnwrapOptionTunnel(UnwrapOptionTunnel unwrapOptionTunnel)
        {
            var frame = (Frame)unwrapOptionTunnel.ParentStructure;
            ValueSource tunnelInputSource = GetTerminalValueSource(unwrapOptionTunnel.InputTerminals[0]),
                frameConditionValueSource = GetValueSource(frame.GetConditionVariable());
            LLVMValueRef inputOption = tunnelInputSource.GetValue(Builder),
                isSome = Builder.CreateExtractValue(inputOption, 0u, "isSome"),
                value = Builder.CreateExtractValue(inputOption, 1u, "value"),
                oldCondition = frameConditionValueSource.GetValue(Builder),
                newCondition = Builder.CreateAnd(oldCondition, isSome, "newCondition");
            Update(frameConditionValueSource, newCondition);

            Initialize(GetTerminalValueSource(unwrapOptionTunnel.OutputTerminals[0]), value);
            return true;
        }

        public bool VisitFrameSkippedBlockVisitation(FrameSkippedBlockVisitation visitation)
        {
            Frame frame = visitation.Frame;
            // Drop any input variables that may need it
            var variablesToDrop = new List<VariableReference>();
            foreach (var inputTunnel in frame.BorderNodes.OfType<Tunnel>().Where(tunnel => tunnel.Direction == Direction.Input))
            {
                variablesToDrop.Add(inputTunnel.OutputTerminals[0].GetTrueVariable());
            }

            var unwrapOptionTunnels = visitation.Frame.BorderNodes.OfType<UnwrapOptionTunnel>();
            if (unwrapOptionTunnels.HasMoreThan(1))
            {
                foreach (var unwrapOptionTunnel in unwrapOptionTunnels)
                {
                    variablesToDrop.Add(unwrapOptionTunnel.InputTerminals[0].GetTrueVariable());
                }
            }

            foreach (VariableReference variableToDrop in variablesToDrop)
            {
                ModuleContext.CreateDropCallIfDropFunctionExists(Builder, variableToDrop.Type, builder => GetAddress(GetValueSource(variableToDrop), builder));
            }

            // Initialize any output variables to None
            foreach (Tunnel tunnel in frame.BorderNodes.OfType<Tunnel>().Where(t => t.Direction == Direction.Output))
            {
                // TODO: for now, this means that these tunnels require local allocations.
                // It would be nicer to allow them to be Phi values--i.e., ValueSources that can be
                // initialized by values from different predecessor blocks, but may not change
                // after initialization.
                VariableReference outputVariable = tunnel.OutputTerminals[0].GetTrueVariable();
                ValueSource outputSource = GetValueSource(outputVariable);
                LLVMTypeRef outputType = Context.AsLLVMType(outputVariable.Type);
                Update(outputSource, LLVMSharp.LLVM.ConstNull(outputType));
            }

            return true;
        }

        #endregion

        #region Loop

        private ValueSource GetConditionAllocationSource(Compiler.Nodes.Loop loop)
        {
            LoopConditionTunnel loopCondition = loop.BorderNodes.OfType<LoopConditionTunnel>().First();
            Terminal loopConditionInput = loopCondition.InputTerminals[0];
            return GetTerminalValueSource(loopConditionInput);
        }

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
            }
            return true;
        }

        private void VisitLoopBeforeLeftBorderNodes(Compiler.Nodes.Loop loop)
        {
            LoopConditionTunnel loopCondition = loop.BorderNodes.OfType<LoopConditionTunnel>().First();
            Terminal loopConditionInput = loopCondition.InputTerminals[0];

            if (!loopConditionInput.IsConnected)
            {
                InitializeIfNecessary(GetConditionAllocationSource(loop), _ => Context.AsLLVMValue(true));
            }

            // initialize all output tunnels with None values, in case the loop interior does not execute
            foreach (Tunnel outputTunnel in loop.BorderNodes.OfType<Tunnel>().Where(tunnel => tunnel.Direction == Direction.Output))
            {
                // TODO: this requires these tunnels to have local allocations for now.
                // As with output tunnels of conditionally-executing Frames, it would be nice
                // to treat these as Phi ValueSources.
                VariableReference tunnelOutputVariable = outputTunnel.OutputTerminals[0].GetTrueVariable();
                ValueSource tunnelOutputSource = GetValueSource(tunnelOutputVariable);
                LLVMTypeRef tunnelOutputType = Context.AsLLVMType(tunnelOutputVariable.Type);
                Update(tunnelOutputSource, LLVMSharp.LLVM.ConstNull(tunnelOutputType));
            }
        }

        private void VisitLoopAfterLeftBorderNodes(Compiler.Nodes.Loop loop)
        {
            LLVMValueRef condition = GetConditionAllocationSource(loop).GetValue(Builder);
            Update(
                _sharedData.VariableStorage.GetContinuationConditionVariable(_moduleBuilder.CurrentGroupData.AsyncStateGroup),
                condition);
        }

        public bool VisitLoopConditionTunnel(LoopConditionTunnel loopConditionTunnel)
        {
            return true;
        }

        public bool VisitIterateTunnel(IterateTunnel iterateTunnel)
        {
            ValueSource iteratorSource = GetTerminalValueSource(iterateTunnel.InputTerminals[0]);
            ValueSource itemSource = GetTerminalValueSource(iterateTunnel.OutputTerminals[0]);
            var intermediateOptionSource = _sharedData.VariableStorage.GetAdditionalValueSource(iterateTunnel.IntermediateValueName);
            Terminal inputTerminal = iterateTunnel.InputTerminals[0];

            NIType iteratorType = inputTerminal.GetTrueVariable().Type.GetReferentType();
            LLVMValueRef iteratorNextFunction = GetIteratorNextFunction(iteratorType, iterateTunnel.IteratorNextFunctionType.FunctionNIType);
            Builder.CreateCall(
                iteratorNextFunction,
                new LLVMValueRef[]
                {
                    iteratorSource.GetValue(Builder),
                    GetAddress(intermediateOptionSource, Builder)
                },
                string.Empty);
            LLVMValueRef itemOption = intermediateOptionSource.GetValue(Builder),
                isSome = Builder.CreateExtractValue(itemOption, 0u, "isSome"),
                item = Builder.CreateExtractValue(itemOption, 1u, "item");

            // &&= the loop condition with the isSome value
            var loop = (Compiler.Nodes.Loop)iterateTunnel.ParentStructure;
            ValueSource loopConditionAllocationSource = GetConditionAllocationSource(loop);
            LLVMValueRef condition = loopConditionAllocationSource.GetValue(Builder);
            LLVMValueRef conditionAndIsSome = Builder.CreateAnd(condition, isSome, "conditionAndIsSome");
            Update(loopConditionAllocationSource, conditionAndIsSome);

            // bind the inner value to the output tunnel
            Initialize(itemSource, item);
            return true;
        }

        private LLVMValueRef GetIteratorNextFunction(NIType iteratorType, NIType iteratorNextSignature)
        {
            if (iteratorType == DataTypes.RangeIteratorType)
            {
                return GetImportedCommonFunction(CommonModules.RangeIteratorNextName);
            }
            if (iteratorType.IsStringSplitIteratorType())
            {
                return GetImportedCommonFunction(CommonModules.StringSplitIteratorNextName);
            }

            throw new NotSupportedException("Missing Iterator::Next method for type " + iteratorType);
        }

#endregion

#region Option Pattern Structure

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
            }
            return true;
        }

        private void VisitOptionPatternStructureBeforeLeftBorderNodes(OptionPatternStructure optionPatternStructure)
        {
            ValueSource selectorInputAllocationSource = GetTerminalValueSource(optionPatternStructure.Selector.InputTerminals[0]);
            LLVMValueRef option = selectorInputAllocationSource.GetValue(Builder);
            LLVMValueRef isSome = Builder.CreateExtractValue(option, 0, "isSome"),
                isNone = Builder.CreateNot(isSome, "isNone");
            Update(
                _sharedData.VariableStorage.GetContinuationConditionVariable(_moduleBuilder.CurrentGroupData.AsyncStateGroup),
                isNone);
        }

        private void VisitOptionPatternStructureBeforeDiagram(OptionPatternStructure optionPatternStructure, Diagram diagram)
        {
            // TODO: this should be in a diagram-specific VisitOptionPatternStructureSelector
            if (diagram == optionPatternStructure.Diagrams[0])
            {
                DestructureSelectorValueInSomeCase(optionPatternStructure.Selector);
            }
        }

        private void VisitOptionPatternStructureAfterDiagram(OptionPatternStructure optionPatternStructure, Diagram diagram)
        {
            LLVMBasicBlockRef currentBlock = Builder.GetInsertBlock();
            foreach (Tunnel outputTunnel in optionPatternStructure.Tunnels.Where(tunnel => tunnel.Direction == Direction.Output))
            {
                Terminal inputTerminal = outputTunnel.InputTerminals.First(t => t.ParentDiagram == diagram);
                ValueSource inputTerminalValueSource = GetTerminalValueSource(inputTerminal);
                ValueSource outputTerminalValueSource = GetTerminalValueSource(outputTunnel.OutputTerminals[0]);
                // TODO: these Tunnel output variables should also be able to be Phi ValueSources
                Update(outputTerminalValueSource, inputTerminalValueSource.GetValue(Builder));
            }
        }

        public bool VisitOptionPatternStructureSelector(OptionPatternStructureSelector optionPatternStructureSelector)
        {
            return true;
        }

        private void DestructureSelectorValueInSomeCase(OptionPatternStructureSelector optionPatternStructureSelector)
        {
            ValueSource selectorInputAllocationSource = GetTerminalValueSource(optionPatternStructureSelector.InputTerminals[0]);
            ValueSource selectorOutputSource = GetTerminalValueSource(optionPatternStructureSelector.OutputTerminals[0]);
            LLVMValueRef option = selectorInputAllocationSource.GetValue(Builder),
                innerValue = Builder.CreateExtractValue(option, 1, "innerValue");
            Initialize(selectorOutputSource, innerValue);
        }

        #endregion

#region Variant Match Structure

        bool IDfirStructureVisitor<bool>.VisitVariantMatchStructure(VariantMatchStructure variantMatchStructure, StructureTraversalPoint traversalPoint, Diagram nestedDiagram)
        {
            switch (traversalPoint)
            {
                case StructureTraversalPoint.BeforeLeftBorderNodes:
                    VisitVariantMatchStructureBeforeLeftBorderNodes(variantMatchStructure);
                    break;
                case StructureTraversalPoint.AfterLeftBorderNodesAndBeforeDiagram:
                    VisitVariantMatchStructureBeforeDiagram(variantMatchStructure, nestedDiagram);
                    break;
                case StructureTraversalPoint.AfterDiagram:
                    VisitVariantMatchStructureAfterDiagram(variantMatchStructure, nestedDiagram);
                    break;
            }
            return true;
        }

        private void VisitVariantMatchStructureBeforeLeftBorderNodes(VariantMatchStructure variantMatchStructure)
        {
            ValueSource selectorInputSource = GetTerminalValueSource(variantMatchStructure.Selector.InputTerminals[0]);
            LLVMValueRef variant = selectorInputSource.GetValue(Builder);
            LLVMValueRef tag = Builder.CreateExtractValue(variant, 0, "tag");

            LLVMValueRef conditionValue = tag;
            if (variantMatchStructure.Diagrams.Count <= 2)
            {
                // TODO: just for now, truncate the tag to an i1. This will need to be fixed in order to handle >2 cases.
                conditionValue = Builder.CreateTrunc(tag, Context.Int1Type, "truncatedTag");
            }

            Update(
                _sharedData.VariableStorage.GetContinuationConditionVariable(_moduleBuilder.CurrentGroupData.AsyncStateGroup),
                conditionValue);
        }

        private void VisitVariantMatchStructureBeforeDiagram(VariantMatchStructure variantMatchStructure, Diagram diagram)
        {
            // TODO: this should be in a diagram-specific VisitOptionPatternStructureSelector
            int diagramIndex = variantMatchStructure.Diagrams.IndexOf(diagram);
            DestructureSelectorValueWithTag(variantMatchStructure.Selector, diagramIndex);
        }

        private void VisitVariantMatchStructureAfterDiagram(VariantMatchStructure variantMatchStructure, Diagram diagram)
        {
            LLVMBasicBlockRef currentBlock = Builder.GetInsertBlock();
            foreach (Tunnel outputTunnel in variantMatchStructure.Tunnels.Where(tunnel => tunnel.Direction == Direction.Output))
            {
                Terminal inputTerminal = outputTunnel.InputTerminals.First(t => t.ParentDiagram == diagram);
                ValueSource inputTerminalValueSource = GetTerminalValueSource(inputTerminal);
                ValueSource outputTerminalValueSource = GetTerminalValueSource(outputTunnel.OutputTerminals[0]);
                // TODO: these Tunnel output variables should also be able to be Phi ValueSources
                Update(outputTerminalValueSource, inputTerminalValueSource.GetValue(Builder));
            }
        }

        bool IDfirNodeVisitor<bool>.VisitVariantMatchStructureSelector(VariantMatchStructureSelector variantMatchStructureSelector)
        {
            return true;
        }

        private void DestructureSelectorValueWithTag(VariantMatchStructureSelector variantMatchStructureSelector, int diagramIndex)
        {
            ValueSource selectorInputAllocationSource = GetTerminalValueSource(variantMatchStructureSelector.InputTerminals[0]);
            Terminal outputTerminal = variantMatchStructureSelector.OutputTerminals[diagramIndex];
            ValueSource selectorOutputSource = GetTerminalValueSource(outputTerminal);
            // TODO: if the outer structure and diagram initial blocks are all in the same function, we should be able
            // to stash this read somewhere and reuse it
            LLVMValueRef variant = selectorInputAllocationSource.GetValue(Builder);
            LLVMValueRef alloca = Builder.CreateAlloca(variant.TypeOf().GetStructElementTypes()[1], "alloca");
            LLVMValueRef genericTagValue = Builder.CreateExtractValue(variant, 1, "genericTagValue");
            Builder.CreateStore(genericTagValue, alloca);
            LLVMTypeRef elementLLVMType = Context.AsLLVMType(outputTerminal.GetTrueVariable().Type);
            LLVMValueRef bitCastAlloca = Builder.CreateBitCast(alloca, LLVMTypeRef.PointerType(elementLLVMType, 0u), "bitCastAlloca");
            LLVMValueRef loadedElement = Builder.CreateLoad(bitCastAlloca, "loadedElement");
            Initialize(selectorOutputSource, loadedElement);
        }

        internal static void BuildVariantDropFunction(FunctionModuleContext moduleContext, NIType signature, LLVMValueRef variantDropFunction)
        {
            NIType variantType = signature.GetGenericParameters().First();
            Tuple<NIType, int>[] droppableFields = variantType.GetFields()
                .Select((field, i) => new Tuple<NIType, int>(field, i))
                .Where(field => field.Item1.GetDataType().TypeHasDropTrait()).ToArray();
            LLVMBasicBlockRef entryBlock = variantDropFunction.AppendBasicBlock("entry");
            Tuple<LLVMBasicBlockRef, int>[] dropBlocks = droppableFields.Select(field => new Tuple<LLVMBasicBlockRef, int>(variantDropFunction.AppendBasicBlock($"drop{field.Item1.GetName()}"), field.Item2)).ToArray();
            LLVMBasicBlockRef exitBlock = variantDropFunction.AppendBasicBlock("exit");

            var builder = moduleContext.LLVMContext.CreateIRBuilder();

            builder.PositionBuilderAtEnd(entryBlock);
            LLVMValueRef variantPtr = variantDropFunction.GetParam(0u),
                variantTagPtr = builder.CreateStructGEP(variantPtr, 0u, "variantTagPtr"),
                variantDataPtr = builder.CreateStructGEP(variantPtr, 1u, "variantDataPtr"),
                variantTag = builder.CreateLoad(variantTagPtr, "variantTag"),
                tagSwitch = builder.CreateSwitch(variantTag, exitBlock, (uint)dropBlocks.Length);
            foreach (var pair in dropBlocks)
            {
                tagSwitch.AddCase(moduleContext.LLVMContext.AsLLVMValue((byte)pair.Item2), pair.Item1);
            }

            for (int i = 0; i < droppableFields.Length; ++i)
            {
                builder.PositionBuilderAtEnd(dropBlocks[i].Item1);
                NIType fieldType = droppableFields[i].Item1.GetDataType();
                LLVMTypeRef fieldLLVMType = moduleContext.LLVMContext.AsLLVMType(fieldType);
                LLVMValueRef bitCastFieldPtr = builder.CreateBitCast(variantDataPtr, LLVMTypeRef.PointerType(fieldLLVMType, 0u), "bitCastFieldPtr");
                moduleContext.CreateDropCallIfDropFunctionExists(builder, fieldType, _ => bitCastFieldPtr);
                builder.CreateBr(exitBlock);
            }

            builder.PositionBuilderAtEnd(exitBlock);
            builder.CreateRetVoid();
        }

        #endregion
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
