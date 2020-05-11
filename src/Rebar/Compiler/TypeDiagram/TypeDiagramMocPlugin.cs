using System;
using System.Collections.Generic;
using System.Linq;
using NationalInstruments.Compiler;
using NationalInstruments.Composition;
using NationalInstruments.Core;
using NationalInstruments.DataTypes;
using NationalInstruments.Dfir;
using Rebar.Common;
using Rebar.SourceModel.TypeDiagram;

namespace Rebar.Compiler.TypeDiagram
{
    internal class TypeDiagramMocPlugin : MocPlugin
    {
        private readonly ICompositionHost _host;

        public TypeDiagramMocPlugin(ICompositionHost host, CompilerService compilerService) : base(host, compilerService)
        {
            _host = host;
        }

        public override MocTransformManager GenerateMocTransformManagerAndSourceDfir(DfirCreationArguments creationArguments)
        {
            var typeDiagramDefinition = (TypeDiagramDefinition)creationArguments.SourceModel;
            var typeDiagramDfirBuilder = new TypeDiagramDfirBuilder();
            typeDiagramDfirBuilder.VisitTypeDiagram(typeDiagramDefinition.RootDiagram);
            DfirRoot dfirRoot = typeDiagramDfirBuilder.TypeDiagramDfirRoot;

            TypeDiagramMocReflector reflector = new TypeDiagramMocReflector(
                typeDiagramDefinition,
                creationArguments.ReflectionCancellationToken,
                _host.GetSharedExportedValue<ScheduledActivityManager>(),
                AdditionalErrorTexts,
                creationArguments.BuildSpecSource,
                creationArguments.CompileSpecification,
                typeDiagramDfirBuilder.DfirModelMap);
            creationArguments.PrebuildTransform(dfirRoot, reflector, this);

            ExecutionOrderSortingVisitor.SortDiagrams(dfirRoot);
            return GenerateMocTransformManager(creationArguments.CompileSpecification, dfirRoot, new CompileCancellationToken());
        }

        public override MocTransformManager GenerateMocTransformManager(
            CompileSpecification compileSpecification,
            DfirRoot sourceDfir,
            CompileCancellationToken cancellationToken)
        {
            var semanticAnalysisTransforms = new List<IDfirTransformBase>()
            {
                new CreateTypeDiagramNodeFacadesTransform(),
                new UnifyTypesAcrossWiresTransform(),
                new ValidateTypeUsagesTransform(),
                new ReflectVariablesToTerminalsTransform(),
                new StandardTypeReflectionTransform()
            };

            ReflectErrorsTransform.AddErrorReflection(semanticAnalysisTransforms, CompilePhase.SemanticAnalysis);

            var toTargetDfirTransforms = new List<IDfirTransformBase>()
            {
            };

            return new StandardMocTransformManager(
                compileSpecification,
                sourceDfir,
                semanticAnalysisTransforms,
                toTargetDfirTransforms,
                _host.GetSharedExportedValue<ScheduledActivityManager>());
        }

        public override DfirRootRuntimeType GetRuntimeType(IReadOnlySymbolTable symbolTable) => TypeDiagramRuntimeType;

        public static DfirRootRuntimeType TypeDiagramRuntimeType { get; } = new DfirRootRuntimeType("RebarTypeDiagram");
    }

    public static class TypeDiagramExtension
    {
        public static NIType GetSelfType(this DfirRoot typeDiagramDfirRoot)
        {
            Nodes.SelfTypeNode selfTypeNode = typeDiagramDfirRoot.BlockDiagram.Nodes.OfType<Nodes.SelfTypeNode>().First();
            if (selfTypeNode == null)
            {
                return NIType.Unset;
            }
            return BuildTypeFromSelfType(selfTypeNode, typeDiagramDfirRoot.CompileSpecification.Name.SourceName.Last);
        }

        private static NIType BuildTypeFromSelfType(Nodes.SelfTypeNode selfTypeNode, string typeName)
        {
            int fieldIndex = 0;
            switch (selfTypeNode.Mode)
            {
                case SelfTypeMode.Struct:
                    NIClassBuilder classBuilder = NITypes.Factory.DefineValueClass(typeName);
                    foreach (Terminal inputTerminal in selfTypeNode.InputTerminals)
                    {
                        NIType fieldType = inputTerminal.GetTrueVariable().Type;
                        classBuilder.DefineField(fieldType, $"_{fieldIndex}", NIFieldAccessPolicies.ReadWrite);
                        ++fieldIndex;
                    }
                    return classBuilder.CreateType();
                case SelfTypeMode.Variant:
                    NIUnionBuilder unionBuilder = NITypes.Factory.DefineUnion(typeName);
                    foreach (Terminal inputTerminal in selfTypeNode.InputTerminals)
                    {
                        NIType fieldType = inputTerminal.GetTrueVariable().Type;
                        unionBuilder.DefineField(fieldType, $"_{fieldIndex}");
                        ++fieldIndex;
                    }
                    unionBuilder.AddTypeKeywordProviderAttribute(DataTypes.RebarTypeKeyword);
                    return unionBuilder.CreateType();
                default:
                    throw new NotImplementedException($"SelfTypeMode: {selfTypeNode.Mode}");
            }
        }
    }
}
