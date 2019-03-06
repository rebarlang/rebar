﻿using System;
using System.Linq;
using NationalInstruments.Compiler;
using NationalInstruments.Compiler.SemanticAnalysis;
using NationalInstruments.Dfir;
using Rebar.Common;
using Rebar.Compiler.Nodes;

namespace Rebar.Compiler
{
    internal class ExplicitBorrowTransform : IDfirTransform
    {
        public void Execute(DfirRoot dfirRoot, CompileCancellationToken cancellationToken)
        {
            VisitDiagram(dfirRoot.BlockDiagram);
        }

        private void VisitDiagram(Diagram diagram)
        {
            foreach (var node in diagram.Nodes.ToList())
            {
                var structure = node as Structure;
                var wire = node as Wire;
                if (structure != null)
                {
                    VisitStructure(structure);
                }
                else if (wire != null)
                {
                    VisitWire(wire);
                }
                else
                {
                    VisitNode(node);
                }
            }
        }

        private void VisitWire(Wire wire)
        {
            Terminal sourceTerminal;
            if (!wire.TryGetSourceTerminal(out sourceTerminal))
            {
                return;
            }
            sourceTerminal.PullInputType();
            foreach (var sinkTerminal in wire.SinkTerminals)
            {
                sinkTerminal.DataType = sourceTerminal.DataType;
            }
        }

        private void VisitNode(Node node)
        {
            var passthroughTerminalsNode = node as IPassthroughTerminalsNode;
            if (passthroughTerminalsNode != null)
            {
                foreach (var passthroughTerminalPair in passthroughTerminalsNode.PassthroughTerminalPairs)
                {
                    // determine whether the type wired to the input has a higher permissiveness than the input's formal type
                    Terminal inputTerminal = passthroughTerminalPair.InputTerminal;
                    if (passthroughTerminalPair.InputTerminal.IsConnected)
                    {
                        TypePermissiveness connectedPermissiveness =
                                passthroughTerminalPair.InputTerminal.ConnectedTerminal.DataType
                                    .GetTypePermissiveness(),
                            inputPermissiveness =
                                passthroughTerminalPair.InputTerminal.DataType.GetTypePermissiveness();
                        if (connectedPermissiveness > inputPermissiveness)
                        {
                            // add an explicit borrow before the input terminal and an explicit borrow after the output terminal
                            Nodes.BorrowMode borrowMode = DataTypes.GetBorrowMode(connectedPermissiveness, inputPermissiveness);
                            var explicitBorrow = new ExplicitBorrowNode(node.ParentNode, borrowMode);
                            var explicitUnborrow = new ExplicitUnborrowNode(node.ParentNode, borrowMode);
                            passthroughTerminalPair.InputTerminal.ConnectedTerminal.ConnectTo(explicitBorrow.InputTerminal);
                            explicitBorrow.OutputTerminal.WireTogether(passthroughTerminalPair.InputTerminal, SourceModelIdSource.NoSourceModelId);
                            if (passthroughTerminalPair.OutputTerminal.IsConnected)
                            {
                                passthroughTerminalPair.OutputTerminal.ConnectTo(explicitUnborrow.OutputTerminal);
                            }
                            passthroughTerminalPair.OutputTerminal.WireTogether(explicitUnborrow.InputTerminal, SourceModelIdSource.NoSourceModelId);
                        }
                    }
                }
            }
        }

        private void VisitStructure(Structure structure)
        {
            throw new NotImplementedException();
        }
    }
}