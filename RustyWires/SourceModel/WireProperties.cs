using System.Xml.Linq;
using NationalInstruments.DynamicProperties;
using NationalInstruments.SourceModel;
using NationalInstruments.SourceModel.Persistence;
using RustyWires.Common;

namespace RustyWires.SourceModel
{
    internal static class WireProperties
    {
        internal static readonly PropertySymbol FirstVariableWirePropertySymbol =
            Element.ExposeAttachedProperty(
                XName.Get("FirstVariableWire", RustyWiresFunction.ParsableNamespaceName),
                PropertySerializers.BooleanSerializer,
                false,
                typeof(bool),
                PropertySymbolAttributes.DoNotPersist);

        public static bool GetIsFirstVariableWire(this Wire wire)
        {
            object value;
            return wire.TryGetValue(FirstVariableWirePropertySymbol, out value) && (bool)value;
        }

        public static void SetIsFirstVariableWire(this Wire wire, bool value)
        {
            wire.TrySetValue(FirstVariableWirePropertySymbol, value);
        }

        internal static readonly PropertySymbol WireBeginsMutableVariablePropertySymbol =
            Element.ExposeAttachedProperty(
                XName.Get("WireBeginsMutableVariable", RustyWiresFunction.ParsableNamespaceName),
                PropertySerializers.BooleanSerializer,
                false,
                typeof(bool));

        public static bool GetWireBeginsMutableVariable(this Wire wire)
        {
            object mutableTerminalBindingsSetting;
            return wire.TryGetValue(WireBeginsMutableVariablePropertySymbol, out mutableTerminalBindingsSetting)
                && (bool)mutableTerminalBindingsSetting;
        }

        public static void SetWireBeginsMutableVariable(this Wire wire, bool value)
        {
            object oldValue;
            wire.TryGetValue(WireBeginsMutableVariablePropertySymbol, out oldValue);
            wire.TransactionRecruiter.EnlistPropertyItem(
                wire,
                "WireBeginsMutableVariable",
                oldValue,
                value,
                (mode, reason) => wire.TrySetValue(WireBeginsMutableVariablePropertySymbol, mode),
                TransactionHints.Semantic);
            wire.TrySetValue(WireBeginsMutableVariablePropertySymbol, value);
        }

        private static readonly PropertySymbol WireVariablePropertySymbol =
            Element.ExposeAttachedProperty(
                XName.Get("WireVariable", RustyWiresFunction.ParsableNamespaceName),
                PropertySerializers.NullSerializer,
                null,
                typeof(Variable),
                PropertySymbolAttributes.DoNotPersist);

        public static Variable GetWireVariable(this Wire wire)
        {
            object value;
            return wire.TryGetValue(WireVariablePropertySymbol, out value)
                ? (Variable)value
                : null;
        }

        public static void SetWireVariable(this Wire wire, Variable variable)
        {
            wire.TrySetValue(WireVariablePropertySymbol, variable);
        }
    }
}
