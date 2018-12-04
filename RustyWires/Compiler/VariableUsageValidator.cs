using System;
using NationalInstruments.Compiler.SemanticAnalysis;
using NationalInstruments.DataTypes;
using NationalInstruments.Dfir;

namespace RustyWires.Compiler
{
    internal struct VariableUsageValidator
    {
        private readonly Variable _variable;
        private readonly Terminal _terminal;

        public VariableUsageValidator(Variable variable, Terminal terminal, bool validateUsageWithinLifetime = true)
        {
            _variable = variable;
            _terminal = terminal;
            _terminal.TestRequiredTerminalConnected();
            if (validateUsageWithinLifetime)
            {
                TestUsageWithinLifetime();
            }
        }

        private void TestUsageWithinLifetime()
        {
            // TODO: need a more generic check for whether a type has a lifetime
            if (_variable != null && _variable.Type.IsRWReferenceType())
            {
                Lifetime lifetime = _variable.Lifetime;
                bool isUsageWithinLifetime = lifetime.IsBounded || !lifetime.IsEmpty;
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
            if (!_variable.Type.IsMutableType())
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

        public bool TestUnderlyingType(Func<NIType, bool> underlyingTypePredicate, NIType expectedTypeExample)
        {
            if (_variable == null)
            {
                return false;
            }
            NIType underlyingType = _variable.Type.GetUnderlyingTypeFromRustyWiresType();
            if (!underlyingTypePredicate(underlyingType))
            {
                _terminal.SetDfirMessage(TerminalUserMessages.CreateTypeConflictMessage(underlyingType, expectedTypeExample));
                return false;
            }
            return true;
        }

        public bool TestSameUnderlyingTypeAs(VariableUsageValidator other)
        {
            if (other._variable == null)
            {
                return false;
            }
            NIType otherUnderlyingType = other._variable.Type.GetUnderlyingTypeFromRustyWiresType();
            return TestExpectedUnderlyingType(otherUnderlyingType);
        }
    }
}
