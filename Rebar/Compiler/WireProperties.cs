using NationalInstruments.Dfir;

namespace Rebar.Compiler
{
    internal static class WireProperties
    {
        /// <summary>
        /// Token that stores a boolean property per <see cref="Wire"/> that denotes whether that wire is the first wire in
        /// dataflow order associated with the wire's <see cref="Variable"/> (i.e., whether the wire's connected node terminal is
        /// the terminal that created the <see cref="Variable"/>).
        /// </summary>
        private static readonly AttributeDescriptor _isFirstVariableWireTokenName = new AttributeDescriptor("Rebar.Compiler.IsFirstVariableWire", true);

        public static bool GetIsFirstVariableWire(this Wire wire)
        {
            var token = wire.DfirRoot.GetOrCreateNamedSparseAttributeToken<bool>(_isFirstVariableWireTokenName);
            return wire.GetAttribute(token);
        }

        public static void SetIsFirstVariableWire(this Wire wire, bool value)
        {
            var token = wire.DfirRoot.GetOrCreateNamedSparseAttributeToken<bool>(_isFirstVariableWireTokenName);
            wire.SetAttribute(token, value);
        }

        /// <summary>
        /// Token that stores a boolean property per <see cref="Wire"/> that denotes whether the <see cref="Variable"/>
        /// of which the wire is the first wire should be mutable. Ignored for wires that are not the first associated with their
        /// <see cref="Variable"/>.
        /// </summary>
        private static readonly AttributeDescriptor _wireBeginsMutableVariableTokenName = new AttributeDescriptor("Rebar.Compiler.WireBeginsMutableVariable", true);

        public static bool GetWireBeginsMutableVariable(this Wire wire)
        {
            var token = wire.DfirRoot.GetOrCreateNamedSparseAttributeToken<bool>(_wireBeginsMutableVariableTokenName);
            return wire.GetAttribute(token);
        }

        public static void SetWireBeginsMutableVariable(this Wire wire, bool value)
        {
            var token = wire.DfirRoot.GetOrCreateNamedSparseAttributeToken<bool>(_wireBeginsMutableVariableTokenName);
            wire.SetAttribute(token, value);
        }
    }
}
