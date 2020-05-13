using System.Linq;
using System.Threading.Tasks;
using System.Xml.Linq;
using NationalInstruments;
using NationalInstruments.Core;
using NationalInstruments.DataTypes;
using NationalInstruments.DynamicProperties;
using NationalInstruments.Linking;
using NationalInstruments.SourceModel;
using NationalInstruments.SourceModel.Envoys;
using NationalInstruments.SourceModel.Persistence;
using Rebar.SourceModel.TypeDiagram;

namespace Rebar.SourceModel
{
    public class VariantMatchStructure : MatchStructureBase, IDataTypeReferenceOwner, IQualifiedSource, IDependencyTargetExportChanged
    {
        public static readonly PropertySymbol TypeNamePropertySymbol = ExposeStaticProperty<VariantMatchStructure>(
            nameof(TypeName),
            owner => owner.TypeName,
            (owner, value) => owner.TypeName = (QualifiedName)value,
            PropertySerializers.QualifiedNameSerializer,
            QualifiedName.Empty);

        public static readonly PropertySymbol TypePropertySymbol = ExposeStaticProperty<VariantMatchStructure>(
            nameof(Type),
            owner => owner.Type,
            (owner, value) => owner.Type = (NIType)value,
            PropertySerializers.DataTypeSerializer,
            NIType.Unset);

        private const string ElementName = "VariantMatchStructure";

        private DependencyCollection _dependencies;

        [XmlParserFactoryMethod(ElementName, Function.ParsableNamespaceName)]
        public static VariantMatchStructure CreateVariantMatchStructure(IElementCreateInfo elementCreateInfo)
        {
            var variantMatchStructure = new VariantMatchStructure();
            variantMatchStructure.Initialize(elementCreateInfo);
            return variantMatchStructure;
        }

        private VariantMatchStructure()
        {
            _dependencies = DependencyCollection.Create(this, SetDependencies);
            TypeName = QualifiedName.Empty;
        }

        public override XName XmlElementName => XName.Get(ElementName, Function.ParsableNamespaceName);

        public QualifiedName TypeName { get; internal set; }

        public NIType Type { get; private set; }

        /// <inheritdoc />
        protected override RectangleSides GetSidesForBorderNode(BorderNode borderNode) => borderNode is VariantMatchStructureSelector
            ? RectangleSides.Left
            : RectangleSides.All;

        /// <inheritdoc />
        public override BorderNode MakeDefaultBorderNode(Diagram startDiagram, Diagram endDiagram, Wire wire, StructureIntersection intersection)
        {
            return MakeDefaultTunnelCore<VariantMatchStructureTunnel>(startDiagram, endDiagram, wire);
        }

        #region IQualifiedSource implementation

        /// <inheritdoc />
        public DependencyCollection Dependencies
        {
            get
            {
                return _dependencies;
            }
        }

        private static void SetDependencies(IQualifiedSource source, DependencyCollection dependencies)
        {
            ((VariantMatchStructure)source)._dependencies = dependencies;
        }

        #endregion

        #region IDependencyTargetExportChanged

        public async Task OnExportsChangedAsync(Envoy envoy, ExportsChangedData data)
        {
            if (ShouldUpdateDataTypeFromChange(data))
            {
                NIType oldType = Type;
                NIType type = await envoy.GetTypeDiagramSignatureAsync();
                if (type.IsUnset() || type == oldType)
                {
                    return;
                }
                this.TransactUpdateFromDependency(envoy, data, TransactionManager, "Set variant type", () =>
                {
                    Type = type;
                    TransactionRecruiter.EnlistPropertyItem(this, nameof(Type), oldType, type, (t, __) => { Type = t; }, TransactionHints.Semantic);
                    UpdateCasesFromDataType(type);
                });
            }
        }

        private static bool ShouldUpdateDataTypeFromChange(ExportsChangedData data)
        {
            return data.IsForResolve
                || (data.IsForPropertyChange && data.HasChangedProperty(TypeDiagramCache.DataTypePropertyName));
        }

        #endregion

        #region IDataTypeReferenceOwner

        void IDataTypeReferenceOwner.SetOwnedDataType(NIType dataType, PropertySymbol symbol)
        {
            Type = dataType;
            UpdateCasesFromDataType(dataType);
        }

        NIType IDataTypeReferenceOwner.GetOwnedDataType(PropertySymbol symbol) => Type;

        #endregion

        internal void UpdateDependencies(NIType type)
        {
            QualifiedName oldTypeName = TypeName;
            QualifiedName typeName = type.GetTypeDefinitionQualifiedName();
            if (oldTypeName != typeName)
            {
                TypeName = typeName;
                TransactionRecruiter.EnlistPropertyItem(this, nameof(TypeName), oldTypeName, typeName, (n, _) => TypeName = n, TransactionHints.Semantic);

                if (!oldTypeName.IsEmpty)
                {
                    _dependencies.RemoveOfType<TypeDiagramDependency>(this);
                }
                _dependencies.Add(this, new TypeDiagramDependency(this, typeName));
            }
        }

#if FALSE
        internal void UpdateVariantType(NIType type)
        {
            NIType oldType = Type;
            if (oldType != type)
            {
                Type = type;
                TransactionRecruiter.EnlistPropertyItem(this, nameof(Type), oldType, type, (t, _) => Type = t, TransactionHints.Semantic);

                QualifiedName oldTypeName = TypeName;
                QualifiedName typeName = type.GetTypeDefinitionQualifiedName();
                if (oldTypeName != typeName)
                {
                    TypeName = typeName;
                    TransactionRecruiter.EnlistPropertyItem(this, nameof(TypeName), oldTypeName, typeName, (n, _) => TypeName = n, TransactionHints.Semantic);

                    // need to notify that dependencies have changed so that we can resolve to them
                    if (!oldTypeName.IsEmpty)
                    {
                        _dependencies.RemoveOfType<TypeDiagramDependency>(this);
                    }
                    _dependencies.Add(this, new TypeDiagramDependency(this, typeName));
                }
            }
        }
#endif

        /// <inheritdoc />
        public override NestedDiagram CreateNestedDiagram() => new VariantMatchStructureDiagram();

        private void UpdateCasesFromDataType(NIType type)
        {
            if (!type.IsUnion())
            {
                return;
            }

            NIType[] unionFields = type.GetFields().ToArray();
            int unionFieldCount = unionFields.Length;
            if (!NestedDiagrams.HasExactly(unionFieldCount))
            {
                while (NestedDiagrams.Count() > unionFieldCount)
                {
                    RemoveNestedDiagram((VariantMatchStructureDiagram)NestedDiagrams.Last());
                }
                while (NestedDiagrams.Count() < unionFieldCount)
                {
                    AddNewCase();
                }
            }
        }

        private void AddNewCase()
        {
            // Copied from CaseStructure
            NestedDiagram newDiagram = CreateNestedDiagram();

            var existingDiagram = NestedDiagrams.FirstOrDefault();
            if (existingDiagram != null)
            {
                newDiagram.Bounds = existingDiagram.Bounds;
            }
            else
            {
                // EnsureView will resize all nested diagrams to whatever the current size of the structure is.
                EnsureView(EnsureViewHints.Bounds);
            }

            AddComponent(newDiagram);

            foreach (BorderNode borderNode in BorderNodes)
            {
                if (borderNode.GetPrimaryTerminal(newDiagram) == null)
                {
                    var newTerminal = borderNode.MakePrimaryInnerTerminal(newDiagram);
                    var oldTerminal = borderNode.InnerTerminals.FirstOrDefault(t => t != newTerminal);
                    if (oldTerminal != null)
                    {
                        newTerminal.Hotspot = oldTerminal.Hotspot;
                    }
                }
            }
        }
    }
}
