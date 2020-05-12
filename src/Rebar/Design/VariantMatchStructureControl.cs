using NationalInstruments.VI.Design;
using Rebar.SourceModel;

namespace Rebar.Design
{
    public sealed class VariantMatchStructureControl : StackedStructureControl
    {
        protected override void UpdatePattern()
        {
            string pattern = VariantMatchStructureEditor.GetDiagramPattern((VariantMatchStructureDiagram)Model.SelectedDiagram);
            SelectorText.Inlines.Clear();
            SelectorText.Inlines.Add(pattern);
        }
    }
}
