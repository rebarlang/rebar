using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using NationalInstruments;
using NationalInstruments.Core;
using NationalInstruments.DataTypes;
using NationalInstruments.SourceModel;
using NationalInstruments.VI.Design;
using Rebar.SourceModel;

namespace Rebar.Design
{
    internal sealed class VariantMatchStructureEditor : StackedStructureViewModel
    {
        public VariantMatchStructureEditor(VariantMatchStructure model) : base(model)
        {
        }

        /// <inheritdoc />
        protected override PlatformVisual CreateVisualControl(PlatformVisual parent) => new VariantMatchStructureControl();

        /// <inheritdoc />
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

        /// <inheritdoc />
        protected override void NotifyPropertyChanged(string name)
        {
            base.NotifyPropertyChanged(name);
            if (name == nameof(Opacity))
            {
                NotifyPropertyChanged(nameof(CaseSelectorOpacity));
            }
        }

        public override string Pattern
        {
            get
            {
                var selectedDiagram = (VariantMatchStructureDiagram)SelectedDiagram.Element;
                return GetDiagramPattern(selectedDiagram);
            }
            set
            {
                throw new NotImplementedException();
            }
        }

        /// <inheritdoc />
        public override IEnumerable<string> Patterns => GetCaseNames(VariantMatchStructure);

        public VariantMatchStructure VariantMatchStructure => (VariantMatchStructure)Model;

        public override void AddDiagram()
        {
            throw new NotImplementedException();
        }

        public override bool FinishEdit() => true;

        public static string GetDiagramPattern(VariantMatchStructureDiagram diagram)
        {
            int selectedDiagramIndex = diagram.Index;
            return GetCaseNames((VariantMatchStructure)diagram.Owner).ElementAt(selectedDiagramIndex);
        }

        private static IEnumerable<string> GetCaseNames(VariantMatchStructure variantMatchStructure)
        {
            int diagramCount = variantMatchStructure.NestedDiagrams.Count();
            NIType variantType = variantMatchStructure.Type;
            if (variantType.IsUnion())
            {
                IEnumerable<NIType> variantFields = variantType.GetFields();
                if (variantFields.HasExactly(diagramCount))
                {
                    return variantFields.Select(f => f.GetName());
                }
            }
            return string.Empty.Repeat(diagramCount);
        }
    }
}
