using System.Xml.Linq;
using NationalInstruments.Dfir;
using NationalInstruments.Dfir.Component;

namespace Rebar.RebarTarget
{
    internal sealed class ApplicationRoot : BuildableComponentRoot
    {
        public ApplicationRoot(DfirRoot dfirRoot, XName targetedPlatform)
            : base(dfirRoot, targetedPlatform)
        {
        }

        private ApplicationRoot(ApplicationRoot rootToCopy, NodeCopyInfo copyInfo)
            : base(rootToCopy, copyInfo)
        {
        }

        public override NonDiagramNode Copy(NodeCopyInfo copyInfo)
        {
            return new ApplicationRoot(this, copyInfo);
        }
    }
}
