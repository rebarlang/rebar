using System.Diagnostics;
using NationalInstruments.Dfir;

namespace RustyWires.Compiler
{
    [DebuggerDisplay("{Category} {Id} : {BaseLifetime}")]
    internal class Lifetime
    {
        public Lifetime()
        {
            BaseLifetime = null;
            Category = LifetimeCategory.Empty;
            Origin = null;
        }

        public Lifetime(LifetimeCategory category, Node origin, Lifetime baseLifetime)
        {
            BaseLifetime = baseLifetime;
            Category = category;
            Origin = origin;
        }

        public int Id => Origin?.UniqueId ?? 0;

        public Node Origin { get; }

        public LifetimeCategory Category { get; }

        public Lifetime BaseLifetime { get; }

        public bool IsEmpty => Category == LifetimeCategory.Empty;
    }
}
