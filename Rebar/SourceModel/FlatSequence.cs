using System.Xml.Linq;
using NationalInstruments.Core;
using NationalInstruments.SourceModel;
using NationalInstruments.SourceModel.Persistence;
using NationalInstruments.VI.SourceModel;

namespace Rebar.SourceModel
{
    public class FlatSequence : NationalInstruments.VI.SourceModel.FlatSequence
    {
        private const string ElementName = "FlatSequence";

        [XmlParserFactoryMethod(ElementName, Function.ParsableNamespaceName)]
        public static FlatSequence CreateFlatSequence(IElementCreateInfo elementCreateInfo)
        {
            var flatSequence = new FlatSequence();
            flatSequence.Init(elementCreateInfo);
            return flatSequence;
        }

        public override XName XmlElementName => XName.Get(ElementName, Function.ParsableNamespaceName);

        public override BorderNode MakeDefaultBorderNode(
            Diagram startDiagram,
            Diagram endDiagram, 
            Wire wire,
            StructureIntersection intersection)
        {
            return MakeTunnel<FlatSequenceSimpleTunnel>(startDiagram, endDiagram);
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

    public class FlatSequenceSimpleTunnel : FlatSequenceTunnel
    {
        private const string ElementName = "FlatSequenceSimpleTunnel";

        [XmlParserFactoryMethod(ElementName, Function.ParsableNamespaceName)]
        public static FlatSequenceSimpleTunnel CreateFlatSequenceSimpleTunnel(IElementCreateInfo elementCreateInfo)
        {
            var flatSequenceSimpleTunnel = new FlatSequenceSimpleTunnel();
            flatSequenceSimpleTunnel.Init(elementCreateInfo);
            return flatSequenceSimpleTunnel;
        }

        public override XName XmlElementName => XName.Get(ElementName, Function.ParsableNamespaceName);
    }

    public enum TunnelMode
    {
        BorrowMutable,
        BorrowImmutable
    }
}
