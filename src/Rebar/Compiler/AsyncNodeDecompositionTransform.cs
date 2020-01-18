using System;
using System.Collections.Generic;
using System.Linq;
using NationalInstruments;
using NationalInstruments.Compiler;
using NationalInstruments.DataTypes;
using NationalInstruments.Dfir;
using Rebar.Common;
using Rebar.Compiler.Nodes;

namespace Rebar.Compiler
{
    internal class AsyncNodeDecompositionTransform : IDfirTransform
    {
        private static readonly Dictionary<string, NIType> _createPromiseSignatures;

        static AsyncNodeDecompositionTransform()
        {
            _createPromiseSignatures = new Dictionary<string, NIType>();
            _createPromiseSignatures["Yield"] = Signatures.CreateYieldPromiseType;
            _createPromiseSignatures["GetNotifierValue"] = Signatures.GetReaderPromiseType;
        }

        private readonly ITypeUnificationResultFactory _unificationResultFactory;

        public AsyncNodeDecompositionTransform(ITypeUnificationResultFactory unificationResultFactory)
        {
            _unificationResultFactory = unificationResultFactory;
        }

        public void Execute(DfirRoot dfirRoot, CompileCancellationToken cancellationToken)
        {
            var methodCallNodes = dfirRoot.GetAllNodesIncludingSelf().Where(CanDecompose).ToList();
            methodCallNodes.ForEach(Decompose);
        }

        private static bool CanDecompose(Node node)
        {
            if (node is MethodCallNode)
            {
                return true;
            }
            var functionalNode = node as FunctionalNode;
            return functionalNode != null && _createPromiseSignatures.ContainsKey(functionalNode.Signature.GetName());
        }

        private void Decompose(Node node)
        {
            var methodCallNode = node as MethodCallNode;
            if (methodCallNode != null)
            {
                DecomposeMethodCall(methodCallNode);
                return;
            }
            var functionalNode = node as FunctionalNode;
            if (functionalNode != null)
            {
                DecomposeAsyncFunctionalNode(functionalNode, _createPromiseSignatures[functionalNode.Signature.GetName()]);
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

            AwaitNode awaitNode = ConnectAwaitNodeToPromiseTerminal(createMethodCallPromise, promiseType);

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

            var createPromiseNode = new FunctionalNode(functionalNode.ParentDiagram, createPromiseSignature);
            createPromiseNode.CreateFacadesForFunctionSignatureNode(createPromiseSignature);
            AutoBorrowNodeFacade nodeToReplaceFacade = AutoBorrowNodeFacade.GetNodeFacade(functionalNode);
            AutoBorrowNodeFacade replacementFacade = AutoBorrowNodeFacade.GetNodeFacade(createPromiseNode);
            foreach (var terminalPair in functionalNode.InputTerminals.Zip(createPromiseNode.InputTerminals))
            {
                Terminal methodCallTerminal = terminalPair.Key, createMethodCallPromiseTerminal = terminalPair.Value;
                Wire wire = (Wire)methodCallTerminal.ConnectedTerminal.ParentNode;
                methodCallTerminal.ConnectedTerminal.ConnectTo(createMethodCallPromiseTerminal);
                VariableReference sourceVariable = wire.SourceTerminal.ConnectedTerminal.GetTrueVariable();
                replacementFacade[createMethodCallPromiseTerminal]
                    .UnifyWithConnectedWireTypeAsNodeInput(sourceVariable, _unificationResultFactory);
            }
            // TODO: this doesn't work because the Wires connected to createPromiseNode's inputs may have been created during
            // automatic node insertion, and in that case do not have correct facades or variables.
            // createPromiseNode.UnifyNodeInputTerminalTypes(_unificationResultFactory);
            replacementFacade.FinalizeAutoBorrows();

            Terminal createPromiseNodeOutputTerminal = createPromiseNode.OutputTerminals[0];
            NIType promiseType = createPromiseNodeOutputTerminal.GetTrueVariable().Type;

            AwaitNode awaitNode = ConnectAwaitNodeToPromiseTerminal(createPromiseNode, promiseType);

            ConnectOutputTerminal(functionalNode.OutputTerminals[0], awaitNode.OutputTerminal);

            functionalNode.RemoveFromGraph();
        }

        private AwaitNode ConnectAwaitNodeToPromiseTerminal(Node replacement, NIType promiseType)
        {
            var awaitNode = new AwaitNode(replacement.ParentDiagram);
            AutoBorrowNodeFacade awaitNodeFacade = AutoBorrowNodeFacade.GetNodeFacade(awaitNode);
            awaitNodeFacade[awaitNode.InputTerminal] = new SimpleTerminalFacade(
                awaitNode.InputTerminal,
                replacement.GetTypeVariableSet().CreateReferenceToNewTypeVariable());
            Terminal promiseTerminal = replacement.OutputTerminals[0];
            new LiveVariable(promiseTerminal.GetTrueVariable(), promiseTerminal)
                .ConnectToTerminalAsInputAndUnifyVariables(awaitNode.InputTerminal, _unificationResultFactory);
            return awaitNode;
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
