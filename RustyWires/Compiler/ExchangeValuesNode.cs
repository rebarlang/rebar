using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using NationalInstruments.Compiler;
using NationalInstruments.Compiler.SemanticAnalysis;
using NationalInstruments.Core;
using NationalInstruments.DataTypes;
using NationalInstruments.Dfir;

namespace RustyWires.Compiler
{
    internal class ExchangeValuesNode : RustyWiresDfirNode, ITypePropagationImplementation
    {
        public ExchangeValuesNode(Node parentNode) : base(parentNode)
        {
            NIType mutableReferenceType = PFTypes.Void.CreateMutableReference();
            CreateTerminal(Direction.Input, mutableReferenceType, "value in 1");
            CreateTerminal(Direction.Input, mutableReferenceType, "value in 2");
            CreateTerminal(Direction.Output, mutableReferenceType, "value out 1");
            CreateTerminal(Direction.Output, mutableReferenceType, "value out 2");
        }

        private ExchangeValuesNode(Node parentNode, ExchangeValuesNode nodeToCopy, NodeCopyInfo nodeCopyInfo)
            : base(parentNode, nodeToCopy, nodeCopyInfo)
        {
        }

        protected override Node CopyNodeInto(Node newParentNode, NodeCopyInfo copyInfo)
        {
            return new ExchangeValuesNode(newParentNode, this, copyInfo);
        }

        /// <inheritdoc />
        public override IEnumerable<PassthroughTerminalPair> PassthroughTerminalPairs
        {
            get
            {
                yield return new PassthroughTerminalPair(Terminals[0], Terminals[2]);
                yield return new PassthroughTerminalPair(Terminals[1], Terminals[3]);
            }
        }

        public Task DoTypePropagationAsync(
            Node node,
            ITypePropagationAccessor typePropagationAccessor,
            CompileCancellationToken cancellationToken)
        {
            var exchangeValuesNode = (ExchangeValuesNode)node;
            Terminal valueIn1Terminal = exchangeValuesNode.Terminals.ElementAt(0);
            Terminal valueIn2Terminal = exchangeValuesNode.Terminals.ElementAt(1);
            Terminal valueOut1Terminal = exchangeValuesNode.Terminals.ElementAt(2);
            Terminal valueOut2Terminal = exchangeValuesNode.Terminals.ElementAt(3);
            exchangeValuesNode.PullInputTypes();
            if (valueIn1Terminal.TestRequiredTerminalConnected())
            {
                valueIn1Terminal.TestTerminalHasMutableTypeConnected();
                NIType valueIn1Type = valueIn1Terminal.DataType;
                NIType value2UnderlyingType = valueIn1Type.GetUnderlyingTypeFromRustyWiresType();
                valueOut1Terminal.DataType = valueIn1Type;
                valueOut1Terminal.SetLifetime(valueIn1Terminal.ComputeInputTerminalEffectiveLifetime());
            }
            else
            {
                valueOut1Terminal.DataType = PFTypes.Void.CreateMutableReference();
            }
            if (valueIn2Terminal.TestRequiredTerminalConnected())
            {
                valueIn2Terminal.TestTerminalHasMutableTypeConnected();
                NIType valueIn2Type = valueIn2Terminal.DataType;
                NIType value2UnderlyingType = valueIn2Type.GetUnderlyingTypeFromRustyWiresType();
                valueOut2Terminal.DataType = valueIn2Type;
                valueOut2Terminal.SetLifetime(valueIn2Terminal.ComputeInputTerminalEffectiveLifetime());
            }
            else
            {
                valueIn2Terminal.DataType = PFTypes.Void.CreateMutableReference();
            }
            // TODO: propagate lifetimes; ensure that lifetimes of exchanged values and references are compatible
            return AsyncHelpers.CompletedTask;
        }
    }
}
