using System.Xml.Linq;
using NationalInstruments.Dfir;
using NationalInstruments.Dfir.Component;
using NationalInstruments.MocCommon.Components.Compiler;

namespace Rebar.RebarTarget
{
    internal sealed class ApplicationIRBuilder : BuildableComponentIRBuilder
    {
        public override DfirRootRuntimeType RuntimeType => ApplicationComponentMocPluginPlugin.ApplicationRuntimeType;

        protected override ComponentRoot CreateComponentRoot(DfirRoot dfirRoot, XName targetedPlatform)
        {
            return new ApplicationRoot(dfirRoot, targetedPlatform);
        }
    }
}
