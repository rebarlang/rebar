using System.Collections.Generic;
using NationalInstruments.DataTypes;
using NationalInstruments.Dfir;
using Rebar.Compiler;

namespace Rebar.Common
{
    internal sealed class Variable
    {
        public int Id { get; }

        public Terminal OriginatingTerminal { get; }

        public List<Wire> Wires { get; }

        /// <summary>
        /// True if the <see cref="Variable"/> represents a mutable binding.
        /// </summary>
        /// <remarks>This property is independent of whether the <see cref="Variable"/>'s type
        /// is a mutable reference; it is possible to have a mutable ImmutableReference <see cref="Variable"/>
        /// (which can be rebound to a different ImmutableReference) and an immutable MutableReference
        /// <see cref="Variable"/> (where the referred-to storage can be modified, but the <see cref="Variable"/>
        /// cannot be rebound).</remarks>
        public bool Mutable { get; }

        /// <summary>
        /// The data <see cref="NIType"/> stored by the <see cref="Variable"/>.
        /// </summary>
        /// <remarks>This property should not store ImmutableValue or MutableValue types.
        /// ImmutableReference and MutableReference types are allowed.</remarks>
        public NIType Type { get; private set; }

        public Lifetime Lifetime { get; private set; }

        public Variable(int id, bool mutable, Terminal originatingTerminal)
        {
            Id = id;
            Mutable = mutable;
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
            string mut = Mutable ? "mut" : string.Empty;
            return $"v_{Id} : {mut} {Type}";
        }
    }

    internal static class VariableExtensions
    {
        public static NIType GetTypeOrVoid(this Variable variable)
        {
            return variable?.Type ?? PFTypes.Void;
        }
    }
}
