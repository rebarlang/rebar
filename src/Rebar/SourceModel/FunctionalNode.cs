using System;
using System.Collections.Generic;
using System.Linq;
using NationalInstruments.CommonModel;
using NationalInstruments.Core;
using NationalInstruments.DataTypes;
using NationalInstruments.SourceModel;
using Rebar.Common;
using Rebar.Compiler;

namespace Rebar.SourceModel
{
    public abstract class FunctionalNode : SimpleNode
    {
        protected FunctionalNode(NIType signature)
        {
            Signature = signature;
            foreach (NodeTerminal terminal in CreateNodeTerminalsFromSignature(signature))
            {
                FixedTerminals.Add(terminal);
            }
            Width = StockDiagramGeometries.GridSize * 4;
        }

        public NIType Signature { get; }

        protected virtual float MinimumHeight => StockDiagramGeometries.GridSize * 2;

        public virtual IEnumerable<string> RequiredFeatureToggles => Enumerable.Empty<string>();

        /// <inheritdoc />
        protected override void SetIconViewGeometry()
        {
            SetIconNodeGeometry(this, FixedTerminals.Cast<NodeTerminal>());
        }

        private static List<NodeTerminal> CreateNodeTerminalsFromSignature(NIType functionSignature)
        {
            List<NodeTerminal> terminals = new List<NodeTerminal>();
            Signature signature = Signatures.GetSignatureForNIType(functionSignature);
            foreach (SignatureTerminal signatureTerminal in signature.Inputs)
            {
                terminals.Add(new NodeTerminal(Direction.Input, signatureTerminal.DisplayType, signatureTerminal.Name));
            }
            foreach (SignatureTerminal signatureTerminal in signature.Outputs)
            {
                terminals.Add(new NodeTerminal(Direction.Output, signatureTerminal.DisplayType, signatureTerminal.Name));
            }
            return terminals;
        }

        private static void SetIconNodeGeometry(FunctionalNode node, IEnumerable<NodeTerminal> terminals)
        {
            int inputs = 0, outputs = 0;
            foreach (NodeTerminal terminal in terminals)
            {
                if (terminal.Direction == Direction.Input)
                {
                    ++inputs;
                    terminal.Hotspot = new SMPoint(0, StockDiagramGeometries.GridSize * (2 * inputs - 1));
                }
                else
                {
                    ++outputs;
                    terminal.Hotspot = new SMPoint(node.Width, StockDiagramGeometries.GridSize * (2 * outputs - 1));
                }
            }
            int rows = Math.Max(1, Math.Max(inputs, outputs));
            float currentHeight = node.Bounds.Height;
            node.Bounds = new SMRect(
                node.Left,
                node.Top,
                node.Width,
                Math.Max(node.MinimumHeight, StockDiagramGeometries.GridSize * 2 * rows));
        }

        /// <inheritdoc />
        public override void AcceptVisitor(IElementVisitor visitor)
        {
            var functionVisitor = visitor as IFunctionVisitor;
            if (functionVisitor != null)
            {
                functionVisitor.VisitFunctionalNode(this);
            }
            else
            {
                base.AcceptVisitor(visitor);
            }
        }
    }
}
