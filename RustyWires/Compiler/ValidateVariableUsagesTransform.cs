using NationalInstruments;
using NationalInstruments.DataTypes;
using NationalInstruments.Dfir;
using RustyWires.Compiler.Nodes;

namespace RustyWires.Compiler
{
    internal class ValidateVariableUsagesTransform : VisitorTransformBase
    {
        protected override void VisitNode(Node node)
        {
            var rustyWiresDfirNode = node as RustyWiresDfirNode;
            rustyWiresDfirNode?.CheckVariableUsages();
        }

        protected override void VisitWire(Wire wire)
        {
            Variable sourceVariable = wire.SourceTerminal.GetVariable();
            if (wire.SinkTerminals.HasMoreThan(1) && sourceVariable != null && !WireTypeMayFork(sourceVariable.Type))
            {
                wire.SetDfirMessage(RustyWiresMessages.WireCannotFork);
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

        protected override void VisitBorderNode(BorderNode borderNode)
        {
            var rustyWiresBorderNode = borderNode as RustyWiresBorderNode;
            rustyWiresBorderNode?.CheckVariableUsages();
        }
    }
}
