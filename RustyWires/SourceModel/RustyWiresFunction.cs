using System.Linq;
using NationalInstruments.SourceModel;
using NationalInstruments.SourceModel.Persistence;
using System.Xml.Linq;
using NationalInstruments.MocCommon.SourceModel;
using System.Collections.Generic;
using NationalInstruments.VI.SourceModel;
using RustyWires.Compiler;

namespace RustyWires.SourceModel
{
    public class RustyWiresFunction : DataflowFunctionDefinition
    {
        #region Dynamic Properties

        /// <summary>
        /// Namespace name
        /// </summary>
        public const string ParsableNamespaceName = "http://www.ni.com/RustyWires";

        private const string ElementName = "RustyWiresFunction";

        public const string RustyWiresMocIdentifier = "RustyWiresFunction.Moc";

        #endregion

        /// <summary>
        /// DefinitionType
        /// </summary>
        public const string RustyWiresFunctionDefinitionType = "RustyWires.SourceModel.RustyWiresFunction";

        /// <summary>
        ///  Get the root diagram of the sketch.
        /// </summary>
        public RootDiagram Diagram => Components.OfType<RootDiagram>().Single();

        private RustyWiresFunction()
            : base(new BlockDiagram(), false)
        {
        }

        [ExportDefinitionFactory(RustyWiresFunctionDefinitionType)]
        [StaticBindingKeywords(RustyWiresMocIdentifier)]
        // [StaticBindingKeywords("ProjectItemCopyPasteDefaultService")]
        [XmlParserFactoryMethod(ElementName, ParsableNamespaceName)]
        public static RustyWiresFunction Create(IElementCreateInfo elementCreateInfo)
        {
            var rustyWiresFunction = new RustyWiresFunction();
            rustyWiresFunction.Host = elementCreateInfo.Host;
            rustyWiresFunction.Init(elementCreateInfo);
            return rustyWiresFunction;
        }

        protected override RootDiagram CreateNewRootDiagram()
        {
            return BlockDiagram.Create(ElementCreateInfo.ForNew);
        }

        public override XName XmlElementName => XName.Get(ElementName, RustyWiresFunction.ParsableNamespaceName);

        /// <inheritdoc />
        public override IWiringBehavior WiringBehavior => new VirtualInstrumentWiringBehavior();

        /// <inheritdoc />
        protected override void CreateBatchRules(ICollection<ModelBatchRule> rules)
        {
            rules.Add(new CoreBatchRule());
            // rules.Add(new UIModelContextBatchRule());
            rules.Add(new VerticalGrowNodeBoundsRule());
            // rules.Add(new GroupRule());
            rules.Add(new DockedConstantBatchRule());
            rules.Add(new WiringBatchRule());
            rules.Add(new WireCommentBatchRule());
            rules.Add(new SequenceStructureBatchRule());
            rules.Add(new PairedTunnelBatchRule());
        }

        /// <inheritdoc />
        public override void AcceptVisitor(IElementVisitor visitor)
        {
            var rustyWiresVisitor = visitor as IRustyWiresFunctionVisitor;
            if (rustyWiresVisitor != null)
            {
                rustyWiresVisitor.VisitRustyWiresFunction(this);
            }
            else
            {
                base.AcceptVisitor(visitor);
            }
        }

        // TODO
        public override IEnumerable<IDiagramParameter> Parameters => Enumerable.Empty<IDiagramParameter>();
    }
}
