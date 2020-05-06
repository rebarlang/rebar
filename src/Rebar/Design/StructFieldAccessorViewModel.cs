using NationalInstruments.Core;
using NationalInstruments.Design;
using Rebar.SourceModel;

namespace Rebar.Design
{
    public class StructFieldAccessorViewModel : GrowNodeViewModel
    {
        public StructFieldAccessorViewModel(StructFieldAccessor element) : base(element)
        {
        }

        /// <inheritdoc />
        public override RenderDataCollection ListViewIconDataSource => new RenderDataCollection();

        /// <inheritdoc />
        /// <remarks>
        /// In NXG 5.0, the base class GrowNodeViewModel overrides this to false. This ultimately leads
        /// to the NodeListViewControl for this node exiting early from handling mouse left button events
        /// instead of showing the terminal field selection menu. Arguably this is a bug in NXG; we don't
        /// actually want to allow reordering, and reordering should not be tied to showing terminal menus.
        /// </remarks>
        public override bool CanReorderListViewChunks => true;
    }
}
