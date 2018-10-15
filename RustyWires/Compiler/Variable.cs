using NationalInstruments.DataTypes;
using NationalInstruments.Dfir;
using System.Collections.Generic;

namespace RustyWires.Compiler
{
    internal sealed class Variable
    {
        public int Id { get; }

        public Terminal OriginatingTerminal { get; }

        public List<Wire> Wires { get; }

        public NIType Type { get; set; }

        public Lifetime Lifetime { get; set; }

        public Variable(int id, Terminal originatingTerminal)
        {
            Id = id;
            OriginatingTerminal = originatingTerminal;
            Wires = new List<Wire>();
        }

        /// <summary>
        /// Convenience method for setting the <see cref="Type"/> property that allows using the ?. operator.
        /// </summary>
        /// <param name="type">The <see cref="NIType"/> to set on this <see cref="Variable"/>.</param>
        public void SetType(NIType type)
        {
            Type = type;
        }

        /// <summary>
        /// Convenience method for setting the <see cref="Lifetime"/> property that allows using the ?. operator.
        /// </summary>
        /// <param name="lifetime">The <see cref="Lifetime"/> to set on this <see cref="Variable"/>.</param>
        public void SetLifetime(Lifetime lifetime)
        {
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
