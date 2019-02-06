using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using NationalInstruments.Core;
using NationalInstruments.DataTypes;
using NationalInstruments.SourceModel;
using NationalInstruments.SourceModel.Persistence;
using RustyWires.Common;
using RustyWires.Compiler;

namespace RustyWires.SourceModel
{
    public class CreateCell : RustyWiresSimpleNode
    {
        private const string ElementName = "CreateCell";

        protected CreateCell()
        {
            var immutableReferenceType = PFTypes.Void.CreateImmutableReference();
            FixedTerminals.Add(new NodeTerminal(Direction.Input, PFTypes.Void, "value in"));
            FixedTerminals.Add(new NodeTerminal(Direction.Output, PFTypes.Void.CreateLockingCell(), "cell out"));
        }

        [XmlParserFactoryMethod(ElementName, RustyWiresFunction.ParsableNamespaceName)]
        public static CreateCell CreateCreateCell(IElementCreateInfo elementCreateInfo)
        {
            var createCell = new CreateCell();
            createCell.Init(elementCreateInfo);
            return createCell;
        }

        public override XName XmlElementName => XName.Get(ElementName, RustyWiresFunction.ParsableNamespaceName);

        protected override void SetIconViewGeometry()
        {
            Bounds = new SMRect(Left, Top, StockDiagramGeometries.GridSize * 4, StockDiagramGeometries.GridSize * 4);
            var terminals = Terminals.OfType<NodeTerminal>().ToArray();
            terminals[0].Hotspot = new SMPoint(0, StockDiagramGeometries.GridSize * 1);
            terminals[1].Hotspot = new SMPoint(StockDiagramGeometries.GridSize * 4, StockDiagramGeometries.GridSize * 1);
        }

        /// <inheritdoc />
        public override void AcceptVisitor(IElementVisitor visitor)
        {
            var rustyWiresVisitor = visitor as IRustyWiresFunctionVisitor;
            if (rustyWiresVisitor != null)
            {
                rustyWiresVisitor.VisitCreateCellNode(this);
            }
            else
            {
                base.AcceptVisitor(visitor);
            }
        }
    }
}
