using System;
using System.Linq;
using System.Xml.Linq;
using NationalInstruments.CommonModel;
using NationalInstruments.Core;
using NationalInstruments.DataTypes;
using NationalInstruments.SourceModel;
using NationalInstruments.SourceModel.Persistence;
using Rebar.Common;
using Rebar.Compiler;

namespace Rebar.SourceModel
{
    public class TerminateLifetime : SimpleNode
    {
        private const string ElementName = "TerminateLifetime";

        protected TerminateLifetime()
        {
            var immutableReferenceType = NITypes.Void.CreateImmutableReference();
            var inputTerminal = new NodeTerminal(Direction.Input, immutableReferenceType, "inner lifetime 0");
            FixedTerminals.Add(inputTerminal);
            var outputTerminal = new NodeTerminal(Direction.Output, immutableReferenceType, "outer lifetime");
            FixedTerminals.Add(outputTerminal);
        }

        [XmlParserFactoryMethod(ElementName, Function.ParsableNamespaceName)]
        public static TerminateLifetime CreateTerminateLifetime(IElementCreateInfo elementCreateInfo)
        {
            var createCell = new TerminateLifetime();
            createCell.Initialize(elementCreateInfo);
            return createCell;
        }

        public override XName XmlElementName => XName.Get(ElementName, Function.ParsableNamespaceName);

        protected override void SetIconViewGeometry()
        {
            int maxSideTerminals = Math.Max(InputTerminals.Count(), OutputTerminals.Count());
            Bounds = new SMRect(Left, Top, StockDiagramGeometries.GridSize * 4, StockDiagramGeometries.GridSize * Math.Max(maxSideTerminals, 2) * 2);
            int i = 0;
            foreach (var inputTerminal in InputTerminals)
            {
                inputTerminal.Hotspot = new SMPoint(0, StockDiagramGeometries.GridSize * (i * 2 + 1));
                ++i;
            }
            i = 0;
            foreach (var outputTerminal in OutputTerminals)
            {
                outputTerminal.Hotspot = new SMPoint(StockDiagramGeometries.GridSize * 4, StockDiagramGeometries.GridSize * (i * 2 + 1));
                ++i;
            }
        }

        public void UpdateTerminals(int inputTerminalCount, int outputTerminalCount)
        {
            var immutableReferenceType = NITypes.Void.CreateImmutableReference();
            int currentInputTerminalCount = InputTerminals.Count();
            if (currentInputTerminalCount < inputTerminalCount)
            {
                for (; currentInputTerminalCount < inputTerminalCount; ++currentInputTerminalCount)
                {
                    var inputTerminal = new NodeTerminal(Direction.Input, immutableReferenceType, "nested scope");
                    InsertComponent(currentInputTerminalCount, inputTerminal);
                }
            }
            else if (currentInputTerminalCount > inputTerminalCount)
            {
                int i = currentInputTerminalCount - 1;
                while (i >= 0 && currentInputTerminalCount > inputTerminalCount)
                {
                    Terminal inputTerminal = InputTerminals.ElementAt(i);
                    if (!inputTerminal.Connected)
                    {
                        RemoveComponent(inputTerminal);
                        --currentInputTerminalCount;
                    }
                    --i;
                }
            }

            int currentOutputTerminalCount = OutputTerminals.Count();
            if (currentOutputTerminalCount < outputTerminalCount)
            {
                for (; currentOutputTerminalCount < outputTerminalCount; ++currentOutputTerminalCount)
                {
                    var outputTerminal = new NodeTerminal(Direction.Output, immutableReferenceType, "outer scope");
                    InsertComponent(currentInputTerminalCount + currentOutputTerminalCount, outputTerminal);
                }
            }
            else if (currentOutputTerminalCount > outputTerminalCount)
            {
                int i = currentOutputTerminalCount - 1;
                while (i >= 0 && currentOutputTerminalCount > outputTerminalCount)
                {
                    Terminal outputTerminal = OutputTerminals.ElementAt(i);
                    if (!outputTerminal.Connected)
                    {
                        RemoveComponent(outputTerminal);
                        --currentOutputTerminalCount;
                    }
                    --i;
                }
            }

            SetIconViewGeometry();
        }

        /// <inheritdoc />
        public override void AcceptVisitor(IElementVisitor visitor)
        {
            var functionVisitor = visitor as IFunctionVisitor;
            if (functionVisitor != null)
            {
                functionVisitor.VisitTerminateLifetimeNode(this);
            }
            else
            {
                base.AcceptVisitor(visitor);
            }
        }
    }
}
