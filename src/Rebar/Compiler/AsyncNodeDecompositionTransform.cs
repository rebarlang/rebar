using System;
using System.Collections.Generic;
using System.Linq;
using NationalInstruments;
using NationalInstruments.Compiler;
using NationalInstruments.DataTypes;
using NationalInstruments.Dfir;
using NationalInstruments.Linking;
using Rebar.Common;
using Rebar.Compiler.Nodes;

namespace Rebar.Compiler
{
    internal class AsyncNodeDecompositionTransform : IDfirTransform
    {
        private static readonly Dictionary<string, NIType> _createPromiseSignatures;
        private static readonly Dictionary<string, NIType> _createPanicResultSignatures;

        static AsyncNodeDecompositionTransform()
        {
            _createPromiseSignatures = new Dictionary<string, NIType>();
            _createPromiseSignatures["Yield"] = Signatures.CreateYieldPromiseType;
            _createPromiseSignatures["GetNotifierValue"] = Signatures.GetReaderPromiseType;

            _createPanicResultSignatures = new Dictionary<string, NIType>();
            _createPanicResultSignatures["UnwrapOption"] = Signatures.OptionToPanicResultType;
        }

        private readonly ITypeUnificationResultFactory _unificationResultFactory;
        private readonly Dictionary<ExtendedQualifiedName, bool> _isYielding;
        private readonly Dictionary<ExtendedQualifiedName, bool> _mayPanic;

        public AsyncNodeDecompositionTransform(
            Dictionary<ExtendedQualifiedName, bool> isYielding,
            Dictionary<ExtendedQualifiedName, bool> mayPanic,
            ITypeUnificationResultFactory unificationResultFactory)
        {
            _isYielding = isYielding;
            _mayPanic = mayPanic;
            _unificationResultFactory = unificationResultFactory;
        }

        public void Execute(DfirRoot dfirRoot, CompileCancellationToken cancellationToken)
        {
            var nodesToDecompose = dfirRoot.GetAllNodesIncludingSelf().Where(CanDecompose).ToList();
            nodesToDecompose.ForEach(Decompose);
        }

        private bool CanDecompose(Node node)
        {
            var methodCallNode = node as MethodCallNode;
            if (methodCallNode != null)
            {
                return _isYielding[methodCallNode.TargetName];
            }
            var functionalNode = node as FunctionalNode;
            if (functionalNode != null)
            {
                string signatureName = functionalNode.Signature.GetName();
                bool isYielding = _createPromiseSignatures.ContainsKey(signatureName),
                    isPanicking = _createPanicResultSignatures.ContainsKey(signatureName);
                if (isYielding && isPanicking)
                {
                    throw new NotImplementedException("Cannot handle a non-MethodCallNode that yields and panics");
                }
                return isYielding || isPanicking;
            }
            return false;
        }

        private void Decompose(Node node)
        {
            var methodCallNode = node as MethodCallNode;
            if (methodCallNode != null)
            {
                if (_mayPanic[methodCallNode.TargetName])
                {
                    throw new NotImplementedException("Decomposing calls to panicking Functions not implemented yet.");
                }
                DecomposeMethodCall(methodCallNode);
                return;
            }
            var functionalNode = node as FunctionalNode;
            if (functionalNode != null)
            {
                string signatureName = functionalNode.Signature.GetName();
                NIType signature;
                if (_createPanicResultSignatures.TryGetValue(signatureName, out signature))
                {
                    DecomposePanickingFunctionalNode(functionalNode, signature);
                    return;
                }
                DecomposeAsyncFunctionalNode(functionalNode, _createPromiseSignatures[signatureName]);
            }
        }

        private void DecomposeMethodCall(MethodCallNode methodCallNode)
        {
            var createMethodCallPromise = new CreateMethodCallPromise(methodCallNode.ParentDiagram, methodCallNode.Signature, methodCallNode.TargetName);
            AutoBorrowNodeFacade methodCallNodeFacade = AutoBorrowNodeFacade.GetNodeFacade(methodCallNode);
            AutoBorrowNodeFacade createMethodCallPromiseFacade = AutoBorrowNodeFacade.GetNodeFacade(createMethodCallPromise);
            foreach (var terminalPair in methodCallNode.InputTerminals.Zip(createMethodCallPromise.InputTerminals))
            {
                Terminal methodCallTerminal = terminalPair.Key, createMethodCallPromiseTerminal = terminalPair.Value;
                methodCallTerminal.ConnectedTerminal.ConnectTo(createMethodCallPromiseTerminal);
                createMethodCallPromiseFacade[createMethodCallPromiseTerminal] = methodCallNodeFacade[methodCallTerminal];
            }

            NIType outputType;
            // TODO: try to use something like Unit or Void
            NIType emptyOutputType = PFTypes.Boolean;
            switch (methodCallNode.OutputTerminals.Count)
            {
                case 0:
                    outputType = emptyOutputType;
                    break;
                case 1:
                    outputType = methodCallNode.OutputTerminals[0].GetTrueVariable().Type;
                    break;
                default:
                    outputType = methodCallNode.OutputTerminals.Select(t => t.GetTrueVariable().Type).DefineTupleType();
                    break;
            }
            Terminal methodCallOutputTerminal = methodCallNode.OutputTerminals.FirstOrDefault();
            NIType promiseType = outputType.CreateMethodCallPromise();
            Terminal promiseTerminal = createMethodCallPromise.OutputTerminals[0];
            createMethodCallPromiseFacade[promiseTerminal] = new SimpleTerminalFacade(
                promiseTerminal,
                createMethodCallPromise.GetTypeVariableSet().CreateTypeVariableReferenceFromNIType(promiseType));

            AwaitNode awaitNode = ConnectNewNodeToOutputTerminal(createMethodCallPromise, diagram => new AwaitNode(diagram));

            CreateDefaultFacadeForAwaitOutputTerminal(awaitNode, outputType);
            switch (methodCallNode.OutputTerminals.Count)
            {
                case 0:
                    // no method call output terminals; drop the result of the await
                    InsertDropTransform.InsertDropForVariable(awaitNode.ParentDiagram, LiveVariable.FromTerminal(awaitNode.OutputTerminal), _unificationResultFactory);
                    break;
                case 1:
                    ConnectOutputTerminal(methodCallNode.OutputTerminals[0], awaitNode.OutputTerminal);
                    break;
                default:
                    {
                        // for >1 method call output terminals, decompose the tuple result of the await and match each
                        // decomposed terminal to a method call terminal
                        DecomposeTupleNode decomposeTupleNode = InsertDropTransform.InsertDecompositionForTupleVariable(
                            methodCallNode.ParentDiagram,
                            LiveVariable.FromTerminal(awaitNode.OutputTerminal),
                            _unificationResultFactory);
                        foreach (var pair in methodCallNode.OutputTerminals.Zip(decomposeTupleNode.OutputTerminals))
                        {
                            Terminal methodCallTerminal = pair.Key, decomposeTupleTerminal = pair.Value;
                            ConnectOutputTerminal(methodCallTerminal, decomposeTupleTerminal);
                        }
                        break;
                    }
            }

            methodCallNode.RemoveFromGraph();
        }

        private void DecomposeAsyncFunctionalNode(FunctionalNode functionalNode, NIType createPromiseSignature)
        {
            if (functionalNode.OutputTerminals.HasMoreThan(1))
            {
                throw new NotSupportedException("Decomposing FunctionalNodes with multiple output parameters not supported yet.");
            }

            FunctionalNode createPromiseNode = CreateInputReplacementNode(functionalNode, diagram => CreateFunctionalNodeWithFacade(diagram, createPromiseSignature));
            AwaitNode awaitNode = ConnectNewNodeToOutputTerminal(createPromiseNode, diagram => new AwaitNode(diagram));
            ConnectOutputTerminal(functionalNode.OutputTerminals[0], awaitNode.OutputTerminal);

            functionalNode.RemoveFromGraph();
        }

        private void DecomposePanickingFunctionalNode(FunctionalNode functionalNode, NIType createPanicResultSignature)
        {
            if (functionalNode.OutputTerminals.HasMoreThan(1))
            {
                throw new NotSupportedException("Decomposing FunctionalNodes with multiple output parameters not supported yet.");
            }

            FunctionalNode createPanicResultNode = CreateInputReplacementNode(functionalNode, diagram => CreateFunctionalNodeWithFacade(diagram, createPanicResultSignature));
            PanicOrContinueNode panicOrContinueNode = ConnectNewNodeToOutputTerminal(createPanicResultNode, diagram => new PanicOrContinueNode(diagram));
            ConnectOutputTerminal(functionalNode.OutputTerminals[0], panicOrContinueNode.OutputTerminal);

            functionalNode.RemoveFromGraph();
        }

        private FunctionalNode CreateInputReplacementNode(FunctionalNode toReplace, Func<Diagram, FunctionalNode> createNode)
        {
            var replacementNode = createNode(toReplace.ParentDiagram);
            AutoBorrowNodeFacade nodeToReplaceFacade = AutoBorrowNodeFacade.GetNodeFacade(toReplace);
            AutoBorrowNodeFacade replacementFacade = AutoBorrowNodeFacade.GetNodeFacade(replacementNode);
            foreach (var terminalPair in toReplace.InputTerminals.Zip(replacementNode.InputTerminals))
            {
                Terminal toReplaceTerminal = terminalPair.Key, replacementTerminal = terminalPair.Value;
                Wire wire = (Wire)toReplaceTerminal.ConnectedTerminal.ParentNode;
                toReplaceTerminal.ConnectedTerminal.ConnectTo(replacementTerminal);
                VariableReference sourceVariable = wire.SourceTerminal.ConnectedTerminal.GetTrueVariable();
                replacementFacade[replacementTerminal]
                    .UnifyWithConnectedWireTypeAsNodeInput(sourceVariable, _unificationResultFactory);
            }
            // TODO: this doesn't work because the Wires connected to createPromiseNode's inputs may have been created during
            // automatic node insertion, and in that case do not have correct facades or variables.
            // createPromiseNode.UnifyNodeInputTerminalTypes(_unificationResultFactory);
            replacementFacade.FinalizeAutoBorrows();
            return replacementNode;
        }

        private static FunctionalNode CreateFunctionalNodeWithFacade(Diagram parentDiagram, NIType signature)
        {
            var node = new FunctionalNode(parentDiagram, signature);
            node.CreateFacadesForFunctionSignatureNode(signature);
            return node;
        }

        private T ConnectNewNodeToOutputTerminal<T>(Node replacement, Func<Diagram, T> createNode) where T : Node
        {
            var nodeToConnect = createNode(replacement.ParentDiagram);
            AutoBorrowNodeFacade nodeToConnectFacade = AutoBorrowNodeFacade.GetNodeFacade(nodeToConnect);
            Terminal inputTerminal = nodeToConnect.InputTerminals[0];
            nodeToConnectFacade[inputTerminal] = new SimpleTerminalFacade(
                inputTerminal,
                replacement.GetTypeVariableSet().CreateReferenceToNewTypeVariable());
            Terminal outputTerminal = replacement.OutputTerminals[0];
            new LiveVariable(outputTerminal.GetTrueVariable(), outputTerminal)
                .ConnectToTerminalAsInputAndUnifyVariables(inputTerminal, _unificationResultFactory);
            return nodeToConnect;
        }

        private void ConnectOutputTerminal(Terminal outputTerminal, Terminal newOutputTerminal)
        {
            AutoBorrowNodeFacade nodeToReplaceFacade = AutoBorrowNodeFacade.GetNodeFacade(outputTerminal.ParentNode);
            AutoBorrowNodeFacade newTerminalNodeFacade = AutoBorrowNodeFacade.GetNodeFacade(newOutputTerminal.ParentNode);
            if (outputTerminal.IsConnected)
            {
                outputTerminal.ConnectedTerminal.ConnectTo(newOutputTerminal);
                newTerminalNodeFacade[newOutputTerminal] = nodeToReplaceFacade[outputTerminal];
            }
            else
            {
                InsertDropTransform.InsertDropForVariable(newOutputTerminal.ParentDiagram, LiveVariable.FromTerminal(newOutputTerminal), _unificationResultFactory);
            }
        }

        private void CreateDefaultFacadeForAwaitOutputTerminal(AwaitNode awaitNode, NIType awaitOutputType)
        {
            AutoBorrowNodeFacade awaitNodeFacade = AutoBorrowNodeFacade.GetNodeFacade(awaitNode);
            awaitNodeFacade[awaitNode.OutputTerminal] = new SimpleTerminalFacade(
                awaitNode.OutputTerminal,
                awaitNode.GetTypeVariableSet().CreateTypeVariableReferenceFromNIType(awaitOutputType));
        }
    }
}
