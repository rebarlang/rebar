using NationalInstruments.Composition;
using NationalInstruments.DataTypes;
using NationalInstruments.ExecutionFramework;

namespace Rebar.RebarTarget
{
    public class RuntimeExecutionTarget : IRuntimeExecutionTarget
    {
        public RuntimeExecutionTarget(ICompositionHost host)
        {
            Host = host;
        }

        public IApplicationReferenceProvider ApplicationReferenceProvider => null;

        public ICompositionHost Host { get; }

        public ITargetTypeSerializer TargetTypeSerializer => null;

        public void Uninitialize()
        {
        }
    }
}
