using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using LLVMSharp;
using NationalInstruments;
using NationalInstruments.CommonModel;
using NationalInstruments.DataTypes;
using NationalInstruments.Dfir;
using NationalInstruments.ExecutionFramework;
using Rebar.Common;
using Rebar.Compiler;
using Rebar.Compiler.Nodes;
using Rebar.RebarTarget.LLVM;
using Rebar.RebarTarget.LLVM.CodeGen;

namespace Rebar.RebarTarget
{
    internal sealed class CodeGenExpander :
        IDfirNodeVisitor<IEnumerable<Visitation>>,
        IDfirStructureVisitor<IEnumerable<Visitation>>,
        IInternalDfirNodeVisitor<IEnumerable<Visitation>>,
        IVisitationHandler<IEnumerable<Visitation>>
    {
        #region FunctionalNode expanders

        private delegate IEnumerable<CodeGenElement> FunctionalNodeExpander(FunctionalNode node, CodeGenExpander codeGenExpander);

        private static readonly Dictionary<string, FunctionalNodeExpander> _functionalNodeExpanders;

        private static IEnumerable<CodeGenElement> ExpandEmpty(FunctionalNode node, CodeGenExpander codeGenExpander) => Enumerable.Empty<CodeGenElement>();

        private static IEnumerable<CodeGenElement> ExpandInspect(FunctionalNode inspectNode, CodeGenExpander codeGenExpander)
        {
            VariableReference inputVariable = inspectNode.InputTerminals[0].GetTrueVariable();

            // define global data in module for inspected value
            LLVMTypeRef globalType = codeGenExpander.Context.AsLLVMType(inputVariable.Type.GetReferentType());
            string globalName = $"inspect_{inspectNode.UniqueId}";
            LLVMValueRef globalAddress = codeGenExpander.ModuleContext.Module.AddGlobal(globalType, globalName);
            // Setting an initializer is necessary to distinguish this from an externally-defined global
            globalAddress.SetInitializer(LLVMSharp.LLVM.ConstNull(globalType));

            // load the input dereference value and store it in the global
            int index = codeGenExpander.ReserveIndices(1);
            yield return new GetDereferencedValue(inputVariable, index);
            yield return new Op((builder, valueStorage) => builder.CreateStore(valueStorage[index], globalAddress));
        }

        private static IEnumerable<CodeGenElement> ExpandOutput(FunctionalNode outputNode, CodeGenExpander codeGenExpander)
        {
            VariableReference input = outputNode.InputTerminals[0].GetTrueVariable();
            NIType referentType = input.Type.GetReferentType();
            FunctionImporter functionImporter = codeGenExpander.ModuleContext.FunctionImporter;
            if (referentType.IsBoolean())
            {
                int index = codeGenExpander.ReserveIndices(1);
                yield return new GetDereferencedValue(input, index);
                yield return new Call(
                    functionImporter.GetImportedCommonFunction(CommonModules.OutputBoolName),
                    new int[] { index });
                yield break;
            }
            if (referentType.IsInteger())
            {
                LLVMValueRef outputFunction;
                switch (referentType.GetKind())
                {
                    case NITypeKind.Int8:
                        outputFunction = functionImporter.GetImportedCommonFunction(CommonModules.OutputInt8Name);
                        break;
                    case NITypeKind.UInt8:
                        outputFunction = functionImporter.GetImportedCommonFunction(CommonModules.OutputUInt8Name);
                        break;
                    case NITypeKind.Int16:
                        outputFunction = functionImporter.GetImportedCommonFunction(CommonModules.OutputInt16Name);
                        break;
                    case NITypeKind.UInt16:
                        outputFunction = functionImporter.GetImportedCommonFunction(CommonModules.OutputUInt16Name);
                        break;
                    case NITypeKind.Int32:
                        outputFunction = functionImporter.GetImportedCommonFunction(CommonModules.OutputInt32Name);
                        break;
                    case NITypeKind.UInt32:
                        outputFunction = functionImporter.GetImportedCommonFunction(CommonModules.OutputUInt32Name);
                        break;
                    case NITypeKind.Int64:
                        outputFunction = functionImporter.GetImportedCommonFunction(CommonModules.OutputInt64Name);
                        break;
                    case NITypeKind.UInt64:
                        outputFunction = functionImporter.GetImportedCommonFunction(CommonModules.OutputUInt64Name);
                        break;
                    default:
                        throw new NotImplementedException($"Don't know how to display type {referentType} yet.");
                }
                int index = codeGenExpander.ReserveIndices(1);
                yield return new GetDereferencedValue(input, index);
                yield return new Call(outputFunction, new int[] { index });
                yield break;
            }
            if (referentType.IsString())
            {
                // TODO: this should go away once auto-borrowing into string slices works
                // call output_string with string pointer and size
                int index = codeGenExpander.ReserveIndices(1);
                yield return new GetValue(input, index);
                yield return new Op((builder, valueStorage) =>
                {
                    LLVMValueRef stringPtr = valueStorage[index],
                        stringSlice = builder.CreateCall(
                            functionImporter.GetImportedCommonFunction(CommonModules.StringToSliceRetName),
                            new LLVMValueRef[] { stringPtr },
                            "stringSlice");
                    builder.CreateCall(
                        functionImporter.GetImportedCommonFunction(CommonModules.OutputStringSliceName),
                        new LLVMValueRef[] { stringSlice },
                        string.Empty);
                });
                yield break;
            }
            if (referentType == DataTypes.StringSliceType)
            {
                int index = codeGenExpander.ReserveIndices(1);
                yield return new GetValue(input, index);
                yield return new Call(
                    functionImporter.GetImportedCommonFunction(CommonModules.OutputStringSliceName),
                    new int[] { index });
                yield break;
            }
            else
            {
                throw new NotImplementedException($"Don't know how to display type {referentType} yet.");
            }
        }

        private static IEnumerable<CodeGenElement> ExpandAssign(FunctionalNode node, CodeGenExpander codeGenExpander)
        {
            VariableReference assigneeVariable = node.InputTerminals[0].GetTrueVariable();
            LLVMValueRef dropFunction;
            if (TraitHelpers.TryGetDropFunction(assigneeVariable.Type.GetReferentType(), codeGenExpander.ModuleContext, out dropFunction))
            {
                int addressIndex = codeGenExpander.ReserveIndices(1);
                yield return new GetValue(assigneeVariable, addressIndex);
                yield return new Call(dropFunction, new int[] { addressIndex });
            }

            VariableReference newValueVariable = node.InputTerminals[1].GetTrueVariable();
            int valueIndex = codeGenExpander.ReserveIndices(1);
            yield return new GetValue(newValueVariable, valueIndex);
            yield return new UpdateDereferencedValue(assigneeVariable, valueIndex);
        }

        private static IEnumerable<CodeGenElement> ExpandCreateCopy(FunctionalNode node, CodeGenExpander codeGenExpander)
        {
            VariableReference input0Variable = node.InputTerminals[0].GetTrueVariable(),
                outputVariable = node.OutputTerminals[1].GetTrueVariable();
            NIType valueType = input0Variable.Type.GetReferentType();
            if (valueType.WireTypeMayFork())
            {
                int index = codeGenExpander.ReserveIndices(1);
                yield return new GetDereferencedValue(input0Variable, index);
                yield return new InitializeValue(outputVariable, index);
                yield break;
            }

            LLVMValueRef cloneFunction;
            if (codeGenExpander.ModuleContext.TryGetCloneFunction(valueType, out cloneFunction))
            {
                // TODO: create helper that expands a FunctionalNode into a Call
                int startIndex = codeGenExpander.ReserveIndices(2);
                yield return new GetValue(input0Variable, startIndex);
                yield return new GetAddress(outputVariable, startIndex + 1, forInitialize: true);
                yield return new Call(cloneFunction, new int[] { startIndex, startIndex + 1 });
                yield break;
            }

            throw new NotSupportedException("Don't know how to compile CreateCopy for type " + valueType);
        }

        private static IEnumerable<CodeGenElement> ExpandExchange(FunctionalNode node, CodeGenExpander codeGenExpander)
        {
            VariableReference variable0 = node.InputTerminals[0].GetTrueVariable(),
                variable1 = node.InputTerminals[1].GetTrueVariable();
            int startIndex = codeGenExpander.ReserveIndices(2);
            yield return new GetDereferencedValue(variable0, startIndex);
            yield return new GetDereferencedValue(variable1, startIndex + 1);
            yield return new UpdateDereferencedValue(variable0, startIndex + 1);
            yield return new UpdateDereferencedValue(variable1, startIndex);
        }

        private static IEnumerable<CodeGenElement> ExpandSelectReference(FunctionalNode node, CodeGenExpander codeGenExpander)
        {
            int startIndex = codeGenExpander.ReserveIndices(4);
            yield return new GetDereferencedValue(node.InputTerminals[0].GetTrueVariable(), startIndex);
            yield return new GetValue(node.InputTerminals[1].GetTrueVariable(), startIndex + 1);
            yield return new GetValue(node.InputTerminals[2].GetTrueVariable(), startIndex + 2);
            yield return new Op((builder, valueStorage) =>
            {
                valueStorage[startIndex + 3] = builder.CreateSelect(valueStorage[startIndex], valueStorage[startIndex + 1], valueStorage[startIndex + 2], "select");
            });
            yield return new InitializeValue(node.OutputTerminals[1].GetTrueVariable(), startIndex + 3);
        }

        private static IEnumerable<CodeGenElement> ExpandSomeConstructor(FunctionalNode someConstructorNode, CodeGenExpander codeGenExpander)
        {
            VariableReference inputVariable = someConstructorNode.InputTerminals[0].GetTrueVariable(),
                outputVariable = someConstructorNode.OutputTerminals[0].GetTrueVariable();
            LLVMTypeRef outputType = codeGenExpander.Context.AsLLVMType(someConstructorNode.OutputTerminals[0].GetTrueVariable().Type);
            int startIndex = codeGenExpander.ReserveIndices(2);
            // TODO: could be MoveValue
            yield return new GetValue(inputVariable, startIndex);
            yield return new Op((builder, valueStorage) =>
            {
                valueStorage[startIndex + 1] = codeGenExpander.Context.BuildOptionValue(builder, outputType, valueStorage[startIndex]);
            });
            yield return new InitializeValue(outputVariable, startIndex + 1);
        }

        private static IEnumerable<CodeGenElement> ExpandNoneConstructor(FunctionalNode noneConstructorNode, CodeGenExpander codeGenExpander)
        {
            VariableReference outputVariable = noneConstructorNode.OutputTerminals[0].GetTrueVariable();
            LLVMTypeRef outputType = codeGenExpander.Context.AsLLVMType(outputVariable.Type);
            int index = codeGenExpander.ReserveIndices(1);
            yield return new GetConstant((moduleContext, builder) => moduleContext.LLVMContext.BuildOptionValue(builder, outputType, null), index);
            yield return new InitializeValue(outputVariable, index);
        }

        private static FunctionalNodeExpander CreatePureUnaryOperationExpander(Func<IRBuilder, LLVMValueRef, LLVMValueRef> generateOperation)
        {
            return (_, __) => ExpandUnaryOperation(_, __, generateOperation, false);
        }

        private static FunctionalNodeExpander CreateMutatingUnaryOperationExpander(Func<IRBuilder, LLVMValueRef, LLVMValueRef> generateOperation)
        {
            return (_, __) => ExpandUnaryOperation(_, __, generateOperation, true);
        }

        private static IEnumerable<CodeGenElement> ExpandUnaryOperation(
            FunctionalNode operationNode,
            CodeGenExpander codeGenExpander,
            Func<IRBuilder, LLVMValueRef, LLVMValueRef> generateOperation,
            bool mutating)
        {
            int startIndex = codeGenExpander.ReserveIndices(2);
            VariableReference inputVariable = operationNode.InputTerminals[0].GetTrueVariable();
            yield return new GetDereferencedValue(inputVariable, startIndex);
            yield return new Op((builder, valueStorage) =>
            {
                valueStorage[startIndex + 1] = generateOperation(builder, valueStorage[startIndex]);
            });
            if (!mutating)
            {
                yield return new InitializeValue(operationNode.OutputTerminals[1].GetTrueVariable(), startIndex + 1);
            }
            else
            {
                yield return new UpdateDereferencedValue(inputVariable, startIndex + 1);
            }
        }

        private static FunctionalNodeExpander CreatePureBinaryOperationExpander(Func<IRBuilder, LLVMValueRef, LLVMValueRef, LLVMValueRef> generateOperation)
        {
            return (_, __) => ExpandBinaryOperation(_, __, generateOperation, false);
        }

        private static FunctionalNodeExpander CreateMutatingBinaryOperationExpander(Func<IRBuilder, LLVMValueRef, LLVMValueRef, LLVMValueRef> generateOperation)
        {
            return (_, __) => ExpandBinaryOperation(_, __, generateOperation, true);
        }

        private static IEnumerable<CodeGenElement> ExpandBinaryOperation(
            FunctionalNode node,
            CodeGenExpander codeGenExpander,
            Func<IRBuilder, LLVMValueRef, LLVMValueRef, LLVMValueRef> generateOperation,
            bool mutating)
        {
            VariableReference leftVariable = node.InputTerminals[0].GetTrueVariable(),
                rightVariable = node.InputTerminals[1].GetTrueVariable();
            int startIndex = codeGenExpander.ReserveIndices(3);
            yield return new GetDereferencedValue(leftVariable, startIndex);
            yield return new GetDereferencedValue(rightVariable, startIndex + 1);
            yield return new Op((builder, storage) =>
            {
                storage[startIndex + 2] = generateOperation(builder, storage[startIndex], storage[startIndex + 1]);
            });
            if (!mutating)
            {
                yield return new InitializeValue(node.OutputTerminals[2].GetTrueVariable(), startIndex + 2);
            }
            else
            {
                yield return new UpdateDereferencedValue(leftVariable, startIndex + 2);
            }
        }

        private static IEnumerable<CodeGenElement> ExpandIncrement(FunctionalNode increment, CodeGenExpander codeGenExpander)
        {
            int startIndex = codeGenExpander.ReserveIndices(3);
            return new List<CodeGenElement>()
            {
                new GetDereferencedValue(increment.InputTerminals[0].GetTrueVariable(), startIndex),
                new GetConstant((moduleContext, builder) => moduleContext.LLVMContext.AsLLVMValue(1), startIndex + 1),
                new Op((builder, valueStorage) => valueStorage[startIndex + 2] = builder.CreateAdd(valueStorage[startIndex], valueStorage[startIndex + 1], "increment")),
                new InitializeValue(increment.OutputTerminals[1].GetTrueVariable(), startIndex + 2)
            };
        }

        private static IEnumerable<CodeGenElement> ExpandAccumulateIncrement(FunctionalNode increment, CodeGenExpander codeGenExpander)
        {
            int startIndex = codeGenExpander.ReserveIndices(3);
            return new List<CodeGenElement>()
            {
                new GetDereferencedValue(increment.InputTerminals[0].GetTrueVariable(), startIndex),
                new GetConstant((moduleContext, builder) => moduleContext.LLVMContext.AsLLVMValue(1), startIndex + 1),
                new Op((builder, valueStorage) => valueStorage[startIndex + 2] = builder.CreateAdd(valueStorage[startIndex], valueStorage[startIndex + 1], "increment")),
                new UpdateDereferencedValue(increment.InputTerminals[0].GetTrueVariable(), startIndex + 2)
            };
        }

        private static FunctionalNodeExpander CreateImportedCommonFunctionExpander(string functionName)
        {
            return (functionalNode, codeGenExpander) =>
                codeGenExpander.ExpandCallForFunctionalNode(
                    codeGenExpander.ModuleContext.FunctionImporter.GetImportedCommonFunction(functionName),
                    functionalNode,
                    functionalNode.Signature);
        }

        private static FunctionalNodeExpander CreateSpecializedFunctionCallExpander(Action<FunctionModuleContext, NIType, LLVMValueRef> functionCreator)
        {
            return (functionalNode, codeGenExpander) =>
            {
                NIType signature = functionalNode.FunctionType.FunctionNIType;
                return codeGenExpander.ExpandCallForFunctionalNode(
                    codeGenExpander.ModuleContext.GetSpecializedFunctionWithSignature(signature, functionCreator),
                    functionalNode,
                    signature);
            };
        }

        private IEnumerable<CodeGenElement> ExpandCallForFunctionalNode(LLVMValueRef function, FunctionalNode node)
        {
            return ExpandCallForFunctionalNode(function, node, node.Signature);
        }

        private IEnumerable<CodeGenElement> ExpandCallForFunctionalNode(LLVMValueRef function, Node node, NIType nodeFunctionSignature)
        {
            Signature nodeSignature = Signatures.GetSignatureForNIType(nodeFunctionSignature);
            int indexCount = node.InputTerminals.Count + nodeSignature.Outputs.Where(output => !output.IsPassthrough).Count();
            int startIndex = ReserveIndices(indexCount);
            int i = startIndex;
            foreach (Terminal inputTerminal in node.InputTerminals)
            {
                yield return new GetValue(inputTerminal.GetTrueVariable(), i);
                ++i;
            }
            foreach (var outputPair in node.OutputTerminals.Zip(nodeSignature.Outputs))
            {
                if (outputPair.Value.IsPassthrough)
                {
                    continue;
                }
                yield return new GetAddress(outputPair.Key.GetTrueVariable(), i, forInitialize: true);
                ++i;
            }
            yield return new Call(function, Enumerable.Range(startIndex, indexCount).ToArray());
        }

        #endregion

        static CodeGenExpander()
        {
            _functionalNodeExpanders = new Dictionary<string, FunctionalNodeExpander>();

            _functionalNodeExpanders["ImmutPass"] = ExpandEmpty;
            _functionalNodeExpanders["MutPass"] = ExpandEmpty;
            _functionalNodeExpanders["Inspect"] = ExpandInspect;
            _functionalNodeExpanders["FakeDropCreate"] = CreateImportedCommonFunctionExpander(CommonModules.FakeDropCreateName);
            _functionalNodeExpanders["Output"] = ExpandOutput;

            _functionalNodeExpanders["Assign"] = ExpandAssign;
            _functionalNodeExpanders["CreateCopy"] = ExpandCreateCopy;
            _functionalNodeExpanders["Exchange"] = ExpandExchange;
            _functionalNodeExpanders["SelectReference"] = ExpandSelectReference;

            _functionalNodeExpanders["Add"] = CreatePureBinaryOperationExpander((builder, left, right) => builder.CreateAdd(left, right, "add"));
            _functionalNodeExpanders["Subtract"] = CreatePureBinaryOperationExpander((builder, left, right) => builder.CreateSub(left, right, "subtract"));
            _functionalNodeExpanders["Multiply"] = CreatePureBinaryOperationExpander((builder, left, right) => builder.CreateMul(left, right, "multiply"));
            _functionalNodeExpanders["Divide"] = CreatePureBinaryOperationExpander((builder, left, right) => builder.CreateSDiv(left, right, "divide"));
            _functionalNodeExpanders["Increment"] = ExpandIncrement;
            _functionalNodeExpanders["Modulus"] = CreatePureBinaryOperationExpander((builder, left, right) => builder.CreateSRem(left, right, "modulus"));
            _functionalNodeExpanders["And"] = CreatePureBinaryOperationExpander((builder, left, right) => builder.CreateAnd(left, right, "and"));
            _functionalNodeExpanders["Or"] = CreatePureBinaryOperationExpander((builder, left, right) => builder.CreateOr(left, right, "or"));
            _functionalNodeExpanders["Xor"] = CreatePureBinaryOperationExpander((builder, left, right) => builder.CreateXor(left, right, "xor"));
            _functionalNodeExpanders["Not"] = CreatePureUnaryOperationExpander((builder, value) => builder.CreateNot(value, "not"));

            _functionalNodeExpanders["AccumulateAdd"] = CreateMutatingBinaryOperationExpander((builder, left, right) => builder.CreateAdd(left, right, "add"));
            _functionalNodeExpanders["AccumulateSubtract"] = CreateMutatingBinaryOperationExpander((builder, left, right) => builder.CreateSub(left, right, "subtract"));
            _functionalNodeExpanders["AccumulateMultiply"] = CreateMutatingBinaryOperationExpander((builder, left, right) => builder.CreateMul(left, right, "multiply"));
            _functionalNodeExpanders["AccumulateDivide"] = CreateMutatingBinaryOperationExpander((builder, left, right) => builder.CreateSDiv(left, right, "divide"));
            _functionalNodeExpanders["AccumulateIncrement"] = ExpandAccumulateIncrement;
            _functionalNodeExpanders["AccumulateAnd"] = CreateMutatingBinaryOperationExpander((builder, left, right) => builder.CreateAnd(left, right, "and"));
            _functionalNodeExpanders["AccumulateOr"] = CreateMutatingBinaryOperationExpander((builder, left, right) => builder.CreateOr(left, right, "or"));
            _functionalNodeExpanders["AccumulateXor"] = CreateMutatingBinaryOperationExpander((builder, left, right) => builder.CreateXor(left, right, "xor"));
            _functionalNodeExpanders["AccumulateNot"] = CreateMutatingUnaryOperationExpander((builder, value) => builder.CreateNot(value, "not"));

            _functionalNodeExpanders["Equal"] = CreatePureBinaryOperationExpander((builder, left, right) => builder.CreateICmp(LLVMIntPredicate.LLVMIntEQ, left, right, "eq"));
            _functionalNodeExpanders["NotEqual"] = CreatePureBinaryOperationExpander((builder, left, right) => builder.CreateICmp(LLVMIntPredicate.LLVMIntNE, left, right, "ne"));
            _functionalNodeExpanders["LessThan"] = CreatePureBinaryOperationExpander((builder, left, right) => builder.CreateICmp(LLVMIntPredicate.LLVMIntSLT, left, right, "lt"));
            _functionalNodeExpanders["LessEqual"] = CreatePureBinaryOperationExpander((builder, left, right) => builder.CreateICmp(LLVMIntPredicate.LLVMIntSLE, left, right, "le"));
            _functionalNodeExpanders["GreaterThan"] = CreatePureBinaryOperationExpander((builder, left, right) => builder.CreateICmp(LLVMIntPredicate.LLVMIntSGT, left, right, "gt"));
            _functionalNodeExpanders["GreaterEqual"] = CreatePureBinaryOperationExpander((builder, left, right) => builder.CreateICmp(LLVMIntPredicate.LLVMIntSGE, left, right, "ge"));

            // option
            _functionalNodeExpanders["Some"] = ExpandSomeConstructor;
            _functionalNodeExpanders["None"] = ExpandNoneConstructor;
            _functionalNodeExpanders["OptionToPanicResult"] = CreateSpecializedFunctionCallExpander(FunctionCompiler.BuildOptionToPanicResultFunction);

            _functionalNodeExpanders["Range"] = CreateImportedCommonFunctionExpander(CommonModules.CreateRangeIteratorName);

            _functionalNodeExpanders["StringFromSlice"] = CreateImportedCommonFunctionExpander(CommonModules.StringFromSliceName);
            _functionalNodeExpanders["StringFromByteSlice"] = CreateImportedCommonFunctionExpander(CommonModules.StringFromSliceName);
            _functionalNodeExpanders["StringToSlice"] = CreateImportedCommonFunctionExpander(CommonModules.StringToSliceName);
            _functionalNodeExpanders["StringAppend"] = CreateImportedCommonFunctionExpander(CommonModules.StringAppendName);
            _functionalNodeExpanders["StringConcat"] = CreateImportedCommonFunctionExpander(CommonModules.StringConcatName);
            _functionalNodeExpanders["StringSliceToStringSplitIterator"] = CreateImportedCommonFunctionExpander(CommonModules.StringSliceToStringSplitIteratorName);

            _functionalNodeExpanders["VectorCreate"] = CreateSpecializedFunctionCallExpander(FunctionCompiler.BuildVectorCreateFunction);
            _functionalNodeExpanders["VectorInitialize"] = CreateSpecializedFunctionCallExpander(FunctionCompiler.BuildVectorInitializeFunction);
            _functionalNodeExpanders["VectorToSlice"] = CreateSpecializedFunctionCallExpander(FunctionCompiler.BuildVectorToSliceFunction);
            _functionalNodeExpanders["VectorAppend"] = CreateSpecializedFunctionCallExpander(FunctionCompiler.BuildVectorAppendFunction);
            _functionalNodeExpanders["VectorInsert"] = ExpandEmpty;
            _functionalNodeExpanders["VectorRemoveLast"] = CreateSpecializedFunctionCallExpander(FunctionCompiler.BuildVectorRemoveLastFunction);

            _functionalNodeExpanders["SliceIndex"] = CreateSpecializedFunctionCallExpander(FunctionCompiler.CreateSliceIndexFunction);
            _functionalNodeExpanders["SliceToIterator"] = CreateSpecializedFunctionCallExpander(FunctionCompiler.CreateSliceToIteratorFunction);
            _functionalNodeExpanders["SliceToMutableIterator"] = CreateSpecializedFunctionCallExpander(FunctionCompiler.CreateSliceToIteratorFunction);

            _functionalNodeExpanders["CreateLockingCell"] = ExpandEmpty;

            _functionalNodeExpanders["SharedCreate"] = CreateSpecializedFunctionCallExpander(FunctionCompiler.BuildSharedCreateFunction);
            _functionalNodeExpanders["SharedGetValue"] = CreateSpecializedFunctionCallExpander(FunctionCompiler.BuildSharedGetValueFunction);

            _functionalNodeExpanders["OpenFileHandle"] = CreateImportedCommonFunctionExpander(CommonModules.OpenFileHandleName);
            _functionalNodeExpanders["ReadLineFromFileHandle"] = CreateImportedCommonFunctionExpander(CommonModules.ReadLineFromFileHandleName);
            _functionalNodeExpanders["WriteStringToFileHandle"] = CreateImportedCommonFunctionExpander(CommonModules.WriteStringToFileHandleName);

            _functionalNodeExpanders["CreateYieldPromise"] = CreateSpecializedFunctionCallExpander(FunctionCompiler.BuildCreateYieldPromiseFunction);

            _functionalNodeExpanders["CreateNotifierPair"] = CreateSpecializedFunctionCallExpander(FunctionCompiler.BuildCreateNotifierPairFunction);
            _functionalNodeExpanders["GetReaderPromise"] = CreateSpecializedFunctionCallExpander(FunctionCompiler.BuildGetNotifierReaderPromiseFunction);
            _functionalNodeExpanders["SetNotifierValue"] = CreateSpecializedFunctionCallExpander(FunctionCompiler.BuildSetNotifierValueFunction);
        }

        private readonly Dictionary<CompilableDefinitionName, bool> _calleesMayPanic;

        public CodeGenExpander(
            DfirRoot dfirRoot,
            FunctionModuleContext moduleContext,
            Dictionary<CompilableDefinitionName, bool> calleesMayPanic)
        {
            DfirRoot = dfirRoot;
            ModuleContext = moduleContext;
            _calleesMayPanic = calleesMayPanic;
        }

        private DfirRoot DfirRoot { get; }

        private FunctionModuleContext ModuleContext { get; }

        public ContextWrapper Context => ModuleContext.LLVMContext;

        public int ReservedIndexCount { get; private set; }

        private AsyncStateGroup CurrentGroup { get; set; }

        private int ReserveIndices(int indexCount)
        {
            int current = ReservedIndexCount;
            ReservedIndexCount += indexCount;
            return current;
        }

        public void ExpandAsyncStateGroup(AsyncStateGroup group)
        {
            var conditionalContinuation = group.Continuation as ConditionallyScheduleGroupsContinuation;
            if (conditionalContinuation != null)
            {
                if (conditionalContinuation.SuccessorConditionGroups.Count != 2)
                {
                    throw new NotSupportedException("Only boolean conditions supported for continuations");
                }
                VariableSet variableSet = DfirRoot.GetVariableSet();
                VariableReference continuation = variableSet.CreateNewVariable(
                    0,
                    variableSet.TypeVariableSet.CreateTypeVariableReferenceFromNIType(NITypes.Boolean),
                    mutable: true);
                continuation.Name = $"{group.Label}_continuationStatePtr";
                group.ContinuationCondition = continuation;
            }

            CurrentGroup = group;
            List<Visitation> expandedList = group.Visitations.SelectMany(visitation => visitation.Visit<IEnumerable<Visitation>>(this)).ToList();
            group.ReplaceVisitations(expandedList);
            CurrentGroup = null;
        }

        public IEnumerable<Visitation> VisitAwaitNode(AwaitNode awaitNode)
        {
            // We can assume that an AwaitNode will be the first node in its AsyncStateGroup.
            // We will poll the promise; if it returns Some(T), we will drop the promise if necessary
            // and continue with the rest of the group, and otherwise return early.
            NIType promiseType = awaitNode.InputTerminal.GetTrueVariable().Type;
            LLVMValueRef promisePollFunction = ModuleContext.GetPromisePollFunction(promiseType);

            int startIndex = ReserveIndices(6);
            yield return new GetAddress(awaitNode.InputTerminal.GetTrueVariable(), startIndex);
            yield return new Op((builder, valueStorage) => FunctionCompiler.GenerateWakerFromCurrentGroup(builder, valueStorage, startIndex + 1));
            yield return new GetAddress(awaitNode.PollResultVariable, startIndex + 2);
            yield return new Call(promisePollFunction, new int[] { startIndex, startIndex + 1, startIndex + 2 });
            yield return new GetValue(awaitNode.PollResultVariable, startIndex + 3);
            yield return new GetStructFieldValue(0, startIndex + 3, startIndex + 4);
            yield return new Op((builder, valueStorage) => FunctionCompiler.GeneratePromisePollAndBranch(builder, valueStorage, startIndex + 4));

            LLVMValueRef promiseDropFunction;
            if (TraitHelpers.TryGetDropFunction(promiseType, ModuleContext, out promiseDropFunction))
            {
                yield return new Call(promiseDropFunction, new int[] { startIndex });
            }
            yield return new GetStructFieldValue(1, startIndex + 3, startIndex + 5);
            yield return new InitializeValue(awaitNode.OutputTerminal.GetTrueVariable(), startIndex + 5);
        }

        public IEnumerable<Visitation> VisitBorrowTunnel(BorrowTunnel borrowTunnel)
        {
            VariableReference inputVariable = borrowTunnel.InputTerminals[0].GetTrueVariable(),
                outputVariable = borrowTunnel.OutputTerminals[0].GetTrueVariable();
            yield return new InitializeAsReference(inputVariable, outputVariable);
        }

        public IEnumerable<Visitation> VisitBuildTupleNode(BuildTupleNode buildTupleNode)
        {
            int fieldCount = buildTupleNode.InputTerminals.Count;
            int startIndex = ReserveIndices(fieldCount + 1);

            // TODO: note that each input value is moved into another data structure
            int i = startIndex;
            foreach (Terminal inputTerminal in buildTupleNode.InputTerminals)
            {
                yield return new GetValue(inputTerminal.GetTrueVariable(), i);
                ++i;
            }

            VariableReference outputVariable = buildTupleNode.OutputTerminals[0].GetTrueVariable();
            LLVMTypeRef outputLLVMType = Context.AsLLVMType(outputVariable.Type);
            yield return new BuildStruct(outputLLVMType, Enumerable.Range(startIndex, fieldCount).ToArray(), startIndex + fieldCount);

            yield return new InitializeValue(outputVariable, startIndex + fieldCount);
        }

        public IEnumerable<Visitation> VisitConstant(Constant constant)
        {
            int index = ReserveIndices(1);
            VariableReference outputVariable = constant.OutputTerminal.GetTrueVariable();
            NIType constantType = outputVariable.Type;
            if (constantType.IsInteger())
            {
                yield return new GetConstant((moduleContext, builder) => moduleContext.LLVMContext.GetIntegerValue(constant.Value, constant.DataType), index);
            }
            else if (constantType.IsBoolean())
            {
                yield return new GetConstant((moduleContext, builder) => moduleContext.LLVMContext.AsLLVMValue((bool)constant.Value), index);
            }
            else if (outputVariable.Type.IsRebarReferenceType() && outputVariable.Type.GetReferentType() == DataTypes.StringSliceType)
            {

                yield return new GetConstant(
                    (moduleContext, builder) =>
                    {
                        string stringValue = (string)constant.Value;
                        int length = Encoding.UTF8.GetByteCount(stringValue);
                        LLVMValueRef stringValueConstant = ModuleContext.LLVMContext.ConstString(stringValue);
                        LLVMValueRef stringConstantPtr = ModuleContext.Module.AddGlobal(stringValueConstant.TypeOf(), $"string{constant.UniqueId}");
                        stringConstantPtr.SetInitializer(stringValueConstant);

                        LLVMValueRef castPointer = builder.CreateBitCast(
                            stringConstantPtr,
                            moduleContext.LLVMContext.BytePointerType(),
                            "ptrCast");
                        LLVMValueRef[] stringSliceFields = new LLVMValueRef[]
                        {
                            castPointer,
                            moduleContext.LLVMContext.AsLLVMValue(length)
                        };
                        return moduleContext.LLVMContext.ConstStruct(stringSliceFields);
                    },
                    index);
            }
            else
            {
                throw new NotImplementedException();
            }
            yield return new InitializeValue(outputVariable, index);
        }

        public IEnumerable<Visitation> VisitCreateMethodCallPromise(CreateMethodCallPromise createMethodCallPromise)
        {
            int promiseIndex = ReserveIndices(1);
            yield return new GetAddress(createMethodCallPromise.PromiseTerminal.GetTrueVariable(), promiseIndex);
            int outputPtrIndex = ReserveIndices(1);
            // if the target may panic, then the promise's output field is a PanicResult whose first field will receive the actual output values.
            if (_calleesMayPanic[createMethodCallPromise.TargetName])
            {
                int panicResultPtrIndex = ReserveIndices(1);
                yield return new GetStructFieldPointer((int)FunctionCompiler.MethodCallPromiseOutputFieldIndex, promiseIndex, panicResultPtrIndex);
                yield return new GetStructFieldPointer(1, panicResultPtrIndex, outputPtrIndex);
            }
            else
            {
                yield return new GetStructFieldPointer((int)FunctionCompiler.MethodCallPromiseOutputFieldIndex, promiseIndex, outputPtrIndex);
            }

            var parameterIndices = new List<int>();
            foreach (Terminal inputTerminal in createMethodCallPromise.InputTerminals)
            {
                int index = ReserveIndices(1);
                yield return new GetValue(inputTerminal.GetTrueVariable(), index);
                parameterIndices.Add(index);
            }

            NIType[] outputParameters = createMethodCallPromise.Signature.GetParameters()
                .Where(p => p.GetOutputParameterPassingRule() != NIParameterPassingRule.NotAllowed)
                .ToArray();
            switch (outputParameters.Length)
            {
                case 0:
                    break;
                case 1:
                    parameterIndices.Add(outputPtrIndex);
                    break;
                default:
                    // for >1 parameters, the output field is a tuple of the output values
                    for (int i = 0; i < outputParameters.Length; ++i)
                    {
                        int fieldIndex = ReserveIndices(1);
                        yield return new GetStructFieldPointer(i, outputPtrIndex, fieldIndex);
                        parameterIndices.Add(fieldIndex);
                    }
                    break;
            }

            LLVMValueRef initializeStateFunction = ModuleContext.GetImportedInitializeStateFunction(createMethodCallPromise);
            int statePtrIndex = ReserveIndices(1);
            yield return new CallWithReturn(initializeStateFunction, parameterIndices.ToArray(), statePtrIndex);
            LLVMValueRef pollFunction = ModuleContext.GetImportedPollFunction(createMethodCallPromise);
            yield return new Op((builder, valueStorage) =>
            {
                LLVMValueRef promisePtr = valueStorage[promiseIndex],
                    promisePollStatePtr = builder.CreateStructGEP(promisePtr, (int)FunctionCompiler.MethodCallPromiseStatePtrFieldIndex, "promisePollStatePtr"),
                    promisePollFunctionPtr = builder.CreateStructGEP(promisePtr, (int)FunctionCompiler.MethodCallPromisePollFunctionPtrFieldIndex, "promisePollFunctionPtr");
                builder.CreateStore(pollFunction, promisePollFunctionPtr);
                builder.CreateStore(valueStorage[statePtrIndex], promisePollStatePtr);
            });
        }

        public IEnumerable<Visitation> VisitDataAccessor(DataAccessor dataAccessor)
        {
            VariableReference terminalVariable = dataAccessor.Terminal.GetTrueVariable();
            VariableReference dataItemVariable = dataAccessor.DataItem.GetVariable();
            int index = ReserveIndices(1);
            if (dataAccessor.Terminal.Direction == Direction.Output)
            {
                // TODO: distinguish inout from in parameters?
                yield return new GetValue(dataItemVariable, index);
                yield return new InitializeValue(terminalVariable, index);
            }
            else if (dataAccessor.Terminal.Direction == Direction.Input)
            {
                // assume that the function parameter is a pointer to where we need to store the value
                yield return new GetValue(terminalVariable, index);
                yield return new UpdateValue(dataItemVariable, index);
            }
        }

        public IEnumerable<Visitation> VisitDecomposeStructNode(DecomposeStructNode decomposeStructNode)
        {
            int fieldCount = decomposeStructNode.OutputTerminals.Count;
            int startIndex = ReserveIndices(fieldCount + 1);

            yield return new GetValue(decomposeStructNode.InputTerminals[0].GetTrueVariable(), startIndex + fieldCount);

            int i = 0;
            foreach (Terminal outputTerminal in decomposeStructNode.OutputTerminals)
            {
                yield return new GetStructFieldValue(i, startIndex + fieldCount, startIndex + i);
                yield return new InitializeValue(outputTerminal.GetTrueVariable(), startIndex + i);
                ++i;
            }
        }

        public IEnumerable<Visitation> VisitDecomposeTupleNode(DecomposeTupleNode decomposeTupleNode)
        {
            int fieldCount = decomposeTupleNode.OutputTerminals.Count;
            int startIndex = ReserveIndices(fieldCount + 1);

            yield return new GetValue(decomposeTupleNode.InputTerminals[0].GetTrueVariable(), startIndex + fieldCount);

            // TODO: for Borrow mode, mark each output reference as a struct offset of the input reference
            int i = 0;
            foreach (Terminal outputTerminal in decomposeTupleNode.OutputTerminals)
            {
                yield return decomposeTupleNode.DecomposeMode == DecomposeMode.Borrow
                    ? (CodeGenElement)new GetStructFieldPointer(i, startIndex + fieldCount, startIndex + i)
                    : new GetStructFieldValue(i, startIndex + fieldCount, startIndex + i);
                yield return new InitializeValue(outputTerminal.GetTrueVariable(), startIndex + i);
                ++i;
            }
        }

        public IEnumerable<Visitation> VisitDropNode(DropNode dropNode)
        {
            VariableReference input = dropNode.InputTerminals[0].GetTrueVariable();
            LLVMValueRef dropFunction;
            if (TraitHelpers.TryGetDropFunction(input.Type, ModuleContext, out dropFunction))
            {
                int index = ReserveIndices(1);
                yield return new GetAddress(input, index);
                yield return new Call(dropFunction, new int[] { index });
            }
        }

        public IEnumerable<Visitation> VisitExplicitBorrowNode(ExplicitBorrowNode explicitBorrowNode)
        {
            foreach (KeyValuePair<Terminal, Terminal> terminalPair in explicitBorrowNode.InputTerminals.Zip(explicitBorrowNode.OutputTerminals))
            {
                VariableReference inputVariable = terminalPair.Key.GetTrueVariable(),
                    outputVariable = terminalPair.Value.GetTrueVariable();
                if (inputVariable.Type == outputVariable.Type || inputVariable.Type.IsReferenceToSameTypeAs(outputVariable.Type))
                {
                    yield return new ShareValue(inputVariable, outputVariable);
                }
                else
                {
                    // TODO: there is a bug here with creating a reference to an immutable reference binding;
                    // in CreateReferenceValueSource we create a constant reference value source for the immutable reference,
                    // which means we can't create a reference to an allocation for it.
                    yield return new InitializeAsReference(inputVariable, outputVariable);
                }
            }
        }

        public IEnumerable<Visitation> VisitFrame(Frame frame, StructureTraversalPoint traversalPoint)
        {
            if (frame.DoesStructureExecuteConditionally())
            {
                switch (traversalPoint)
                {
                    case StructureTraversalPoint.BeforeLeftBorderNodes:
                        int index = ReserveIndices(1);
                        yield return new GetConstant((moduleContext, builder) => moduleContext.LLVMContext.AsLLVMValue(true), index);
                        yield return new InitializeValue(frame.GetConditionVariable(), index);
                        break;
                    case StructureTraversalPoint.AfterLeftBorderNodesAndBeforeDiagram:
                        index = ReserveIndices(1);
                        yield return new GetValue(frame.GetConditionVariable(), index);
                        yield return new UpdateValue(CurrentGroup.ContinuationCondition, index);
                        break;
                }
            }
        }

        public IEnumerable<Visitation> VisitFrameSkippedBlockVisitation(FrameSkippedBlockVisitation visitation)
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
                LLVMValueRef dropFunction;
                if (TraitHelpers.TryGetDropFunction(variableToDrop.Type, ModuleContext, out dropFunction))
                {
                    int index = ReserveIndices(1);
                    yield return new GetAddress(variableToDrop, index);
                    yield return new Call(dropFunction, new int[] { index });
                }
            }

            // Initialize any output variables to None
            foreach (Tunnel tunnel in frame.BorderNodes.OfType<Tunnel>().Where(t => t.Direction == Direction.Output))
            {
                // TODO: for now, this means that these tunnels require local allocations.
                // It would be nicer to allow them to be Phi values--i.e., ValueSources that can be
                // initialized by values from different predecessor blocks, but may not change
                // after initialization.
                VariableReference outputVariable = tunnel.OutputTerminals[0].GetTrueVariable();
                LLVMTypeRef outputType = Context.AsLLVMType(outputVariable.Type);
                int index = ReserveIndices(1);
                yield return new GetConstant((moduleContext, builder) => LLVMSharp.LLVM.ConstNull(outputType), index);
                yield return new UpdateValue(outputVariable, index);
            }
        }

        public IEnumerable<Visitation> VisitFunctionalNode(FunctionalNode functionalNode)
        {
            FunctionalNodeExpander expander;
            if (_functionalNodeExpanders.TryGetValue(functionalNode.Signature.GetName(), out expander))
            {
                return expander(functionalNode, this);
            }
            throw new NotSupportedException("Don't know how to expand FunctionalNode with signature " + functionalNode.Signature.GetName());
        }

        public IEnumerable<Visitation> VisitIterateTunnel(IterateTunnel iterateTunnel)
        {
            int startIndex = ReserveIndices(7);
            VariableReference iteratorVariable = iterateTunnel.InputTerminals[0].GetTrueVariable();
            yield return new GetValue(iteratorVariable, startIndex);
            yield return new GetAddress(iterateTunnel.IntermediateValueVariable, startIndex + 1, forInitialize: true);
            NIType iteratorType = iteratorVariable.Type.GetReferentType();
            LLVMValueRef iteratorNextFunction = ModuleContext.GetIteratorNextFunction(iteratorType, iterateTunnel.IteratorNextFunctionType.FunctionNIType);
            yield return new Call(iteratorNextFunction, new int[] { startIndex, startIndex + 1 });
            yield return new GetValue(iterateTunnel.IntermediateValueVariable, startIndex + 2);
            yield return new GetStructFieldValue(0, startIndex + 2, startIndex + 3);
            yield return new GetStructFieldValue(1, startIndex + 2, startIndex + 4);
            VariableReference loopConditionVariable = GetConditionVariable((Compiler.Nodes.Loop)iterateTunnel.ParentStructure);
            yield return new GetValue(loopConditionVariable, startIndex + 5);
            yield return new Op((builder, valueStorage) => valueStorage[startIndex + 6] = builder.CreateAnd(valueStorage[startIndex + 3], valueStorage[startIndex + 5], "conditionAndIsSome"));
            yield return new UpdateValue(loopConditionVariable, startIndex + 6);
            yield return new InitializeValue(iterateTunnel.OutputTerminals[0].GetTrueVariable(), startIndex + 4);
        }

        public IEnumerable<Visitation> VisitLockTunnel(LockTunnel lockTunnel)
        {
            throw new NotImplementedException();
        }

        public IEnumerable<Visitation> VisitLoop(Compiler.Nodes.Loop loop, StructureTraversalPoint traversalPoint)
        {
            switch (traversalPoint)
            {
                case StructureTraversalPoint.BeforeLeftBorderNodes:
                    {
                        LoopConditionTunnel loopCondition = loop.BorderNodes.OfType<LoopConditionTunnel>().First();
                        Terminal loopConditionInput = loopCondition.InputTerminals[0];

                        if (!loopConditionInput.IsConnected)
                        {
                            int conditionIndex = ReserveIndices(1);
                            yield return new GetConstant((moduleContext, builder) => moduleContext.LLVMContext.AsLLVMValue(true), conditionIndex);
                            yield return new InitializeValue(GetConditionVariable(loop), conditionIndex);
                        }

                        // initialize all output tunnels with None values, in case the loop interior does not execute
                        foreach (Tunnel outputTunnel in loop.BorderNodes.OfType<Tunnel>().Where(tunnel => tunnel.Direction == Direction.Output))
                        {
                            // TODO: this requires these tunnels to have local allocations for now.
                            // As with output tunnels of conditionally-executing Frames, it would be nice
                            // to treat these as Phi ValueSources.
                            VariableReference tunnelOutputVariable = outputTunnel.OutputTerminals[0].GetTrueVariable();
                            LLVMTypeRef tunnelOutputType = Context.AsLLVMType(tunnelOutputVariable.Type);
                            int outputIndex = ReserveIndices(1);
                            yield return new GetConstant((moduleContext, builder) => LLVMSharp.LLVM.ConstNull(tunnelOutputType), outputIndex);
                            yield return new UpdateValue(tunnelOutputVariable, outputIndex);
                        }
                    }
                    break;
                case StructureTraversalPoint.AfterLeftBorderNodesAndBeforeDiagram:
                    {
                        int conditionIndex = ReserveIndices(1);
                        yield return new GetValue(GetConditionVariable(loop), conditionIndex);
                        yield return new UpdateValue(CurrentGroup.ContinuationCondition, conditionIndex);
                    }
                    break;
                default:
                    break;
            }
        }

        private VariableReference GetConditionVariable(Compiler.Nodes.Loop loop)
        {
            LoopConditionTunnel loopCondition = loop.BorderNodes.OfType<LoopConditionTunnel>().First();
            return loopCondition.InputTerminals[0].GetTrueVariable();
        }

        public IEnumerable<Visitation> VisitLoopConditionTunnel(LoopConditionTunnel loopConditionTunnel)
        {
            yield return new InitializeAsReference(
                loopConditionTunnel.InputTerminals[0].GetTrueVariable(),
                loopConditionTunnel.OutputTerminals[0].GetTrueVariable());
        }

        public IEnumerable<Visitation> VisitMethodCallNode(MethodCallNode methodCallNode)
        {
            LLVMValueRef targetFunction = ModuleContext.GetImportedSynchronousFunction(methodCallNode);
            return ExpandCallForFunctionalNode(targetFunction, methodCallNode, methodCallNode.Signature);
        }

        public IEnumerable<Visitation> VisitOptionPatternStructure(OptionPatternStructure optionPatternStructure, StructureTraversalPoint traversalPoint, Diagram nestedDiagram)
        {
            switch (traversalPoint)
            {
                case StructureTraversalPoint.BeforeLeftBorderNodes:
                    {
                        int startIndex = ReserveIndices(3);
                        yield return new GetValue(optionPatternStructure.Selector.InputTerminals[0].GetTrueVariable(), startIndex);
                        yield return new GetStructFieldValue(0, startIndex, startIndex + 1);
                        yield return new Op((builder, valueStorage) => valueStorage[startIndex + 2] = builder.CreateNot(valueStorage[startIndex + 1], "isNone"));
                        yield return new UpdateValue(CurrentGroup.ContinuationCondition, startIndex + 2);
                    }
                    break;
                case StructureTraversalPoint.AfterLeftBorderNodesAndBeforeDiagram:
                    {
                        // TODO: this should be in a diagram-specific VisitOptionPatternStructureSelector
                        if (nestedDiagram == optionPatternStructure.Diagrams[0])
                        {
                            int startIndex = ReserveIndices(2);
                            yield return new GetValue(optionPatternStructure.Selector.InputTerminals[0].GetTrueVariable(), startIndex);
                            yield return new GetStructFieldValue(1, startIndex, startIndex + 1);
                            yield return new InitializeValue(optionPatternStructure.Selector.OutputTerminals[0].GetTrueVariable(), startIndex + 1);
                        }
                    }
                    break;
                case StructureTraversalPoint.AfterDiagram:
                    {
                        foreach (Tunnel outputTunnel in optionPatternStructure.Tunnels.Where(tunnel => tunnel.Direction == Direction.Output))
                        {
                            Terminal inputTerminal = outputTunnel.InputTerminals.First(t => t.ParentDiagram == nestedDiagram);
                            // TODO: these Tunnel output variables should also be able to be Phi ValueSources
                            int index = ReserveIndices(1);
                            yield return new GetValue(inputTerminal.GetTrueVariable(), index);
                            yield return new UpdateValue(outputTunnel.OutputTerminals[0].GetTrueVariable(), index);
                        }
                    }
                    break;
                default:
                    yield return new StructureVisitation(optionPatternStructure, nestedDiagram, traversalPoint);
                    break;
            }
        }

        public IEnumerable<Visitation> VisitOptionPatternStructureSelector(OptionPatternStructureSelector optionPatternStructureSelector) => Enumerable.Empty<CodeGenElement>();

        public IEnumerable<Visitation> VisitPanicOrContinueNode(PanicOrContinueNode panicOrContinueNode)
        {
            int startIndex = ReserveIndices(3);
            yield return new GetValue(panicOrContinueNode.InputTerminal.GetTrueVariable(), startIndex);
            yield return new GetStructFieldValue(0, startIndex, startIndex + 1);
            yield return new Op((builder, valueStorage) => 
                FunctionCompiler.GeneratePanicOrContinueBranch(builder, valueStorage, panicOrContinueNode.UniqueId, startIndex + 1));
            yield return new GetStructFieldValue(1, startIndex, startIndex + 2);
            yield return new InitializeValue(panicOrContinueNode.OutputTerminal.GetTrueVariable(), startIndex + 2);
        }

        public IEnumerable<Visitation> VisitStructConstructorNode(StructConstructorNode structConstructorNode)
        {
            int fieldCount = structConstructorNode.InputTerminals.Count;
            int startIndex = ReserveIndices(fieldCount + 1);

            // TODO: note that each input value is moved into another data structure
            int i = startIndex;
            foreach (Terminal inputTerminal in structConstructorNode.InputTerminals)
            {
                yield return new GetValue(inputTerminal.GetTrueVariable(), i);
                ++i;
            }

            VariableReference outputVariable = structConstructorNode.OutputTerminals[0].GetTrueVariable();
            LLVMTypeRef outputLLVMType = Context.AsLLVMType(outputVariable.Type);
            yield return new BuildStruct(outputLLVMType, Enumerable.Range(startIndex, fieldCount).ToArray(), startIndex + fieldCount);

            yield return new InitializeValue(outputVariable, startIndex + fieldCount);
        }

        public IEnumerable<Visitation> VisitStructFieldAccessorNode(StructFieldAccessorNode structFieldAccessorNode)
        {
            int fieldCount = structFieldAccessorNode.OutputTerminals.Count;
            int startIndex = ReserveIndices(fieldCount + 1);
            VariableReference structInputVariable = structFieldAccessorNode.StructInputTerminal.GetTrueVariable();
            NIType structType = structInputVariable.Type.GetReferentType();
            yield return new GetValue(structInputVariable, startIndex + fieldCount);

            int i = 0;
            string[] structTypeFieldNames = structType.GetFields().Select(f => f.GetName()).ToArray();
            foreach (var pair in structFieldAccessorNode.FieldNames.Zip(structFieldAccessorNode.OutputTerminals))
            {
                string accessedFieldName = pair.Key;
                int accessedFieldIndex = structTypeFieldNames.IndexOf(accessedFieldName);
                if (accessedFieldIndex == -1)
                {
                    throw new InvalidStateException("Field name not found in struct type: " + accessedFieldName);
                }
                // TODO: the output variables can become constant GEPs of the input pointer variable
                yield return new GetStructFieldPointer(accessedFieldIndex, startIndex + fieldCount, startIndex + i);

                Terminal outputTerminal = pair.Value;
                yield return new InitializeValue(outputTerminal.GetTrueVariable(), startIndex + i);
                ++i;
            }
        }

        public IEnumerable<Visitation> VisitTerminateLifetimeNode(TerminateLifetimeNode terminateLifetimeNode) => Enumerable.Empty<CodeGenElement>();

        public IEnumerable<Visitation> VisitTerminateLifetimeTunnel(TerminateLifetimeTunnel terminateLifetimeTunnel) => Enumerable.Empty<CodeGenElement>();

        public IEnumerable<Visitation> VisitTunnel(Tunnel tunnel)
        {
            if (tunnel.Direction == Direction.Input)
            {
                VariableReference inputVariable = tunnel.InputTerminals[0].GetTrueVariable();
                foreach (Terminal outputTerminal in tunnel.OutputTerminals)
                {
                    yield return new ShareValue(inputVariable, outputTerminal.GetTrueVariable());
                }
            }
            else // tunnel.Direction == Output
            {
                int inputTerminalCount = tunnel.InputTerminals.Count,
                    outputTerminalCount = tunnel.OutputTerminals.Count;
                if (inputTerminalCount == 1)
                {
                    VariableReference inputVariable = tunnel.InputTerminals[0].GetTrueVariable(),
                        outputVariable = tunnel.OutputTerminals[0].GetTrueVariable();
                    if (outputVariable.Type == inputVariable.Type.CreateOption())
                    {
                        int index = ReserveIndices(3);
                        yield return new GetConstant((moduleContext, builder) => moduleContext.LLVMContext.AsLLVMValue(true), index);
                        yield return new GetValue(inputVariable, index + 1);
                        yield return new BuildStruct(Context.AsLLVMType(outputVariable.Type), new int[] { index, index + 1 }, index + 2);
                        yield return new UpdateValue(outputVariable, index + 2);
                    }
                    else
                    {
                        // TODO: maybe it's better to compute variable usage for the input and output separately, and only reuse
                        // the ValueSource if they are close enough
                        yield return new ShareValue(inputVariable, outputVariable);
                    }
                }
                else
                {
                    // Note: handled in VisitOptionPatternStructure/VisitVariantMatchStructure
                }
            }
        }

        public IEnumerable<Visitation> VisitUnwrapOptionTunnel(UnwrapOptionTunnel unwrapOptionTunnel)
        {
            var frame = (Frame)unwrapOptionTunnel.ParentStructure;
            int startIndex = ReserveIndices(5);
            yield return new GetValue(unwrapOptionTunnel.InputTerminals[0].GetTrueVariable(), startIndex);
            yield return new GetStructFieldValue(0, startIndex, startIndex + 1);
            yield return new GetStructFieldValue(1, startIndex, startIndex + 2);
            yield return new GetValue(frame.GetConditionVariable(), startIndex + 3);
            yield return new Op((builder, valueStorage) => valueStorage[startIndex + 4] = builder.CreateAnd(valueStorage[startIndex + 1], valueStorage[startIndex + 3], "newCondition"));
            yield return new UpdateValue(frame.GetConditionVariable(), startIndex + 4);
            yield return new InitializeValue(unwrapOptionTunnel.OutputTerminals[0].GetTrueVariable(), startIndex + 2);
        }

        public IEnumerable<Visitation> VisitWire(Wire wire)
        {
            if (!wire.SinkTerminals.HasMoreThan(1))
            {
                yield break;
            }
            VariableReference sourceVariable = wire.SourceTerminal.GetTrueVariable();
            foreach (var sinkTerminal in wire.SinkTerminals.Skip(1))
            {
                yield return new InitializeWithCopy(sourceVariable, sinkTerminal.GetTrueVariable());
            }
        }
    }
}
