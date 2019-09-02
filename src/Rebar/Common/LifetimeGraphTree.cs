using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace Rebar.Common
{
    internal sealed class LifetimeGraphTree
    {
        [DebuggerDisplay("{DebuggerDisplay}")]
        private class BoundedLifetime : Lifetime
        {
            private readonly LifetimeGraphTree _graphTree;

            public BoundedLifetime(LifetimeGraphTree graphTree, BoundedLifetimeGraph graph)
            {
                _graphTree = graphTree;
                DiagramGraph = graph;
            }

            public BoundedLifetimeGraph DiagramGraph { get; }

            public override bool IsEmpty => false;

            public override bool DoesOutlastLifetimeGraph(LifetimeGraphIdentifier lifetimeGraphIdentifier)
                => _graphTree.DoesLifetimeOutlastLifetimeGraph(this, lifetimeGraphIdentifier);

            public override bool IsBounded => true;

            // TODO
            private string DebuggerDisplay => "BoundedLifetime";
        }

        private class BoundedLifetimeGraph
        {
            private readonly LifetimeGraphTree _graphTree;
            private readonly Dictionary<BoundedLifetime, HashSet<BoundedLifetime>> _lifetimeSupertypes = new Dictionary<BoundedLifetime, HashSet<BoundedLifetime>>();

            public BoundedLifetimeGraph(LifetimeGraphTree graphTree)
            {
                _graphTree = graphTree;
                DiagramLifetime = new BoundedLifetime(_graphTree, this);
            }

            public BoundedLifetime DiagramLifetime { get; }

            public void SetOutlastsRelationship(Lifetime outlaster, Lifetime outlasted)
            {
                BoundedLifetime boundedOutlaster = outlaster as BoundedLifetime, boundedOutlasted = outlasted as BoundedLifetime;
                if (boundedOutlaster == null || boundedOutlasted == null)
                {
                    return;
                }
                if (IsSubtypeLifetimeOf(boundedOutlasted, boundedOutlaster))
                {
                    throw new ArgumentException("outlasted already outlasts outlaster");
                }
                if (IsSubtypeLifetimeOf(boundedOutlaster, boundedOutlaster))
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

            public Lifetime CreateLifetimeThatIsBoundedByDiagram()
            {
                BoundedLifetime lifetime = new BoundedLifetime(_graphTree, this);
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
                    return IsSubtypeLifetimeOf(boundedToCheck, boundedComparison);
                }
                return false;
            }

            private bool DoesUnboundedOutlastBounded(Lifetime unbounded)
            {
                return unbounded == Lifetime.Static || unbounded == Lifetime.Unbounded;
            }

            private bool IsSubtypeLifetimeOf(BoundedLifetime toCheck, BoundedLifetime comparison)
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
                return supertypes.Any(supertype => IsSubtypeLifetimeOf(supertype, comparison));
            }
        }

        private readonly Dictionary<LifetimeGraphIdentifier, BoundedLifetimeGraph> _diagramGraphs = new Dictionary<LifetimeGraphIdentifier, BoundedLifetimeGraph>();
        private readonly Dictionary<LifetimeGraphIdentifier, LifetimeGraphIdentifier> _graphParents = new Dictionary<LifetimeGraphIdentifier, LifetimeGraphIdentifier>();

        public void EstablishLifetimeGraph(LifetimeGraphIdentifier identifier, LifetimeGraphIdentifier parentIdentifier)
        {
            _diagramGraphs[identifier] = new BoundedLifetimeGraph(this);
            _graphParents[identifier] = parentIdentifier;
        }

        public Lifetime GetLifetimeGraphRootLifetime(LifetimeGraphIdentifier graphIdentifier)
        {
            return _diagramGraphs[graphIdentifier].DiagramLifetime;
        }

        public Lifetime CreateLifetimeThatIsBoundedByLifetimeGraph(LifetimeGraphIdentifier graphIdentifier)
        {
            return _diagramGraphs[graphIdentifier].CreateLifetimeThatIsBoundedByDiagram();
        }

        // TODO: to be used by function parameters
        public Lifetime CreateLifetimeThatOutlastsRootLifetimeGraph()
        {
            throw new NotImplementedException();
        }

        private bool DoesLifetimeOutlastLifetimeGraph(BoundedLifetime boundedLifetime, LifetimeGraphIdentifier graphIdentifier)
        {
            BoundedLifetimeGraph boundedLifetimeGraph = boundedLifetime.DiagramGraph;
            BoundedLifetimeGraph diagramGraph = _diagramGraphs[graphIdentifier];
            if (boundedLifetimeGraph == diagramGraph)
            {
                return boundedLifetimeGraph.DoesOutlast(boundedLifetime, boundedLifetimeGraph.DiagramLifetime);
            }
            else
            {
                LifetimeGraphIdentifier currentGraphIdentifier = graphIdentifier, parentGraphIdentifier;
                while (_graphParents.TryGetValue(graphIdentifier, out parentGraphIdentifier))
                {
                    diagramGraph = _diagramGraphs[currentGraphIdentifier];
                    if (diagramGraph == boundedLifetimeGraph)
                    {
                        return true;
                    }
                    currentGraphIdentifier = parentGraphIdentifier;
                }
                return false;
            }
        }

        public LifetimeGraphIdentifier GetBoundedLifetimeGraphIdentifier(Lifetime lifetime)
        {
            BoundedLifetime boundedLifetime = lifetime as BoundedLifetime;
            if (boundedLifetime == null)
            {
                return default(LifetimeGraphIdentifier);
            }
            return _diagramGraphs.First(pair => pair.Value == boundedLifetime.DiagramGraph).Key;
        }

        public bool IsDiagramLifetimeOfAnyLifetimeGraph(Lifetime lifetime)
        {
            return _diagramGraphs.Any(pair => pair.Value.DiagramLifetime == lifetime);
        }
    }

    internal struct LifetimeGraphIdentifier
    {
        public LifetimeGraphIdentifier(int id)
        {
            Id = id;
        }

        public int Id { get; }

        public override bool Equals(object obj)
        {
            if (!(obj is LifetimeGraphIdentifier))
            {
                return false;
            }
            var otherIdentifier = (LifetimeGraphIdentifier)obj;
            return Id == otherIdentifier.Id;
        }

        public override int GetHashCode()
        {
            return Id.GetHashCode();
        }
    }
}
