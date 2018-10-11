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
    internal class SelectReferenceNode : RustyWiresDfirNode, ITypePropagationImplementation
    {
        public SelectReferenceNode(Node parentNode) : base(parentNode)
        {
            var immutableReferenceType = PFTypes.Void.CreateImmutableReference();
            CreateTerminal(Direction.Input, immutableReferenceType, "ref in 1");
            CreateTerminal(Direction.Input, immutableReferenceType, "ref in 2");
            CreateTerminal(Direction.Output, immutableReferenceType, "ref out");
        }

        private SelectReferenceNode(Node parentNode, SelectReferenceNode nodeToCopy, NodeCopyInfo nodeCopyInfo)
            : base(parentNode, nodeToCopy, nodeCopyInfo)
        {
        }

        protected override Node CopyNodeInto(Node newParentNode, NodeCopyInfo copyInfo)
        {
            return new SelectReferenceNode(newParentNode, this, copyInfo);
        }

        /// <inheritdoc />
        public override IEnumerable<PassthroughTerminalPair> PassthroughTerminalPairs => Enumerable.Empty<PassthroughTerminalPair>();

        public Task DoTypePropagationAsync(
            Node node,
            ITypePropagationAccessor typePropagationAccessor,
            CompileCancellationToken cancellationToken)
        {
            var selectReferenceNode = (SelectReferenceNode)node;
            selectReferenceNode.PullInputTypes();

            Terminal refInTerminal1 = selectReferenceNode.Terminals.ElementAt(0),
                refInTerminal2 = selectReferenceNode.Terminals.ElementAt(1),
                refOutTerminal = selectReferenceNode.Terminals.ElementAt(2);
            bool terminal1Connected = refInTerminal1.TestRequiredTerminalConnected();
            bool terminal2Connected = refInTerminal2.TestRequiredTerminalConnected();
            NIType refInType1 = refInTerminal1.DataType;
            NIType refInType2 = refInTerminal2.DataType;
            if (terminal1Connected)
            {
                if (refInType1.IsRWReferenceType() && refInTerminal1.GetSourceLifetime().IsEmpty)
                {
                    refInTerminal1.SetDfirMessage(RustyWiresMessages.WiredReferenceDoesNotLiveLongEnough);
                }
            }
            if (terminal2Connected)
            {
                if (refInType2.IsRWReferenceType() && refInTerminal2.GetSourceLifetime().IsEmpty)
                {
                    refInTerminal2.SetDfirMessage(RustyWiresMessages.WiredReferenceDoesNotLiveLongEnough);
                }
            }
            if (terminal1Connected && terminal2Connected)
            {
                NIType underlyingType1 = refInType1.GetUnderlyingTypeFromRustyWiresType();
                NIType underlyingType2 = refInType2.GetUnderlyingTypeFromRustyWiresType();
                if (underlyingType1 == underlyingType2)
                {
                    // compatible types
                    refOutTerminal.DataType = underlyingType1.CreateImmutableReference();
                }
                else
                {
                    refOutTerminal.DataType = PFTypes.Void.CreateImmutableReference();
                }

                Lifetime refInLifetime1 = refInTerminal1.ComputeInputTerminalEffectiveLifetime();
                Lifetime refInLifetime2 = refInTerminal2.ComputeInputTerminalEffectiveLifetime();
                Lifetime commonLifetime = node.DfirRoot.GetLifetimeSet().ComputeCommonLifetime(refInLifetime1, refInLifetime2);
                refOutTerminal.SetLifetime(commonLifetime);
            }
            else
            {
                refOutTerminal.DataType = PFTypes.Void.CreateImmutableReference();
            }
            return AsyncHelpers.CompletedTask;
        }
    }
}
