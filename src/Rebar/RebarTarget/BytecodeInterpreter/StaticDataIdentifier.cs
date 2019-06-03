using NationalInstruments.Dfir;

namespace Rebar.RebarTarget.Execution
{
    public sealed class StaticDataIdentifier
    {
        private readonly object _identifyingObject;

        private StaticDataIdentifier(object identifyingObject)
        {
            _identifyingObject = identifyingObject;
        }

        public static StaticDataIdentifier CreateFromNode(Node node)
        {
            return new StaticDataIdentifier(node);
        }

        public override bool Equals(object obj)
        {
            var otherIdentifier = obj as StaticDataIdentifier;
            return otherIdentifier != null && otherIdentifier._identifyingObject == _identifyingObject;
        }

        public override int GetHashCode()
        {
            return _identifyingObject?.GetHashCode() ?? 0;
        }
    }
}
