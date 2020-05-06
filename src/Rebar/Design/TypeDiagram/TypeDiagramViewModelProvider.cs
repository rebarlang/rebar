using NationalInstruments.Design;
using NationalInstruments.Shell;
using NationalInstruments.SourceModel;
using Rebar.SourceModel.TypeDiagram;

namespace Rebar.Design.TypeDiagram
{
    [ExportProvideViewModels(typeof(TypeDiagramEditor))]
    public class TypeDiagramViewModelProvider : ViewModelProvider
    {
        /// <inheritdoc />
        protected override void AddSupportedModels()
        {
            AddSupportedModel<DiagramLabel>(n => new DiagramLabelViewModel(n));
            AddSupportedModel<SelfType>(n => new SelfTypeViewModel(n /*, @"Resources\Diagram\Nodes\SelfType.png"*/));
            AddSupportedModel<PrimitiveType>(n => new BasicNodeViewModel(n, "Primitive Type"));
        }
    }
}
