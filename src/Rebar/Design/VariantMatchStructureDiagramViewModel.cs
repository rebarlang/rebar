using NationalInstruments.Design;
using NationalInstruments.SourceModel;
using Rebar.SourceModel;

namespace Rebar.Design
{
    internal class VariantMatchStructureDiagramViewModel : ViewElementParentViewModel
    {
        public VariantMatchStructureDiagramViewModel(IViewElementParent parent)
            : base(parent)
        {
        }

        public string Pattern => VariantMatchStructureEditor.GetDiagramPattern((VariantMatchStructureDiagram)Model);
    }
}
