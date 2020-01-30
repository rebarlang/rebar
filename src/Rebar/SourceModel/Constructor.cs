using System;
using System.Collections.Generic;
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
using Rebar.Compiler;
using Rebar.SourceModel.TypeDiagram;

namespace Rebar.SourceModel
{
    public class Constructor : Node, IDataTypeReferenceOwner, IQualifiedSource, IDependencyTargetExportChanged
    {
        private const string ElementName = "Constructor";

        private DependencyCollection _dependencies;

        private OwnerComponentCollection FixedTerminals { get; }

        public static readonly PropertySymbol TypeNamePropertySymbol = ExposeStaticProperty<Constructor>(
            nameof(TypeName),
            owner => owner.TypeName,
            (owner, value) => owner.TypeName = (QualifiedName)value,
            PropertySerializers.QualifiedNameSerializer,
            QualifiedName.Empty);

        public static readonly PropertySymbol TypePropertySymbol = ExposeStaticProperty<Constructor>(
            nameof(Type),
            owner => owner.Type,
            (owner, value) => owner.Type = (NIType)value,
            PropertySerializers.DataTypeSerializer,
            NIType.Unset);

        private Constructor()
        {
            OutputTerminal = new NodeTerminal(Direction.Output, PFTypes.Void, "value");
            FixedTerminals = new OwnerComponentCollection(this);
            FixedTerminals.Add(OutputTerminal);
            TypeName = QualifiedName.Empty;
            Type = PFTypes.Void;
        }

        [XmlParserFactoryMethod(ElementName, Function.ParsableNamespaceName)]
        public static Constructor CreateConstructor(IElementCreateInfo elementCreateInfo)
        {
            var constructor = new Constructor();
            constructor.Init(elementCreateInfo);
            return constructor;
        }

        /// <inheritdoc />
        public override XName XmlElementName => XName.Get(ElementName, Function.ParsableNamespaceName);

        public QualifiedName TypeName { get; internal set; }

        public NIType Type { get; private set; }

        public NodeTerminal OutputTerminal { get; }

        public IEnumerable<ConstructorTerminal> ConstructorTerminals => Terminals.OfType<ConstructorTerminal>();

        /// <inheritdoc />
        public override IEnumerable<Element> Components => FixedTerminals;

        /// <inheritdoc />
        public override IEnumerable<Element> ComponentsForGenerate(ElementGenerationOptions options)
        {
            return base.ComponentsForGenerate(options).Concat(ConstructorTerminals);
        }

        /// <inheritdoc />
        protected override ViewElementTemplate DefaultTemplate => ViewElementTemplate.List;

        /// <inheritdoc />
        public override void EnsureView(EnsureViewHints hints)
        {
            if (hints.HasTemplateHint())
            {
                SetGeometry();
            }
            else if (hints.HasBoundsHint())
            {
                ArrangeListViewOutputs();
            }
        }

        private void SetGeometry()
        {
            if (Template == ViewElementTemplate.Icon)
            {
                SetIconViewGeometry();
            }
            else
            {
                SetListViewGeometry();
            }
        }

        private void SetIconViewGeometry()
        {
            int inputTerminalCount = FixedTerminals.Count - 1,
                height = StockDiagramGeometries.GridSize * 2 * Math.Max(2, inputTerminalCount);
            Bounds = new SMRect(Left, Top, StockDiagramGeometries.GridSize * 4, height);
            var terminals = FixedTerminals.OfType<NodeTerminal>().ToArray();
            terminals[0].Hotspot = new SMPoint(StockDiagramGeometries.GridSize * 4, StockDiagramGeometries.GridSize * 1);
            int index = 0;
            foreach (Terminal terminal in FixedTerminals.Skip(1))
            {
                terminal.Hotspot = new SMPoint(0, StockDiagramGeometries.GridSize * (2 * index + 1));
                ++index;
            }
        }

        private void SetListViewGeometry()
        {
            var terminals = FixedTerminals.OfType<NodeTerminal>().ToArray();
            terminals[0].Hotspot = TerminalHotspots.CreateOutputTerminalHotspot(TerminalSize.Small, Width, 0u);
            uint index = 0u;
            foreach (Terminal terminal in terminals.Skip(1))
            {
                terminal.Hotspot = TerminalHotspots.CreateListViewInputTerminalHotspot(ListViewHeaderHeight, index);
                ++index;
            }
            Height = ListViewHeaderHeight + index * StockDiagramGeometries.LargeTerminalHeight + ListViewFooterHeight;
        }

        public override void AcceptVisitor(IElementVisitor visitor)
        {
            var functionVisitor = visitor as IFunctionVisitor;
            if (functionVisitor != null)
            {
                functionVisitor.VisitConstructor(this);
            }
            else
            {
                base.AcceptVisitor(visitor);
            }
        }

        #region Documentation

        /// <inheritdoc />
        public override IDocumentation CreateDocumentationForTerminal(Terminal terminal)
        {
            if (terminal.Index > 0)
            {
                NIType fieldType = Type.GetFields().ElementAt(terminal.Index - 1);
                return new Documentation() { Name = fieldType.GetName() };
            }
            return base.CreateDocumentationForTerminal(terminal);
        }

        /// <inheritdoc />
        protected override IDocumentation CreateDocumentation()
        {
            return new TypeDefinitionDocumentation(Type);
        }

        private class TypeDefinitionDocumentation : IDocumentation
        {
            private readonly NIType _type;

            public TypeDefinitionDocumentation(NIType type)
            {
                _type = type;
            }

            /// <inheritdoc />
            public string Description => _type.GetName();

            /// <inheritdoc />
            public string InstanceName => Name;

            /// <inheritdoc />
            public string Name => _type.GetName();
        }

        #endregion

        #region IQualifiedSource implementation

        /// <inheritdoc />
        public DependencyCollection Dependencies
        {
            get
            {
                _dependencies = _dependencies ?? DependencyCollection.Create(
                    this,
                    SetDependencies,
                    new[] { new TypeDiagramDependency(this, TypeName) });
                return _dependencies;
            }
        }

        private static void SetDependencies(IQualifiedSource source, DependencyCollection dependencies)
        {
            ((Constructor)source)._dependencies = dependencies;
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
                using (var transaction = TransactionManager.BeginTransactionIfNecessary("Set type", TransactionPurpose.NonUser))
                {
                    Type = type;
                    TransactionRecruiter.EnlistPropertyItem(this, nameof(Type), oldType, type, (t, __) => { Type = t; }, TransactionHints.Semantic);
                    UpdateTerminalsFromDataType(type);
                    transaction?.Commit();
                }
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
            UpdateTerminalsFromDataType(dataType);
        }

        NIType IDataTypeReferenceOwner.GetOwnedDataType(PropertySymbol symbol) => Type;

        #endregion

        private void UpdateTerminalsFromDataType(NIType type)
        {
            if (type.IsValueClass())
            {
                NIType[] structFields = type.GetFields().ToArray();
                int newFieldCount = structFields.Length;
                if (!InputTerminals.HasExactly(newFieldCount))
                {
                    while (newFieldCount < InputTerminals.Count())
                    {
                        FixedTerminals.Remove(FixedTerminals.Last());
                    }
                    while (newFieldCount > InputTerminals.Count())
                    {
                        int index = InputTerminals.Count();
                        FixedTerminals.Add(new ConstructorTerminal(PFTypes.Void, $"element{index}"));
                    }
                    foreach (var pair in FixedTerminals.Skip(1).Cast<ConstructorTerminal>().Zip(structFields))
                    {
                        pair.Key.FieldName = pair.Value.GetName();
                    }
                }
                SetGeometry();
            }
        }
    }

    public class ConstructorTerminal : NodeTerminal
    {
        private const string ElementName = "ConstructorTerminal";

        public static readonly PropertySymbol FieldNamePropertySymbol = ExposeStaticProperty<ConstructorTerminal>(
            nameof(FieldName),
            terminal => terminal._fieldName,
            (terminal, value) => terminal._fieldName = (string)value,
            PropertySerializers.StringSerializer,
            null);

        private string _fieldName;

        private ConstructorTerminal()
        {
        }

        public ConstructorTerminal(NIType type, string name)
            : base(Direction.Input, type, name)
        {
        }

        [XmlParserFactoryMethod(ElementName, Function.ParsableNamespaceName)]
        public static ConstructorTerminal CreateConstructorTerminal(IElementCreateInfo elementCreateInfo)
        {
            var constructorTerminal = new ConstructorTerminal();
            constructorTerminal.Init(elementCreateInfo);
            return constructorTerminal;
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
