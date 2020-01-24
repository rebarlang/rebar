using NationalInstruments.Core;
using NationalInstruments.Design;
using NationalInstruments.SourceModel;
using Rebar.SourceModel;

namespace Rebar.Design
{
    public class ConstructorViewModel : NodeViewModel
    {
        public ConstructorViewModel(Constructor node) : base(node)
        {
        }

        /// <inheritdoc />
        public override RenderDataCollection ListViewIconDataSource => new RenderDataCollection();

        /// <inheritdoc />
        protected override bool FilterTerminalForListView(Terminal terminal) => terminal.Index > 0;

        /// <inheritdoc />
        public override ResizeAdornerOptions GetResizeAdornerOptions(bool softSelect)
            => new ResizeAdornerOptions() { Directions = ResizeDirections.Right };

        public bool HeaderTextVisible => true;
    }
}
