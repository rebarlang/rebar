using NationalInstruments.Dfir;
using Rebar.Common;

namespace Rebar.Compiler
{
    internal static class LifetimeExtensions
    {
        public static Lifetime GetSourceLifetime(this Terminal terminal)
        {
            if (!terminal.IsConnected)
            {
                return null;
            }

            var connectedTerminal = terminal.ConnectedTerminal;
            var connectedTerminalVariable = connectedTerminal.GetFacadeVariable();
            if (connectedTerminalVariable.Lifetime == null && connectedTerminal.ParentNode is Wire)
            {
                Wire wire = (Wire)connectedTerminal.ParentNode;
                Terminal sourceTerminal;
                if (wire.TryGetSourceTerminal(out sourceTerminal))
                {
                    Lifetime sourceLifetime = sourceTerminal.GetSourceLifetime();
                    VariableReference sourceVariable = sourceTerminal.GetFacadeVariable();
                    sourceVariable.SetTypeAndLifetime(sourceVariable.Type, sourceLifetime);
                    foreach (var sinkTerminal in wire.SinkTerminals)
                    {
                        VariableReference sinkVariable = sinkTerminal.GetFacadeVariable();
                        sinkVariable.SetTypeAndLifetime(sinkVariable.Type, sourceLifetime);
                    }
                }
            }
            return connectedTerminalVariable.Lifetime;
        }

        public static Lifetime ComputeInputTerminalEffectiveLifetime(this Terminal inputTerminal)
        {
            VariableReference inputVariable = inputTerminal.GetFacadeVariable();
            // TODO: this should take a parameter for the type permission level above which to consider the input to be re-borrowed;
            // for now, assume that this level is ImmutableReference.
            if (inputVariable.Type.IsImmutableReferenceType())
            {
                return inputVariable.Lifetime;
            }
            else
            {
                return Lifetime.Empty;
            }
        }
    }
}
