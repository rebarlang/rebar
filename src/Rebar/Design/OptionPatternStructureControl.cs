using NationalInstruments.VI.Design;

namespace Rebar.Design
{
    public class OptionPatternStructureControl : StackedStructureControl
    {
        protected override void UpdatePattern()
        {
            string pattern = OptionPatternStructureEditor.GetDiagramPattern(Model.SelectedDiagram);
            SelectorText.Inlines.Clear();
            SelectorText.Inlines.Add(pattern);
        }
    }
}
