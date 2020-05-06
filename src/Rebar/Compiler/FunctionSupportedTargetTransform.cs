using NationalInstruments.CommonModel;
using NationalInstruments.Compiler;
using NationalInstruments.Compiler.SemanticAnalysis;
using NationalInstruments.Dfir;

namespace Rebar.Compiler
{
    /// <summary>
    /// Transform that adds errors to <see cref="DfirRoot"/>s that are being compiled for non-RebarTargets.
    /// </summary>
    internal class RebarSupportedTargetTransform : IDfirTransform
    {
        private const string TransformCategory = "RebarSupportedTargetTransform";

        private readonly ISemanticAnalysisTargetInfo _semanticAnalysisTargetInfo;

        public RebarSupportedTargetTransform(ISemanticAnalysisTargetInfo semanticAnalysisTargetInfo)
        {
            _semanticAnalysisTargetInfo = semanticAnalysisTargetInfo;
        }

        /// <inheritdoc />
        public void Execute(DfirRoot dfirRoot, CompileCancellationToken cancellationToken)
        {
            dfirRoot.MarkErrorCategoryChanged(TransformCategory);

            if (_semanticAnalysisTargetInfo.TargetKind != RebarTarget.TargetCompiler.Kind)
            {
                dfirRoot.SetDfirMessage(MessageSeverity.Error, TransformCategory, AllModelsOfComputationErrorMessages.UnsupportedDocumentTypeOnTarget);
            }
        }
    }
}
