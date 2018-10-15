using System.Linq;
using NationalInstruments.Compiler.SemanticAnalysis;
using NationalInstruments.DataTypes;
using NationalInstruments.Dfir;

namespace RustyWires.Compiler
{
    internal struct VariableUsageValidator
    {
        private readonly Variable _variable;
        private readonly Terminal _terminal;

        public VariableUsageValidator(Variable variable, Terminal terminal)
        {
            _variable = variable;
            _terminal = terminal;
            _terminal.TestRequiredTerminalConnected();
            TestUsageWithinLifetime();
        }

        private void TestUsageWithinLifetime()
        {
            // TODO: need a more generic check for whether a type has a lifetime
            if (_variable != null && _variable.Type.IsRWReferenceType())
            {
                Lifetime lifetime = _variable.Lifetime;
                bool isUsageWithinLifetime = false;
                switch (lifetime.Category)
                {
                    case LifetimeCategory.Empty:
                        break;
                    case LifetimeCategory.FunctionStatic:
                    case LifetimeCategory.FunctionParameter:
                        isUsageWithinLifetime = true;
                        break;
                    case LifetimeCategory.Structure:
                        {
                            Node parent = _terminal.ParentNode;
                            while (parent != null)
                            {
                                if (parent == lifetime.Origin)
                                {
                                    isUsageWithinLifetime = true;
                                    break;
                                }
                                parent = parent.ParentNode;
                            }
                        }
                        break;
                    case LifetimeCategory.Node:
                        {
                            // determine whether _terminal's node lies downstream of the node beginning the lifetime and
                            // upstream of the recompose node terminating it (if one exists)
                            // Note: Since recompose nodes don't exist yet, it should not be possible for a terminal that
                            // uses a variable with LifetimeCategory.Node not to be within its lifetime.
                            Node parent = _terminal.ParentNode;
                            while (parent != null)
                            {
                                if (parent.GetUpstreamNodesSameDiagram(true).Contains(lifetime.Origin))
                                {
                                    isUsageWithinLifetime = true;
                                    break;
                                }
                                parent = parent.ParentNode;
                            }
                        }
                        break;
                }
                if (!isUsageWithinLifetime)
                {
                    _terminal.SetDfirMessage(RustyWiresMessages.WiredReferenceDoesNotLiveLongEnough);
                }
            }
        }

        public bool TestVariableIsMutableType()
        {
            if (_variable == null)
            {
                return false;
            }
            if (_variable.Type.IsImmutableReferenceType() || _variable.Type.IsImmutableValueType())
            {
                _terminal.ParentNode.SetDfirMessage(RustyWiresMessages.TerminalDoesNotAcceptImmutableType);
                return false;
            }
            return true;
        }

        public bool TestVariableIsOwnedType()
        {
            if (_variable == null)
            {
                return false;
            }
            if (_variable.Type.IsImmutableReferenceType() ||
                _variable.Type.IsMutableReferenceType())
            {
                _terminal.ParentNode.SetDfirMessage(RustyWiresMessages.TerminalDoesNotAcceptReference);
                return false;
            }
            return true;
        }

        public bool TestExpectedUnderlyingType(NIType expectedType)
        {
            if (_variable == null)
            {
                return false;
            }
            NIType underlyingType = _variable.Type.GetUnderlyingTypeFromRustyWiresType();
            if (underlyingType != expectedType)
            {
                _terminal.SetDfirMessage(TerminalUserMessages.CreateTypeConflictMessage(underlyingType, expectedType));
                return false;
            }
            return true;
        }
    }
}
