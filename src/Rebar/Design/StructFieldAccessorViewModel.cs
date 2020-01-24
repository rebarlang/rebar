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
    }
}
