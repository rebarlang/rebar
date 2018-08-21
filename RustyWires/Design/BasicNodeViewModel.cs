using System.Collections.Generic;
using System.Windows;
using System.Windows.Media;
using NationalInstruments.Core;
using NationalInstruments.Design;
using NationalInstruments.SourceModel;
using RustyWires.SourceModel;

namespace RustyWires.Design
{
    public class BasicNodeViewModel : NodeViewModel
    {
        private readonly string _name;

        public BasicNodeViewModel(Node node, string name)
            : base(node)
        {
            _name = name;
        }

#if FALSE
        public override IEnumerable<RenderData> RenderData
        {
            get
            {
                var background = StockAssets.GetPrimitiveRectangle(this);
                background.Stretch = Stretch.Fill;
                background.HorizontalAlignment = HorizontalAlignment.Stretch;
                background.VerticalAlignment = VerticalAlignment.Stretch;
                yield return background;
                // yield return new ViewModelImageData(this) { ImageUri = ForegroundUri };
            }
        }
#endif

        public override string Name => _name;
        // protected override ResourceUri ForegroundUri => new ResourceUri(this, "Resources/Diagram/Nodes/DropNode");
    }
}
