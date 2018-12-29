using System.Xml.Linq;
using NationalInstruments.Core;
using NationalInstruments.SourceModel;
using NationalInstruments.SourceModel.Persistence;
using NationalInstruments.VI.SourceModel;

namespace RustyWires.SourceModel
{
    public class RustyWiresFlatSequence : FlatSequence
    {
        private const string ElementName = "RustyWiresFlatSequence";

        [XmlParserFactoryMethod(ElementName, RustyWiresFunction.ParsableNamespaceName)]
        public static RustyWiresFlatSequence CreateRustyWiresFlatSequence(IElementCreateInfo elementCreateInfo)
        {
            var rustyWiresFlatSequence = new RustyWiresFlatSequence();
            rustyWiresFlatSequence.Init(elementCreateInfo);
            return rustyWiresFlatSequence;
        }

        public override XName XmlElementName => XName.Get(ElementName, RustyWiresFunction.ParsableNamespaceName);

        public override BorderNode MakeDefaultBorderNode(
            Diagram startDiagram,
            Diagram endDiagram, 
            Wire wire,
            StructureIntersection intersection)
        {
            return MakeTunnel<RustyWiresFlatSequenceSimpleTunnel>(startDiagram, endDiagram);
        }

        /// <inheritdoc />
        public override SMSize GetDesiredBorderNodeSize(BorderNode borderNode)
        {
            if (borderNode is BorrowTunnel 
                || borderNode is FlatSequenceTerminateLifetimeTunnel
                || borderNode is LockTunnel
                || borderNode is UnwrapOptionTunnel)
            {
                return new SMSize(StockDiagramGeometries.GridSize * 4, StockDiagramGeometries.GridSize * 4);
            }
            return base.GetDesiredBorderNodeSize(borderNode);
        }
    }

    public class RustyWiresFlatSequenceSimpleTunnel : FlatSequenceTunnel
    {
        private const string ElementName = "RustyWiresFlatSequenceSimpleTunnel";

        [XmlParserFactoryMethod(ElementName, RustyWiresFunction.ParsableNamespaceName)]
        public static RustyWiresFlatSequenceSimpleTunnel CreateRustyWiresFlatSequenceSimpleTunnel(IElementCreateInfo elementCreateInfo)
        {
            var rustyWiresFlatSequenceSimpleTunnel = new RustyWiresFlatSequenceSimpleTunnel();
            rustyWiresFlatSequenceSimpleTunnel.Init(elementCreateInfo);
            return rustyWiresFlatSequenceSimpleTunnel;
        }

        public override XName XmlElementName => XName.Get(ElementName, RustyWiresFunction.ParsableNamespaceName);
    }

    public enum RustyWiresTunnelMode
    {
        BorrowMutable,
        BorrowImmutable
    }
}
