using System;
using NationalInstruments.Dfir;
using Rebar.Common;

namespace Rebar.Compiler
{
    internal class NodeInsertionTypeUnificationResultFactory : ITypeUnificationResultFactory
    {
        public ITypeUnificationResult GetTypeUnificationResult(Terminal terminal, TypeVariableReference terminalTypeVariable, TypeVariableReference unifyWith)
        {
            return new RequireSuccessTypeUnificationResult();
        }

        /// <summary>
        /// Implementation of <see cref="ITypeUnificationResult"/> that throws an exception on any unification failure.
        /// </summary>
        private sealed class RequireSuccessTypeUnificationResult : ITypeUnificationResult
        {
            /// <inheritdoc />
            public void AddFailedTypeConstraint(Constraint constraint)
            {
                throw new InvalidOperationException("Cannot fail a type constraint during this unification.");
            }

            /// <inheritdoc />
            public void SetExpectedMutable()
            {
                throw new InvalidOperationException("Cannot fail a mutability check during this unification.");
            }

            /// <inheritdoc />
            public void SetTypeMismatch()
            {
                throw new InvalidOperationException("Cannot have mismatched types during this unification.");
            }
        }
    }
}
