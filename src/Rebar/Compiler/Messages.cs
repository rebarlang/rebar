using NationalInstruments.Compiler.SemanticAnalysis;
using NationalInstruments.Dfir;
using NationalInstruments.SourceModel;
using Rebar.Common;
using Terminal = NationalInstruments.Dfir.Terminal;

namespace Rebar.Compiler
{
    internal static class Messages
    {
        private const string ResourceDictionaryName = "Rebar.Resources.LocalizedStrings";

        private static readonly MessageDescriptor TerminalDoesNotAcceptReferenceDescriptor = new MessageDescriptor(ResourceDictionaryName, "TerminalDoesNotAcceptReference");

        public static readonly DfirMessage TerminalDoesNotAcceptReference =
            new DfirMessage(
                MessageSeverity.Error,
                SemanticAnalysisMessageCategories.Connection, // TODO
                TerminalDoesNotAcceptReferenceDescriptor);

        private static readonly MessageDescriptor TerminalDoesNotAcceptImmutableTypeDescriptor = new MessageDescriptor(ResourceDictionaryName, "TerminalDoesNotAcceptImmutableType");

        public static readonly DfirMessage TerminalDoesNotAcceptImmutableType =
            new DfirMessage(
                MessageSeverity.Error,
                SemanticAnalysisMessageCategories.Connection,
                TerminalDoesNotAcceptImmutableTypeDescriptor);

        private static readonly MessageDescriptor TerminateLifetimeInputLifetimesNotUniqueDescriptor = new MessageDescriptor(ResourceDictionaryName, "TerminateLifetimeInputLifetimesNotUnique");

        public static readonly DfirMessage TerminateLifetimeInputLifetimesNotUnique =
            new DfirMessage(
                MessageSeverity.Error,
                SemanticAnalysisMessageCategories.Connection,
                TerminateLifetimeInputLifetimesNotUniqueDescriptor);

        private static readonly MessageDescriptor TerminateLifetimeInputLifetimeCannotBeTerminatedDescriptor = new MessageDescriptor(ResourceDictionaryName, "TerminateLifetimeInputLifetimeCannotBeTerminated");

        public static readonly DfirMessage TerminateLifetimeInputLifetimeCannotBeTerminated =
            new DfirMessage(
                MessageSeverity.Error,
                SemanticAnalysisMessageCategories.Connection,
                TerminateLifetimeInputLifetimeCannotBeTerminatedDescriptor);

        private static readonly MessageDescriptor TerminateLifetimeNotAllVariablesInLifetimeConnectedDescriptor = new MessageDescriptor(ResourceDictionaryName, "TerminateLifetimeNotAllVariablesInLifetimeConnected");

        public static readonly DfirMessage TerminateLifetimeNotAllVariablesInLifetimeConnected =
            new DfirMessage(
                MessageSeverity.Error,
                SemanticAnalysisMessageCategories.Connection,
                TerminateLifetimeNotAllVariablesInLifetimeConnectedDescriptor);

        private static readonly MessageDescriptor WiredReferenceDoesNotLiveLongEnoughDescriptor = new MessageDescriptor(ResourceDictionaryName, "WiredReferenceDoesNotLiveLongEnough");

        public static readonly DfirMessage WiredReferenceDoesNotLiveLongEnough =
            new DfirMessage(
                MessageSeverity.Error,
                SemanticAnalysisMessageCategories.Connection,
                WiredReferenceDoesNotLiveLongEnoughDescriptor);

        private static readonly MessageDescriptor WireCannotForkDescriptor = new MessageDescriptor(ResourceDictionaryName, "WireCannotFork");

        public static readonly DfirMessage WireCannotFork =
            new DfirMessage(
                MessageSeverity.Error,
                SemanticAnalysisMessageCategories.Connection,
                WireCannotForkDescriptor);

        private static readonly MessageDescriptor FeatureNotEnabledDescriptor = new MessageDescriptor(ResourceDictionaryName, "FeatureNotEnabled");

        public static readonly DfirMessage FeatureNotEnabled =
            new DfirMessage(
                MessageSeverity.Error,
                SemanticAnalysisMessageCategories.Connection,
                FeatureNotEnabledDescriptor);
    }

    internal static class SemanticAnalysisHelpers
    {
        public static bool TestTerminalHasOwnedValueConnected(this Terminal terminal)
        {
            if (terminal.DataType.IsImmutableReferenceType() ||
                terminal.DataType.IsMutableReferenceType())
            {
                terminal.ParentNode.SetDfirMessage(Messages.TerminalDoesNotAcceptReference);
                return false;
            }
            return true;
        }

        public static bool TestTerminalHasMutableTypeConnected(this Terminal terminal)
        {
            VariableReference variable = terminal.GetFacadeVariable();
            if (!(variable.Mutable || variable.Type.IsMutableReferenceType()))
            {
                terminal.ParentNode.SetDfirMessage(Messages.TerminalDoesNotAcceptImmutableType);
                return false;
            }
            return true;
        }
    }
}
