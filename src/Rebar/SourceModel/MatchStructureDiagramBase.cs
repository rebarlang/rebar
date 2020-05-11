using NationalInstruments.SourceModel.Persistence;
using NationalInstruments.VI.SourceModel;

namespace Rebar.SourceModel
{
    public abstract class MatchStructureDiagramBase : StackedStructureDiagram
    {
        /// <inheritdoc />
        /// <remarks>This is necessary because the ancestor class NestedDiagram returns true for this.</remarks>
        public override bool DoNotGenerateThisElement(ElementGenerationOptions options) => false;
    }
}
