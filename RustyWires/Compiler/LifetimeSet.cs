using NationalInstruments.Dfir;
using System.Collections.Generic;
using System.Linq;

namespace RustyWires.Compiler
{
    internal class LifetimeSet
    {
        private readonly List<Lifetime> _lifetimes = new List<Lifetime>();

        public LifetimeSet()
        {
            EmptyLifetime = new Lifetime();
            _lifetimes.Add(EmptyLifetime);
            StaticLifetime = new Lifetime(LifetimeCategory.FunctionStatic, null, null);
        }

        public Lifetime StaticLifetime { get; }

        public Lifetime EmptyLifetime { get; }

        public Lifetime DefineLifetime(LifetimeCategory category, Node origin, Lifetime baseLifetime)
        {
            Lifetime existing = _lifetimes.FirstOrDefault(l => l.Category == category && l.Origin == origin);
            if (existing != null)
            {
                // TODO: check that base lifetimes match
                return existing;
            }
            existing = new Lifetime(category, origin, baseLifetime);
            _lifetimes.Add(existing);
            return existing;
        }

        public Lifetime ComputeCommonLifetime(Lifetime left, Lifetime right)
        {
            if (left.Category == LifetimeCategory.FunctionStatic)
            {
                return right;
            }
            if (right.Category == LifetimeCategory.FunctionStatic)
            {
                return left;
            }

            if (left.Category == LifetimeCategory.Structure && right.Category == LifetimeCategory.Structure)
            {
                if (left.Origin == right.Origin)
                {
                    return left;
                }
                // TODO: if one lifetime is a descendant of the other, return the descendant lifetime
            }
            return EmptyLifetime;
        }
    }
}
