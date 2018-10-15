using NationalInstruments.Dfir;

namespace RustyWires.Compiler
{
    internal static class RustyWiresLifetimes
    {
        private static readonly AttributeDescriptor _lifetimeTokenName = new AttributeDescriptor("RustyWires.Compiler.Lifetime", false);
        private static readonly AttributeDescriptor _lifetimeSetTokenName = new AttributeDescriptor("RustyWires.Compiler.LifetimeSet", false);

        public static void SetLifetime(this Terminal terminal, Lifetime lifetime)
        {
            var token = terminal.DfirRoot.GetOrCreateNamedSparseAttributeToken<Lifetime>(_lifetimeTokenName);
            token.SetAttribute(terminal, lifetime);
        }

        public static Lifetime GetLifetime(this Terminal terminal)
        {
            var token = terminal.DfirRoot.GetOrCreateNamedSparseAttributeToken<Lifetime>(_lifetimeTokenName);
            return token.GetAttribute(terminal);
        }

        public static Lifetime GetSourceLifetime(this Terminal terminal)
        {
            if (!terminal.IsConnected)
            {
                return null;
            }

            var token = terminal.DfirRoot.GetOrCreateNamedSparseAttributeToken<Lifetime>(_lifetimeTokenName);
            var connectedTerminal = terminal.ConnectedTerminal;
            if (!token.HasAttributeStorage(connectedTerminal) && connectedTerminal.ParentNode is Wire)
            {
                Wire wire = (Wire)connectedTerminal.ParentNode;
                Lifetime sourceLifetime = wire.SourceTerminal.GetSourceLifetime();
                wire.SourceTerminal.SetLifetime(sourceLifetime);
                foreach (var sinkTerminal in wire.SinkTerminals)
                {
                    sinkTerminal.SetLifetime(sourceLifetime);
                }
            }
            return connectedTerminal.GetLifetime();
        }

        public static Lifetime ComputeInputTerminalEffectiveLifetime(this Terminal inputTerminal)
        {
            // TODO: this should take a parameter for the type permission level above which to consider the input to be re-borrowed;
            // for now, assume that this level is ImmutableReference.
            if (inputTerminal.DataType.IsImmutableReferenceType())
            {
                return inputTerminal.GetSourceLifetime();
            }
            else
            {
                return inputTerminal.DfirRoot.GetLifetimeSet().EmptyLifetime;
            }
        }

        public static LifetimeSet GetLifetimeSet(this DfirRoot dfirRoot)
        {
            var token = dfirRoot.GetOrCreateNamedSparseAttributeToken<LifetimeSet>(_lifetimeSetTokenName);
            return token.GetAttribute(dfirRoot);
        }
    }
}
