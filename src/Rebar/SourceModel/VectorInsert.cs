using System.Linq;
using System.Xml.Linq;
using NationalInstruments.Core;
using NationalInstruments.DataTypes;
using NationalInstruments.SourceModel;
using NationalInstruments.SourceModel.Persistence;
using Rebar.Common;
using Rebar.Compiler;

namespace Rebar.SourceModel
{
    public class VectorInsert : SimpleNode
    {
        private const string ElementName = "VectorInsert";

        protected VectorInsert()
        {
            NIType vectorType = PFTypes.Int32.CreateVector();
            FixedTerminals.Add(new NodeTerminal(Direction.Input, vectorType.CreateMutableReference(), "vector in"));
            FixedTerminals.Add(new NodeTerminal(Direction.Input, PFTypes.Int32.CreateImmutableReference(), "index in"));
            FixedTerminals.Add(new NodeTerminal(Direction.Input, PFTypes.Int32, "element"));
            FixedTerminals.Add(new NodeTerminal(Direction.Output, vectorType.CreateMutableReference(), "vector out"));
            FixedTerminals.Add(new NodeTerminal(Direction.Output, PFTypes.Int32.CreateImmutableReference(), "index out"));
        }

        [XmlParserFactoryMethod(ElementName, Function.ParsableNamespaceName)]
        public static VectorInsert CreateVectorInsert(IElementCreateInfo elementCreateInfo)
        {
            var vectorInsert = new VectorInsert();
            vectorInsert.Init(elementCreateInfo);
            return vectorInsert;
        }

        /// <inheritdoc />
        public override XName XmlElementName => XName.Get(ElementName, Function.ParsableNamespaceName);

        /// <inheritdoc />
        protected override void SetIconViewGeometry()
        {
            Bounds = new SMRect(Left, Top, StockDiagramGeometries.GridSize * 4, StockDiagramGeometries.GridSize * 6);
            var terminals = Terminals.OfType<NodeTerminal>().ToArray();
            terminals[0].Hotspot = new SMPoint(0, StockDiagramGeometries.GridSize * 1);
            terminals[1].Hotspot = new SMPoint(0, StockDiagramGeometries.GridSize * 3);
            terminals[2].Hotspot = new SMPoint(0, StockDiagramGeometries.GridSize * 5);
            terminals[3].Hotspot = new SMPoint(StockDiagramGeometries.GridSize * 4, StockDiagramGeometries.GridSize * 1);
            terminals[4].Hotspot = new SMPoint(StockDiagramGeometries.GridSize * 4, StockDiagramGeometries.GridSize * 3);
        }

        /// <inheritdoc />
        public override void AcceptVisitor(IElementVisitor visitor)
        {
            var functionVisitor = visitor as IFunctionVisitor;
            if (functionVisitor != null)
            {
                functionVisitor.VisitVectorInsert(this);
            }
            else
            {
                base.AcceptVisitor(visitor);
            }
        }
    }
}
