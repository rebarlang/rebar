using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using NationalInstruments.Core;
using NationalInstruments.SourceModel;
using NationalInstruments.SourceModel.Persistence;

namespace RustyWires.SourceModel
{
    /// <summary>
    /// Generic iteration control structure that can allow one or more left-side border nodes to control whether it stops.
    /// </summary>
    public class Loop : SimpleStructure
    {
        private const string ElementName = "Loop";

        [XmlParserFactoryMethod(ElementName, RustyWiresFunction.ParsableNamespaceName)]
        public static Loop CreateLoop(IElementCreateInfo elementCreateInfo)
        {
            var loop = new Loop();
            loop.Init(elementCreateInfo);
            return loop;
        }

        protected override void Init(IElementCreateInfo info)
        {
            base.Init(info);
            if (info.ForParse)
            {
                string fixupName = nameof(FixupLoop);
                info.FixupRegistrar.RegisterPostParseFixup(this, FixupLoop, fixupName);
                info.FixupRegistrar.AddPostParseFixupOrder(IdReferenceBaseSerializer.IdReferenceFixupName, fixupName);
            }
            else
            {
                var loopConditionTunnel = MakeTunnel<LoopConditionTunnel>(Diagram, NestedDiagrams.First());
                var loopTerminateLifetimeTunnel = MakeTunnel<LoopTerminateLifetimeTunnel>(Diagram, NestedDiagrams.First());
                loopConditionTunnel.TerminateLifetimeTunnel = loopTerminateLifetimeTunnel;
                loopTerminateLifetimeTunnel.BeginLifetimeTunnel = loopConditionTunnel;
            }
        }

        /// <summary>
        /// Fixup used to fully initialize Loop after parsing.
        /// </summary>
        /// <param name="element">The Loop element that was parsed</param>
        /// <param name="services">The IElementServices used for the parsing operation</param>
        protected static void FixupLoop(Element element, IElementServices services)
        {
            var loop = (Loop)element;
            loop.EnsureView(EnsureViewHints.Bounds);

            foreach (var tunnel in loop.BorderNodes)
            {
                // This makes sure inner/outer terminals are all set.
                tunnel.EnsureView(EnsureViewHints.Bounds);
            }
        }

        /// <inheritdoc />
        public override XName XmlElementName => XName.Get(ElementName, RustyWiresFunction.ParsableNamespaceName);

        /// <inheritdoc />
        public override BorderNode MakeDefaultBorderNode(Diagram startDiagram, Diagram endDiagram, Wire wire, StructureIntersection intersection)
        {
            return MakeBorderNode<LoopTunnel>();
        }

        /// <inheritdoc />
        /// <remarks>Adapted from NationalInstruments.VI.SourceModel.Loop.GetGuide.</remarks>
        public override IBorderNodeGuide GetGuide(BorderNode borderNode)
        {
            var loopTerminateLifetimeTunnel = borderNode as LoopTerminateLifetimeTunnel;
            if (loopTerminateLifetimeTunnel != null)
            {
                var height = GetMaxXYForBorderNode(this, borderNode).Y + borderNode.Height + OuterBorderThickness.Bottom;
                List<BorderNode> except = new List<BorderNode>()
                {
                    (BorderNode)loopTerminateLifetimeTunnel.BeginLifetimeTunnel,
                    loopTerminateLifetimeTunnel
                };
                return new TerminateLifetimeTunnelGuide(
                    new SMRect(0, 0, Width, height),
                    loopTerminateLifetimeTunnel,
                    OuterBorderThickness,
                    BorderNodes.Except(except).Select(node => node.Bounds))
                {
                    EdgeOverflow = StockDiagramGeometries.StandardTunnelOffsetForStructures
                };
            }
            else if (borderNode is LoopBorrowTunnel || borderNode is LoopConditionTunnel)
            {
                var height = GetMaxXYForBorderNode(this, borderNode).Y + borderNode.Height + OuterBorderThickness.Bottom;
                // TerminateLifetimeTunnels do all the moving, but this guide ensures the left node is not out of place or on the wrong docking side.
                RectangleBorderNodeGuide guide = new RectangleBorderNodeGuide(
                    new SMRect(0, 0, Width, height),
                    RectangleSides.Left,
                    BorderNodeDocking.None,
                    OuterBorderThickness,
                    BorderNodes.Where(node => node != borderNode).Select(node => node.Bounds));
                guide.EdgeOverflow = StockDiagramGeometries.StandardTunnelOffsetForStructures;
                return guide;
            }
            return base.GetGuide(borderNode);
        }
    }
}
