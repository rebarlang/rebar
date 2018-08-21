using System.Linq;
using NationalInstruments;
using NationalInstruments.Compiler;
using NationalInstruments.DataTypes;
using NationalInstruments.Dfir;

namespace RustyWires.Compiler
{
    internal sealed class WireForkValidationTransform : IDfirTransform
    {
        public void Execute(DfirRoot dfirRoot, CompileCancellationToken cancellationToken)
        {
            foreach (Wire wire in dfirRoot.BlockDiagram.GetAllNodes().OfType<Wire>())
            {
                if (wire.SinkTerminals.HasMoreThan(1) && !WireTypeMayFork(wire.SourceTerminal.DataType))
                {
                    wire.SetDfirMessage(RustyWiresMessages.WireCannotFork);
                }
            }
        }

        private bool WireTypeMayFork(NIType wireType)
        {
            if (wireType.IsMutableValueType() || wireType.IsImmutableValueType())
            {
                return CanShallowCopyDataType(wireType.GetUnderlyingTypeFromRustyWiresType());
            }

            if (wireType.IsMutableReferenceType())
            {
                return false;
            }

            if (wireType.IsImmutableReferenceType())
            {
                return true;
            }

            return false;
        }

        private bool CanShallowCopyDataType(NIType dataType)
        {
            return dataType.IsNumeric();
        }
    }
}
