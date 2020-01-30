using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Xml.Linq;
using NationalInstruments.Core;
using NationalInstruments.DataTypes;
using NationalInstruments.DynamicProperties;
using NationalInstruments.Linking;
using NationalInstruments.SourceModel;
using NationalInstruments.SourceModel.Envoys;
using NationalInstruments.SourceModel.Persistence;
using Rebar.Compiler;
using Rebar.SourceModel.TypeDiagram;

namespace Rebar.SourceModel
{
    public class StructFieldAccessor : VerticalGrowNode, IViewVerticalGrowNode, IQualifiedSource, IDependencyTargetExportChanged
    {
        private const string ElementName = "StructFieldAccessor";

        protected StructFieldAccessor()
        {
            StructInputTerminal = new NodeTerminal(Direction.Input, PFTypes.Void, "valueRef", TerminalHotspots.Input1);
            StructType = PFTypes.Void;
        }

        [XmlParserFactoryMethod(ElementName, Function.ParsableNamespaceName)]
        public static StructFieldAccessor CreateStructFieldAccessor(IElementCreateInfo elementCreateInfo)
        {
            var structFieldAccessor = new StructFieldAccessor();
            structFieldAccessor.Init(elementCreateInfo);
            return structFieldAccessor;
        }

        /// <inheritdoc />
        public override XName XmlElementName => XName.Get(ElementName, Function.ParsableNamespaceName);

        /// <inheritdoc />
        public override IEnumerable<Element> ComponentsForGenerate(ElementGenerationOptions options) =>
            base.ComponentsForGenerate(options).Concat(FieldTerminals);

        public NIType StructType { get; private set; }

        public NodeTerminal StructInputTerminal { get; }

        public IEnumerable<StructFieldAccessorTerminal> FieldTerminals => OutputTerminals.OfType<StructFieldAccessorTerminal>();

        /// <inheritdoc />
        protected override void Init(IElementCreateInfo info)
        {
            base.Init(info);

            AddComponent(StructInputTerminal);

            if (info.ForParse)
            {
                info.FixupRegistrar.RegisterPostParseFixup(this, PostParseFixup);
            }
        }

        private void PostParseFixup(Element parsedElement, IElementServices elementServices)
        {
            VerticalChunkCount = FieldTerminals.Count();
            this.RecalculateNodeHeight();
        }

        /// <inheritdoc />
        protected override ViewElementTemplate DefaultTemplate => ViewElementTemplate.List;

        /// <inheritdoc />
        public override void AcceptVisitor(IElementVisitor visitor)
        {
            var functionVisitor = visitor as IFunctionVisitor;
            if (functionVisitor != null)
            {
                functionVisitor.VisitStructFieldAccessor(this);
            }
            else
            {
                base.AcceptVisitor(visitor);
            }
        }

        public override IDocumentation CreateDocumentationForTerminal(Terminal terminal)
        {
            var structFieldAccessorTerminal = terminal as StructFieldAccessorTerminal;
            if (structFieldAccessorTerminal != null)
            {
                return new Documentation() { Name = structFieldAccessorTerminal.FieldName };
            }
            return base.CreateDocumentationForTerminal(terminal);
        }

        internal void UpdateStructType(NIType type)
        {
            NIType oldType = StructType;
            if (oldType != type)
            {
                StructType = type;
                TransactionRecruiter.EnlistPropertyItem(this, nameof(StructType), oldType, type, (t, _) => StructType = t, TransactionHints.Semantic);
            }
        }

        internal void UpdateDependencies(NIType type)
        {
            QualifiedName oldStructName = StructTypeName, newStructName = GetStructTypeName(type);
            if (oldStructName == newStructName)
            {
                return;
            }

            bool needsDependency = !newStructName.IsEmpty, hasDependency = false;
            foreach (TypeDiagramDependency typeDiagramDependency in Dependencies.OfType<TypeDiagramDependency>(this).ToList())
            {
                if (typeDiagramDependency.TargetName == newStructName)
                {
                    hasDependency = true;
                }
                else
                {
                    Dependencies.Remove(this, typeDiagramDependency);
                }
            }
            if (needsDependency && !hasDependency)
            {
                Dependencies.Add(this, new TypeDiagramDependency(this, newStructName));
            }
        }

        private QualifiedName StructTypeName => GetStructTypeName(StructType);

        private static QualifiedName GetStructTypeName(NIType structType) => structType.IsValueClass()
            ? structType.GetTypeDefinitionQualifiedName()
            : QualifiedName.Empty;

        #region VerticalGrowNode overrides

        /// <inheritdoc />
        public override IList<WireableTerminal> CreateTerminalsForVerticalChunk(int chunkIndex)
        {
            return new List<WireableTerminal>
            {
                new StructFieldAccessorTerminal(
                    Direction.Output,
                    PFTypes.Void,
                    "element",
                    new SMPoint(Width, StockDiagramGeometries.StandardTerminalHeight / 2))
            };
        }

        /// <inheritdoc />
        public override int FixedTerminalCount => 1;

        /// <inheritdoc />
        public override int MinimumVerticalChunkCount => 1;

        /// <inheritdoc />
        public override int GetNumberOfTerminalsInVerticalChunk(int chunkIndex) => 1;

        #endregion

        #region IViewVerticalGrowNode implementation

        public float TopMargin => Template == ViewElementTemplate.List ? StockDiagramGeometries.ListViewHeaderHeight : 0;

        public float BottomMargin => Template == ViewElementTemplate.List ? ListViewFooterHeight : 0;

        public float TerminalHeight => Template == ViewElementTemplate.List ? StockDiagramGeometries.LargeTerminalHeight : StockDiagramGeometries.StandardTerminalHeight;

        public float TerminalHotspotVerticalOffset => TerminalHotspots.HotspotVerticalOffsetForTerminalSize(TerminalSize.Small);

        public float GetVerticalChunkHeight(int chunkIndex) => TerminalHeight;

        public float OffsetForVerticalChunk(int chunkIndex) => TopMargin + chunkIndex * this.GetFixedSizeVerticalChunkHeight();

        public float NodeHeightForVerticalChunkCount(int chunkCount) => OffsetForVerticalChunk(chunkCount) + BottomMargin;

        #endregion

        private DependencyCollection _dependencies;

        /// <inheritdoc />
        public DependencyCollection Dependencies
        {
            get
            {
                QualifiedName structTypeName = StructTypeName;
                IQualifiedDependency[] dependencyArray = structTypeName.IsEmpty
                    ? new IQualifiedDependency[0]
                    : new IQualifiedDependency[] { new TypeDiagramDependency(this, structTypeName) };
                _dependencies = _dependencies ?? DependencyCollection.Create(this, SetDependencies, dependencyArray);
                return _dependencies;
            }
        }

        private static void SetDependencies(IQualifiedSource source, DependencyCollection dependencies)
        {
            ((StructFieldAccessor)source)._dependencies = dependencies;
        }

        async Task IDependencyTargetExportChanged.OnExportsChangedAsync(Envoy envoy, ExportsChangedData data)
        {
            NIType type = await envoy.GetTypeDiagramSignatureAsync();
            if (ShouldUpdateDataTypeFromChange(data))
            {
                UpdateStructType(type);
            }
        }

        private static bool ShouldUpdateDataTypeFromChange(ExportsChangedData data)
        {
            return data.IsForResolve
                || (data.IsForPropertyChange && data.HasChangedProperty(TypeDiagramCache.DataTypePropertyName));
        }
    }

    public class StructFieldAccessorTerminal : NodeTerminal
    {
        private const string ElementName = "StructFieldAccessorTerminal";

        public static readonly PropertySymbol FieldNamePropertySymbol = ExposeStaticProperty<StructFieldAccessorTerminal>(
            nameof(FieldName),
            terminal => terminal._fieldName,
            (terminal, value) => terminal._fieldName = (string)value,
            PropertySerializers.StringSerializer,
            null);

        private string _fieldName;

        private StructFieldAccessorTerminal()
        {
        }

        public StructFieldAccessorTerminal(Direction direction, NIType type, string name, SMPoint hotPoint)
            : base(direction, type, name, hotPoint)
        {
        }

        [XmlParserFactoryMethod(ElementName, Function.ParsableNamespaceName)]
        public static StructFieldAccessorTerminal CreateStructFieldAccessorTerminal(IElementCreateInfo elementCreateInfo)
        {
            var structFieldAccessorTerminal = new StructFieldAccessorTerminal();
            structFieldAccessorTerminal.Init(elementCreateInfo);
            return structFieldAccessorTerminal;
        }

        /// <inheritdoc />
        public override XName XmlElementName => XName.Get(ElementName, Function.ParsableNamespaceName);

        /// <inheritdoc />
        public override IEnumerable<KeyValuePair<PropertySymbol, object>> PropertiesForGenerate(ElementGenerationOptions options)
        {
            // Do not persist the DataType property for these terminals
            return base.PropertiesForGenerate(options).Where(pair => pair.Key != DataTypeProperty);
        }

        public string FieldName
        {
            get { return _fieldName; }
            set
            {
                if (_fieldName != value)
                {
                    TransactionRecruiter.EnlistPropertyItem(this, nameof(FieldName), _fieldName, value, (f, _) => { _fieldName = f; }, TransactionHints.Semantic);
                    _fieldName = value;
                }
            }
        }
    }
}
