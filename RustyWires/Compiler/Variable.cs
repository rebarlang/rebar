using System.Collections.Generic;
using NationalInstruments.DataTypes;
using NationalInstruments.Dfir;

namespace RustyWires.Compiler
{
    internal sealed class Variable
    {
        public int Id { get; }

        public Terminal OriginatingTerminal { get; }

        public List<Wire> Wires { get; }

        public NIType Type { get; private set; }

        public Lifetime Lifetime { get; private set; }

        public Variable(int id, Terminal originatingTerminal)
        {
            Id = id;
            OriginatingTerminal = originatingTerminal;
            Wires = new List<Wire>();
        }

        /// <summary>
        /// Convenience method for setting the <see cref="Type"/> and <see cref="Lifetime"/> properties that allows using the ?. operator.
        /// </summary>
        /// <param name="type">The <see cref="NIType"/> to set on this <see cref="Variable"/>.</param>
        /// <param name="lifetime">The <see cref="Lifetime"/> to set on this <see cref="Variable"/>.</param>
        public void SetTypeAndLifetime(NIType type, Lifetime lifetime)
        {
            Type = type;
            Lifetime = lifetime;
        }

        public override string ToString()
        {
            return $"v_{Id} : {Type}";
        }
    }

    internal static class VariableExtensions
    {
        public static NIType GetUnderlyingTypeOrVoid(this Variable variable)
        {
            if (variable == null)
            {
                return PFTypes.Void;
            }
            return variable.Type.GetUnderlyingTypeFromRustyWiresType();
        }
    }
}
