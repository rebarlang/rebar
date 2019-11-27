using NationalInstruments.Compiler;
using NationalInstruments.Core;
using NationalInstruments.DataTypes;
using NationalInstruments.Dfir;
using NationalInstruments.SourceModel;
using NationalInstruments.SourceModel.Envoys;
using Rebar.SourceModel.TypeDiagram;
using Diagram = NationalInstruments.SourceModel.Diagram;

namespace Rebar.Compiler.TypeDiagram
{
    internal class TypeDiagramMocReflector : MocReflector, IReflectTypes
    {
        private readonly DfirModelMap _map;

        public TypeDiagramMocReflector(
            IReflectableModel source,
            ReflectionCancellationToken reflectionCancellationToken,
            IScheduledActivityManager scheduledActivityManager,
            IMessageDescriptorTranslator additionalErrorTexts,
            Envoy buildSpecSource,
            SpecAndQName specAndQName,
            DfirModelMap map) :
            base(source, reflectionCancellationToken, scheduledActivityManager, additionalErrorTexts, buildSpecSource, specAndQName)
        {
            _map = map;
        }

        public TypeDiagramMocReflector(MocReflector reflectorToCopy) : base(reflectorToCopy)
        {
            _map = ((TypeDiagramMocReflector)reflectorToCopy)._map;
        }

        protected override MocReflector CopyMocReflector()
        {
            return new TypeDiagramMocReflector(this);
        }

        public void ReflectTypes(DfirRoot dfirGraph)
        {
            IReflectableModel source;
            if (TryGetSource(out source))
            {
                var typeDiagramDefinition = (TypeDiagramDefinition)source;
                typeDiagramDefinition.UnderlyingType = dfirGraph.GetSelfType();
                VisitDiagram(typeDiagramDefinition.RootDiagram);
            }
        }

        private void VisitDiagram(Diagram diagram)
        {
            foreach (var node in diagram.Nodes)
            {
                VisitConnectable(node);
            }
        }

        private void VisitConnectable(Connectable connectable)
        {
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
    }
}
