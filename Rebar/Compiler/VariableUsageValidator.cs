using System;
using NationalInstruments.Compiler.SemanticAnalysis;
using NationalInstruments.DataTypes;
using NationalInstruments.Dfir;
using Rebar.Common;

namespace Rebar.Compiler
{
    internal struct VariableUsageValidator
    {
        private readonly Variable _variable;
        private readonly Terminal _terminal;

        public VariableUsageValidator(Variable variable, Terminal terminal, bool validateUsageWithinLifetime = true, bool validateTerminalConnected = true)
        {
            _variable = variable;
            _terminal = terminal;
            if (validateTerminalConnected)
            {
                _terminal.TestRequiredTerminalConnected();
            }
            if (validateUsageWithinLifetime)
            {
                TestUsageWithinLifetime();
            }
        }

        private void TestUsageWithinLifetime()
        {
            // TODO: need a more generic check for whether a type has a lifetime
            if (_variable != null && _variable.Type.IsRebarReferenceType())
            {
                Lifetime lifetime = _variable.Lifetime;
                bool isUsageWithinLifetime = lifetime.IsBounded || !lifetime.IsEmpty;
                if (!isUsageWithinLifetime)
                {
                    _terminal.SetDfirMessage(Messages.WiredReferenceDoesNotLiveLongEnough);
                }
            }
        }

        public bool TestVariableIsMutableType()
        {
            if (_variable == null)
            {
                return false;
            }
            bool isMutable =  _variable.Type.IsRebarReferenceType()
                ? _variable.Type.IsMutableReferenceType()
                : _variable.Mutable;
            // TODO: change to using _variable.Mutable || _variable.Type.IsMutableReference
            if (!isMutable)
            {
                _terminal.ParentNode.SetDfirMessage(Messages.TerminalDoesNotAcceptImmutableType);
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
            if (_variable.Type.IsRebarReferenceType())
            {
                _terminal.ParentNode.SetDfirMessage(Messages.TerminalDoesNotAcceptReference);
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
            NIType underlyingType = _variable.Type.GetTypeOrReferentType();
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
            NIType underlyingType = _variable.Type.GetTypeOrReferentType();
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
            NIType otherUnderlyingType = other._variable.Type.GetTypeOrReferentType();
            return TestExpectedUnderlyingType(otherUnderlyingType);
        }
    }
}
