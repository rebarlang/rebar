using NationalInstruments.Compiler;
using NationalInstruments.Compiler.SemanticAnalysis;
using NationalInstruments.Dfir;
using NationalInstruments.SourceModel;

namespace Rebar.Compiler
{
    /// <summary>
    /// Transform that adds errors to <see cref="DfirRoot"/>s that are being compiled for non-RebarTargets.
    /// </summary>
    internal class RebarSupportedTargetTransform : IDfirTransform
    {
        private const string TransformCategory = "RebarSupportedTargetTransform";

        /// <inheritdoc />
        public void Execute(DfirRoot dfirRoot, CompileCancellationToken cancellationToken)
        {
            dfirRoot.MarkErrorCategoryChanged(TransformCategory);

            var targetKind = dfirRoot.BuildSpec.TargetCompiler.TargetKind;
            if (targetKind != RebarTarget.TargetCompiler.Kind)
            {
                dfirRoot.SetDfirMessage(MessageSeverity.Error, TransformCategory, AllModelsOfComputationErrorMessages.UnsupportedDocumentTypeOnTarget);
            }
        }
    }
}
