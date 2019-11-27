using System;
using System.Linq;
using System.Xml.Linq;
using NationalInstruments.Core;
using NationalInstruments.DataTypes;
using NationalInstruments.DynamicProperties;
using NationalInstruments.SourceModel;
using NationalInstruments.SourceModel.Persistence;

namespace Rebar.SourceModel
{
    public class TypePassthrough : SimpleNode, IDataTypeReferenceOwner
    {
        private const string ElementName = "TypePassthrough";

        public static readonly PropertySymbol TypePropertySymbol =
            ExposeStaticProperty<TypePassthrough>(
                "Type",
                owner => owner.Type,
                (owner, type) => owner.Type = (NIType)type,
                PropertySerializers.DataTypeSerializer,
                NIType.Unset);

        protected TypePassthrough()
        {
            FixedTerminals.Add(new NodeTerminal(Direction.Input, PFTypes.Void, "ref in"));
            FixedTerminals.Add(new NodeTerminal(Direction.Output, PFTypes.Void, "ref out"));
        }

        [XmlParserFactoryMethod(ElementName, Function.ParsableNamespaceName)]
        public static TypePassthrough CreateTypePassthrough(IElementCreateInfo elementCreateInfo)
        {
            var typePassthrough = new TypePassthrough();
            typePassthrough.Init(elementCreateInfo);
            typePassthrough.SetIconViewGeometry();
            return typePassthrough;
        }

        public override XName XmlElementName => XName.Get(ElementName, Function.ParsableNamespaceName);

        internal NIType Type { get; set; }

        protected override void SetIconViewGeometry()
        {
            Bounds = new SMRect(Left, Top, StockDiagramGeometries.GridSize * 4, StockDiagramGeometries.GridSize * 4);
            var terminals = FixedTerminals.OfType<NodeTerminal>().ToArray();
            terminals[0].Hotspot = new SMPoint(0, StockDiagramGeometries.GridSize * 1);
            terminals[1].Hotspot = new SMPoint(StockDiagramGeometries.GridSize * 4, StockDiagramGeometries.GridSize * 1);
        }

        void IDataTypeReferenceOwner.SetOwnedDataType(NIType dataType, PropertySymbol symbol)
        {
            Type = dataType;
        }

        NIType IDataTypeReferenceOwner.GetOwnedDataType(PropertySymbol symbol)
        {
            return Type;
        }
    }
}
