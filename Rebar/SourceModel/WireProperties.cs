using System.Xml.Linq;
using NationalInstruments.DynamicProperties;
using NationalInstruments.SourceModel;
using NationalInstruments.SourceModel.Persistence;
using Rebar.Common;

namespace Rebar.SourceModel
{
    [ExposeAttachedProperties(typeof(WireProperties), Function.ParsableNamespaceName)]
    internal class WireProperties
    {
        internal static readonly PropertySymbol FirstVariableWirePropertySymbol =
            Element.ExposeAttachedProperty(
                XName.Get("FirstVariableWire", Function.ParsableNamespaceName),
                PropertySerializers.BooleanSerializer,
                false,
                typeof(bool),
                PropertySymbolAttributes.DoNotPersist);

        internal static readonly PropertySymbol WireBeginsMutableVariablePropertySymbol =
            Element.ExposeAttachedProperty(
                XName.Get("WireBeginsMutableVariable", Function.ParsableNamespaceName),
                PropertySerializers.BooleanSerializer,
                false,
                typeof(bool));
    }

    internal static class WirePropertyExtensions
    {
        public static bool GetIsFirstVariableWire(this Wire wire)
        {
            object value;
            return wire.TryGetValue(WireProperties.FirstVariableWirePropertySymbol, out value) && (bool)value;
        }

        public static void SetIsFirstVariableWire(this Wire wire, bool value)
        {
            wire.TrySetValue(WireProperties.FirstVariableWirePropertySymbol, value);
        }

        public static bool GetWireBeginsMutableVariable(this Wire wire)
        {
            object mutableTerminalBindingsSetting;
            return wire.TryGetValue(WireProperties.WireBeginsMutableVariablePropertySymbol, out mutableTerminalBindingsSetting)
                && (bool)mutableTerminalBindingsSetting;
        }

        public static void SetWireBeginsMutableVariable(this Wire wire, bool value)
        {
            object oldValue;
            wire.TryGetValue(WireProperties.WireBeginsMutableVariablePropertySymbol, out oldValue);
            wire.TransactionRecruiter.EnlistPropertyItem(
                wire,
                "WireBeginsMutableVariable",
                oldValue,
                value,
                (mode, reason) => wire.TrySetValue(WireProperties.WireBeginsMutableVariablePropertySymbol, mode),
                TransactionHints.Semantic);
            wire.TrySetValue(WireProperties.WireBeginsMutableVariablePropertySymbol, value);
        }

        private static readonly PropertySymbol WireVariablePropertySymbol =
            Element.ExposeAttachedProperty(
                XName.Get("WireVariable", Function.ParsableNamespaceName),
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
