using System;
using System.Collections.Generic;
using System.Linq;

namespace RustyWires.Compiler
{
    internal class BoundedLifetimeGraph
    {
        private class BoundedLifetime : Lifetime
        {
            private readonly BoundedLifetimeGraph _graph;

            public BoundedLifetime(BoundedLifetimeGraph graph)
            {
                _graph = graph;
            }

            public override bool IsEmpty => false;

            public override bool DoesOutlastDiagram => _graph.DoesOutlast(this, _graph.DiagramLifetime);

            public override bool IsBounded => true;
        }

        private readonly Dictionary<BoundedLifetime, HashSet<BoundedLifetime>> _lifetimeSupertypes = new Dictionary<BoundedLifetime, HashSet<BoundedLifetime>>();

        public BoundedLifetimeGraph()
        {
            DiagramLifetime = new BoundedLifetime(this);
        }

        private BoundedLifetime DiagramLifetime { get; }

        public void SetOutlastsRelationship(Lifetime outlaster, Lifetime outlasted)
        {
            BoundedLifetime boundedOutlaster = outlaster as BoundedLifetime, boundedOutlasted = outlasted as BoundedLifetime;
            if (boundedOutlaster == null || boundedOutlasted == null)
            {
                return;
            }
            if (IsSubtypeLiftimeOf(boundedOutlasted, boundedOutlaster))
            {
                throw new ArgumentException("outlasted already outlasts outlaster");
            }
            if (IsSubtypeLiftimeOf(boundedOutlaster, boundedOutlaster))
            {
                return;
            }
            HashSet<BoundedLifetime> supertypes;
            if (!_lifetimeSupertypes.TryGetValue(boundedOutlaster, out supertypes))
            {
                supertypes = new HashSet<BoundedLifetime>();
                _lifetimeSupertypes[boundedOutlaster] = supertypes;
            }
            supertypes.Add(boundedOutlasted);
        }

        public Lifetime CreateLifetimeThatOutlastsDiagram()
        {
            BoundedLifetime lifetime = new BoundedLifetime(this);
            SetOutlastsRelationship(lifetime, DiagramLifetime);
            return lifetime;
        }

        public Lifetime CreateLifetimeThatIsBoundedByDiagram()
        {
            BoundedLifetime lifetime = new BoundedLifetime(this);
            SetOutlastsRelationship(DiagramLifetime, lifetime);
            return lifetime;
        }

        public bool DoesOutlast(Lifetime toCheck, Lifetime comparison)
        {
            var boundedToCheck = toCheck as BoundedLifetime;
            var boundedComparison = comparison as BoundedLifetime;
            if (boundedToCheck == null && boundedComparison != null)
            {
                return DoesUnboundedOutlastBounded(toCheck);
            }
            if (boundedToCheck != null && boundedComparison == null)
            {
                return DoesUnboundedOutlastBounded(comparison);
            }
            if (boundedToCheck != null && boundedToCheck != null)
            {
                return IsSubtypeLiftimeOf(boundedToCheck, boundedComparison);
            }
            return false;
        }

        private bool DoesUnboundedOutlastBounded(Lifetime unbounded)
        {
            return unbounded == Lifetime.Static || unbounded == Lifetime.Unbounded;
        }

        private bool IsSubtypeLiftimeOf(BoundedLifetime toCheck, BoundedLifetime comparison)
        {
            HashSet<BoundedLifetime> supertypes;
            if (!_lifetimeSupertypes.TryGetValue(toCheck, out supertypes))
            {
                return false;
            }
            if (supertypes.Contains(comparison))
            {
                return true;
            }
            return supertypes.Any(supertype => IsSubtypeLiftimeOf(supertype, comparison));
        }
    }
}
