using System.Linq;
using NationalInstruments.Dfir;
using Rebar.Compiler.Nodes;

namespace Rebar.Compiler
{
    public static class StructureExtensions
    {
        internal static bool DoesStructureExecuteConditionally(this Structure structure)
        {
            Frame frame = structure as Frame;
            if (frame != null)
            {
                // TODO: handle multi-frame flat sequence structures
                return frame.BorderNodes.OfType<UnwrapOptionTunnel>().Any();
            }
            return structure is Nodes.Loop;
        }
    }
}
