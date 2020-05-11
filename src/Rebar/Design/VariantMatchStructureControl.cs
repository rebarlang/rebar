using NationalInstruments.VI.Design;

namespace Rebar.Design
{
    public sealed class VariantMatchStructureControl : StackedStructureControl
    {
        protected override void UpdatePattern()
        {
            string pattern = VariantMatchStructureEditor.GetDiagramPattern(Model.SelectedDiagram);
            SelectorText.Inlines.Clear();
            SelectorText.Inlines.Add(pattern);
        }
    }
}
