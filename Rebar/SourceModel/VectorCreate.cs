using System.Linq;
using System.Xml.Linq;
using NationalInstruments.Core;
using NationalInstruments.DataTypes;
using NationalInstruments.SourceModel;
using NationalInstruments.SourceModel.Persistence;
using Rebar.Compiler;
using Rebar.Common;

namespace Rebar.SourceModel
{
    public class VectorCreate : SimpleNode
    {
        private const string ElementName = "VectorCreate";

        protected VectorCreate()
        {
            FixedTerminals.Add(new NodeTerminal(Direction.Output, PFTypes.Int32.CreateVector(), "vector"));
        }

        [XmlParserFactoryMethod(ElementName, Function.ParsableNamespaceName)]
        public static VectorCreate CreateVectorCreate(IElementCreateInfo elementCreateInfo)
        {
            var vectorCreate = new VectorCreate();
            vectorCreate.Init(elementCreateInfo);
            return vectorCreate;
        }

        /// <inheritdoc />
        public override XName XmlElementName => XName.Get(ElementName, Function.ParsableNamespaceName);

        /// <inheritdoc />
        protected override void SetIconViewGeometry()
        {
            Bounds = new SMRect(Left, Top, StockDiagramGeometries.GridSize * 4, StockDiagramGeometries.GridSize * 4);
            var terminals = Terminals.OfType<NodeTerminal>().ToArray();
            terminals[0].Hotspot = new SMPoint(StockDiagramGeometries.GridSize * 4, StockDiagramGeometries.GridSize * 1);
        }

        /// <inheritdoc />
        public override void AcceptVisitor(IElementVisitor visitor)
        {
            var functionVisitor = visitor as IFunctionVisitor;
            if (functionVisitor != null)
            {
                functionVisitor.VisitVectorCreate(this);
            }
            else
            {
                base.AcceptVisitor(visitor);
            }
        }
    }
}
