using System;
using System.Collections.Generic;
using System.Windows;
using NationalInstruments.Core;
using NationalInstruments.SourceModel;
using NationalInstruments.VI.Design;
using NationalInstruments.VI.SourceModel;
using Rebar.SourceModel;

namespace Rebar.Design
{
    public class OptionPatternStructureEditor : StackedStructureViewModel
    {
        private const string SomePattern = "Some";
        private const string NonePattern = "None";

        public OptionPatternStructureEditor(StackedStructure model) : base(model)
        {
        }

        protected override PlatformVisual CreateVisualControl(PlatformVisual parent)
        {
            return new OptionPatternStructureControl();
        }

        protected override ResourceUri ForegroundUri => new ResourceUri(GetType(), "Resources/" + Element.GetType().Name);

        /// <inheritdoc />
        public override IEnumerable<RenderData> RenderData
        {
            get
            {
                var data = base.ForegroundImageData;
                data.Slicing = new SMThickness(10);
                data.Opacity = BorderOpacity;
                data.HorizontalAlignment = HorizontalAlignment.Stretch;
                data.VerticalAlignment = VerticalAlignment.Stretch;
                data.SlicingStretchMode = SlicingStretchMode.TiledTile;
                yield return data;
            }
        }

        public override string Pattern
        {
            get
            {
                var selectedDiagram = (OptionPatternStructureDiagram)SelectedDiagram.Element;
                return GetDiagramPattern(selectedDiagram);
            }
            set
            {
                throw new NotImplementedException();
            }
        }

        public override IEnumerable<string> Patterns
        {
            get
            {
                yield return SomePattern;
                yield return NonePattern;
            }
        }

        public override void AddDiagram()
        {
            throw new NotImplementedException();
        }

        public override bool FinishEdit()
        {
            return true;
        }

        public static string GetDiagramPattern(NestedDiagram diagram)
        {
            int selectedDiagramIndex = diagram.Index;
            return selectedDiagramIndex == 1 ? NonePattern : SomePattern;
        }
    }
}
