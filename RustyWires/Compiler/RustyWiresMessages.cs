using NationalInstruments.Compiler.SemanticAnalysis;
using NationalInstruments.Dfir;
using NationalInstruments.SourceModel;
using RustyWires.Common;
using Terminal = NationalInstruments.Dfir.Terminal;

namespace RustyWires.Compiler
{
    internal static class RustyWiresMessages
    {
        private const string ResourceDictionaryName = "RustyWires.Resources.LocalizedStrings";

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
    }

    internal static class RustyWiresSemanticAnalysisHelpers
    {
        public static bool TestTerminalHasOwnedValueConnected(this Terminal terminal)
        {
            if (terminal.DataType.IsImmutableReferenceType() ||
                terminal.DataType.IsMutableReferenceType())
            {
                terminal.ParentNode.SetDfirMessage(RustyWiresMessages.TerminalDoesNotAcceptReference);
                return false;
            }
            return true;
        }

        public static bool TestTerminalHasMutableTypeConnected(this Terminal terminal)
        {
            Variable variable = terminal.GetVariable();
            if (!(variable.Mutable || variable.Type.IsMutableReferenceType()))
            {
                terminal.ParentNode.SetDfirMessage(RustyWiresMessages.TerminalDoesNotAcceptImmutableType);
                return false;
            }
            return true;
        }
    }
}
