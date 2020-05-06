using System.Linq;
using NationalInstruments;
using NationalInstruments.Compiler;
using NationalInstruments.Core;
using NationalInstruments.DataTypes;
using NationalInstruments.Dfir;
using NationalInstruments.SourceModel;
using NationalInstruments.SourceModel.Envoys;
using Rebar.Common;
using Rebar.Compiler.Nodes;
using Rebar.SourceModel;
using Diagram = NationalInstruments.SourceModel.Diagram;
using Structure = NationalInstruments.SourceModel.Structure;

namespace Rebar.Compiler
{
    internal class FunctionMocReflector : MocReflector, IReflectTypes
    {
        private readonly DfirModelMap _map;

        public FunctionMocReflector(
            IReflectableModel source,
            ReflectionCancellationToken reflectionCancellationToken,
            IScheduledActivityManager scheduledActivityManager,
            IMessageDescriptorTranslator additionalErrorTexts,
            Envoy buildSpecSource,
            CompileSpecification compileSpecification,
            DfirModelMap map)
            : base(source, reflectionCancellationToken, scheduledActivityManager, additionalErrorTexts, buildSpecSource, compileSpecification)
        {
            _map = map;
        }

        public FunctionMocReflector(MocReflector reflectorToCopy) : base(reflectorToCopy)
        {
            _map = ((FunctionMocReflector)reflectorToCopy)._map;
        }

        protected override MocReflector CopyMocReflector()
        {
            return new FunctionMocReflector(this);
        }

        public void ReflectTypes(DfirRoot dfirGraph)
        {
            IReflectableModel source;
            if (TryGetSource(out source))
            {
                var function = (Function)source;
                VisitDiagram(function.Diagram);
            }
        }

        private void VisitDiagram(Diagram diagram)
        {
            foreach (var node in diagram.Nodes)
            {
                VisitConnectable(node);
                var structure = node as Structure;
                if (structure != null)
                {
                    foreach (var borderNode in structure.BorderNodes)
                    {
                        VisitConnectable(borderNode);
                    }
                    foreach (var nestedDiagram in structure.NestedDiagrams)
                    {
                        VisitDiagram(nestedDiagram);
                    }
                }
            }
            foreach (var wire in diagram.Wires)
            {
                VisitWire(wire);
            }
        }

        private void VisitConnectable(Connectable connectable)
        {
            // Update terminals on a TerminateLifetime before reflecting types
            var terminateLifetime = connectable as TerminateLifetime;
            var typePassthrough = connectable as TypePassthrough;
            var structFieldAccessor = connectable as StructFieldAccessor;
            if (terminateLifetime != null)
            {
                VisitTerminateLifetime(terminateLifetime);
            }
            if (typePassthrough != null)
            {
                NIType type = _map.GetDfirForTerminal(typePassthrough.InputTerminals.ElementAt(0)).GetTrueVariable().Type.GetReferentType();
                typePassthrough.Type = type;
            }
            if (structFieldAccessor != null)
            {
                var structFieldAccessorDfir = (StructFieldAccessorNode)_map.GetDfirForModel(structFieldAccessor);
                structFieldAccessor.UpdateDependencies(structFieldAccessorDfir.StructType);
            }

            foreach (var nodeTerminal in connectable.Terminals)
            {
                NationalInstruments.Dfir.Terminal dfirTerminal = _map.GetDfirForTerminal(nodeTerminal);
                NIType typeToReflect = dfirTerminal.DataType;
                if (!nodeTerminal.DataType.Equals(typeToReflect))
                {
                    nodeTerminal.DataType = typeToReflect;
                }
            }
        }

        private void VisitWire(NationalInstruments.SourceModel.Wire wire)
        {
            var dfirWire = (NationalInstruments.Dfir.Wire)_map.GetDfirForModel(wire);
            bool isFirstVariableWire = dfirWire.GetIsFirstVariableWire();
            wire.SetIsFirstVariableWire(isFirstVariableWire);

            NationalInstruments.Dfir.Terminal dfirSourceTerminal;
            if (dfirWire.TryGetSourceTerminal(out dfirSourceTerminal))
            {
                wire.SetWireVariable(dfirSourceTerminal.GetFacadeVariable());
            }

            if (!isFirstVariableWire)
            {
                wire.SetWireBeginsMutableVariable(false);
            }
        }

        private void VisitTerminateLifetime(TerminateLifetime terminateLifetime)
        {
            var terminateLifetimeDfir = (TerminateLifetimeNode)_map.GetDfirForModel(terminateLifetime);
            if (terminateLifetimeDfir.RequiredInputCount != null && terminateLifetimeDfir.RequiredOutputCount != null)
            {
                terminateLifetime.UpdateTerminals(terminateLifetimeDfir.RequiredInputCount.Value, terminateLifetimeDfir.RequiredOutputCount.Value);
            }
            foreach (var pair in terminateLifetime.Terminals.Zip(terminateLifetimeDfir.Terminals))
            {
                if (!_map.ContainsTerminal(pair.Key))
                {
                    _map.MapTerminalAndType(pair.Key, pair.Value);
                }
            }
        }
    }
}
