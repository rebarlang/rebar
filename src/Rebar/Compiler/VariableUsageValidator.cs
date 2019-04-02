using System;
using NationalInstruments.Compiler.SemanticAnalysis;
using NationalInstruments.DataTypes;
using NationalInstruments.Dfir;
using Rebar.Common;

namespace Rebar.Compiler
{
    internal struct VariableUsageValidator
    {
        private readonly VariableReference _facadeVariable, _trueVariable;
        private readonly Terminal _terminal;
        private readonly bool _terminalHasType;

        public VariableUsageValidator(Terminal terminal, bool validateUsageWithinLifetime = true, bool validateTerminalConnected = true)
        {
            _facadeVariable = terminal.GetFacadeVariable();
            _trueVariable = terminal.GetTrueVariable();
            _terminal = terminal;
            _terminalHasType = true;
            if (validateTerminalConnected)
            {
                _terminalHasType = _terminal.TestRequiredTerminalConnected();                
            }
            if (validateUsageWithinLifetime)
            {
                TestUsageWithinLifetime();
            }
        }

        private void TestUsageWithinLifetime()
        {
            // TODO: need a more generic check for whether a type has a lifetime
            if (_facadeVariable.Type.IsRebarReferenceType())
            {
                Lifetime lifetime = _facadeVariable.Lifetime;
                bool isUsageWithinLifetime = lifetime.IsBounded || !lifetime.IsEmpty;
                if (!isUsageWithinLifetime)
                {
                    _terminal.SetDfirMessage(Messages.WiredReferenceDoesNotLiveLongEnough);
                }
            }
        }

        public bool TestVariableIsMutableType()
        {
            if (!_terminalHasType)
            {
                return false;
            }
            bool isMutable = _facadeVariable.Type.IsRebarReferenceType()
                ? _facadeVariable.Type.IsMutableReferenceType()
                : _facadeVariable.Mutable;
            if (!isMutable)
            {
                _terminal.ParentNode.SetDfirMessage(Messages.TerminalDoesNotAcceptImmutableType);
                return false;
            }
            return true;
        }

        public bool TestVariableIsOwnedType()
        {
            if (!_terminalHasType)
            {
                return false;
            }
            if (_trueVariable.Type.IsRebarReferenceType())
            {
                _terminal.ParentNode.SetDfirMessage(Messages.TerminalDoesNotAcceptReference);
                return false;
            }
            return true;
        }

        public bool TestExpectedUnderlyingType(NIType expectedType)
        {
            if (!_terminalHasType)
            {
                return false;
            }
            NIType underlyingType = _facadeVariable.Type.GetTypeOrReferentType();
            if (underlyingType != expectedType)
            {
                _terminal.SetDfirMessage(TerminalUserMessages.CreateTypeConflictMessage(underlyingType, expectedType));
                return false;
            }
            return true;
        }

        public bool TestUnderlyingType(Func<NIType, bool> underlyingTypePredicate, NIType expectedTypeExample)
        {
            if (!_terminalHasType)
            {
                return false;
            }
            NIType underlyingType = _facadeVariable.Type.GetTypeOrReferentType();
            if (!underlyingTypePredicate(underlyingType))
            {
                _terminal.SetDfirMessage(TerminalUserMessages.CreateTypeConflictMessage(underlyingType, expectedTypeExample));
                return false;
            }
            return true;
        }

        public bool TestSameUnderlyingTypeAs(VariableUsageValidator other)
        {
            if (!_terminalHasType)
            {
                return false;
            }
            NIType otherUnderlyingType = other._facadeVariable.Type.GetTypeOrReferentType();
            return TestExpectedUnderlyingType(otherUnderlyingType);
        }
    }
}
