using System.Linq;
using System.Xml.Linq;
using NationalInstruments.Core;
using NationalInstruments.DataTypes;
using NationalInstruments.DynamicProperties;
using NationalInstruments.SourceModel;
using NationalInstruments.SourceModel.Persistence;

namespace Rebar.SourceModel.TypeDiagram
{
    public class PrimitiveType : SimpleNode, IDataTypeReferenceOwner
    {
        public static readonly PropertySymbol TypePropertySymbol =
            ExposeStaticProperty<PrimitiveType>(
                "Type",
                owner => owner.Type,
                (owner, type) => owner.Type = (NIType)type,
                PropertySerializers.DataTypeSerializer,
                NIType.Unset);

        private const string ElementName = "PrimitiveType";

        private PrimitiveType()
        {
            FixedTerminals.Add(new NodeTerminal(Direction.Output, PFTypes.Void, "type"));
        }

        [XmlParserFactoryMethod(ElementName, Function.ParsableNamespaceName)]
        public static PrimitiveType CreatePrimitiveType(IElementCreateInfo elementCreateInfo)
        {
            var primitiveType = new PrimitiveType();
            primitiveType.Init(elementCreateInfo);
            primitiveType.SetIconViewGeometry();
            return primitiveType;
        }

        /// <inheritdoc />
        public override XName XmlElementName => XName.Get(ElementName, Function.ParsableNamespaceName);

        /// <inheritdoc />
        protected override void SetIconViewGeometry()
        {
            Bounds = new SMRect(Left, Top, StockDiagramGeometries.GridSize * 4, StockDiagramGeometries.GridSize * 4);
            var terminals = FixedTerminals.OfType<NodeTerminal>().ToArray();
            terminals[0].Hotspot = new SMPoint(StockDiagramGeometries.GridSize * 4, StockDiagramGeometries.GridSize * 1);
        }

        public NIType Type { get; set; }

        #region Documentation

        /// <inheritdoc />
        protected override IDocumentation CreateDocumentation()
        {
            return new PrimitiveTypeDocumentation(Type);
        }

        private class PrimitiveTypeDocumentation : IDocumentation
        {
            private readonly NIType _type;

            public PrimitiveTypeDocumentation(NIType type)
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

        #region IDataTypeReferenceOwner implementation

        void IDataTypeReferenceOwner.SetOwnedDataType(NIType dataType, PropertySymbol symbol)
        {
            Type = dataType;
        }

        NIType IDataTypeReferenceOwner.GetOwnedDataType(PropertySymbol symbol)
        {
            return Type;
        }

        #endregion
    }
}
