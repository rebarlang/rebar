using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Xml.Linq;
using NationalInstruments.Core;
using NationalInstruments.DataTypes;
using NationalInstruments.Design;
using NationalInstruments.DynamicProperties;
using NationalInstruments.MocCommon.SourceModel;
using NationalInstruments.PanelCommon.SourceModel;
using NationalInstruments.SourceModel;
using NationalInstruments.SourceModel.Envoys;
using NationalInstruments.SourceModel.Persistence;
using NationalInstruments.VI.SourceModel;

namespace Rebar.SourceModel.TypeDiagram
{
    public class TypeDiagramDefinition : DataflowFunctionDefinition, ITypeDefinition, IDataTypeReferenceOwner
    {
        #region Dynamic Properties

        private const string ElementName = "TypeDiagramDefinition";

        /// <summary>
        /// Expose the UnderlyingType property
        /// </summary>
        public static readonly PropertySymbol UnderlyingTypePropertySymbol = ExposeStaticProperty<TypeDiagramDefinition>(
            DataTypePropertyName,
            owner => owner.UnderlyingType,
            (owner, value) => owner.UpdateUnderlyingType((NIType)value, true),
            PropertySerializers.DataTypeSerializer,
            PFTypes.Void);

        /// <summary>
        /// Name of the DataType property used in property changed events.
        /// </summary>
        public const string DataTypePropertyName = "DataType";

        public const string TypeDiagramMocIdentifier = "TypeDiagram.Moc";

        #endregion

        /// <summary>
        /// DefinitionType
        /// </summary>
        public const string TypeDiagramDefinitionType = "Rebar.SourceModel.TypeDiagram.TypeDiagramDefinition";

        /// <summary>
        ///  Get the root diagram of the type.
        /// </summary>
        public NationalInstruments.SourceModel.RootDiagram Diagram => Components.OfType<NationalInstruments.SourceModel.RootDiagram>().Single();

        private NIType _underlyingType = PFTypes.Void;

        private TypeDiagramDefinition()
            : base(new BlockDiagram(), false)
        {
        }

        [ExportDefinitionFactory(TypeDiagramDefinitionType)]
        [StaticBindingKeywords(TypeDiagramMocIdentifier)]
        // [StaticBindingKeywords("ProjectItemCopyPasteDefaultService")]
        [XmlParserFactoryMethod(ElementName, Function.ParsableNamespaceName)]
        public static TypeDiagramDefinition Create(IElementCreateInfo elementCreateInfo)
        {
            var typeDiagramDefinition = new TypeDiagramDefinition();
            typeDiagramDefinition.Host = elementCreateInfo.Host;
            typeDiagramDefinition.Init(elementCreateInfo);
            return typeDiagramDefinition;
        }

        protected override NationalInstruments.SourceModel.RootDiagram CreateNewRootDiagram()
        {
            BlockDiagram blockDiagram = BlockDiagram.Create(ElementCreateInfo.ForNew);
            SelfType selfTypeNode = SelfType.CreateSelfType(ElementCreateInfo.ForNew);
            blockDiagram.AddChild(selfTypeNode);
            selfTypeNode.Left = 300;
            selfTypeNode.Top = 200;
            return blockDiagram;
        }

        public override XName XmlElementName => XName.Get(ElementName, Function.ParsableNamespaceName);

        /// <inheritdoc />
        public override IWiringBehavior WiringBehavior => new VirtualInstrumentWiringBehavior();

        /// <inheritdoc />
        protected override void CreateBatchRules(ICollection<ModelBatchRule> rules)
        {
            rules.Add(new CoreBatchRule());
            rules.Add(new ContentBatchRule());
            rules.Add(new VerticalGrowNodeBoundsRule());
            rules.Add(new DockedConstantBatchRule());
            rules.Add(new WiringBatchRule());
            rules.Add(new WireCommentBatchRule());
        }

        public UIModel CreateControlModelForMergeScript(NIType typeDefinitionType, bool forChecksum)
        {
            throw new NotImplementedException();
        }

        // TODO
        public override IEnumerable<IDiagramParameter> Parameters => Enumerable.Empty<IDiagramParameter>();

        public bool RequiresRootControl => false;

        public NIType UnderlyingType
        {
            get { return _underlyingType; }
            set { UpdateUnderlyingType(value, true); }
        }

        private void UpdateUnderlyingType(NIType value, bool updateDependencies)
        {
            if (!_underlyingType.Equals(value))
            {
                NIType oldValue = _underlyingType;
                _underlyingType = value;

                // Don't set dependencies on load, they will be parsed in.
                if (updateDependencies)
                {
                    TypeDefinitionSupport.SetTypeDefinitionDependencies(value, this);
                }
                TransactionRecruiter.EnlistPropertyItem(this, DataTypePropertyName, oldValue, _underlyingType, (d, _) => _underlyingType = d, TransactionHints.Semantic);
            }
        }

        public NIType DataType => CreateType();

        private NIType CreateType()
        {
            QualifiedName targetRelativeName = Envoy != null ? Envoy.MakeRelativeDependencyName().BeginningSegment : QualifiedName.Empty;
            NIAttributedBaseBuilder builder;
            if (UnderlyingType.IsClass())
            {
                builder = UnderlyingType.DefineClassFromExisting();
            }
            else
            {
                string name = Name.Last;
                builder = UnderlyingType.DefineTypedef(name);
            }
            builder.DefineNamespaceName(targetRelativeName);
            return builder.CreateType();
        }

        // Hopefully this works, since for now these types won't be scopes
        public Envoy Scope => null;

        public bool IsValid => true;

        #region IDataTypeReferenceOwner implementation

        void IDataTypeReferenceOwner.SetOwnedDataType(NIType dataType, PropertySymbol symbol)
        {
            if (symbol == UnderlyingTypePropertySymbol)
            {
                UpdateUnderlyingType(dataType, false);
                return;
            }
            throw new NotImplementedException();
        }

        NIType IDataTypeReferenceOwner.GetOwnedDataType(PropertySymbol symbol)
        {
            if (symbol == UnderlyingTypePropertySymbol)
            {
                return _underlyingType;
            }
            throw new NotImplementedException();
        }

        #endregion
    }

    public static class TypeDiagramExtensions
    {
        public static async Task<NIType> GetTypeDiagramSignatureAsync(this Envoy typeDiagramEnvoy)
        {
            IProvideDataType typeDiagramCacheService = typeDiagramEnvoy.GetBasicCacheServices()
                .OfType<IProvideDataType>()
                .FirstOrDefault();
            if (typeDiagramCacheService != null)
            {
                return await typeDiagramCacheService.GetDataTypeAsync();
            }
            return NIType.Unset;
        }
    }
}
