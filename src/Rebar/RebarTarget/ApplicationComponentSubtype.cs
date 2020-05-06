using System.Collections.Generic;
using System.ComponentModel.Composition;
using NationalInstruments.Compiler;
using NationalInstruments.Composition;
using NationalInstruments.Core;
using NationalInstruments.Dfir.Component;
using NationalInstruments.MocCommon.Components.Design;
using NationalInstruments.MocCommon.Components.SourceModel;
using NationalInstruments.Shell;
using NationalInstruments.SourceModel;
using NationalInstruments.SourceModel.Envoys;
using Rebar.SourceModel;
using Rebar.SourceModel.TypeDiagram;

namespace Rebar.RebarTarget
{
    /// <summary>
    /// Implementation of <see cref="IComponentSubtype"/> for the Rebar application component subtype.
    /// </summary>
    [ExportComponentSubtype(Identifier, ComponentType.Application)]
    [Export(typeof(ApplicationComponentSubtype))]
    [PartCreationPolicy(CreationPolicy.Shared)]
    [PartMetadata(ExportIdentifier.ExportIdentifierKey, "{C482A09D-9426-4919-955B-04CD09E04823}")]
    public sealed class ApplicationComponentSubtype : BuildableComponentSubtype
    {
        /// <summary>
        /// The persisted name of the source component subtype.
        /// </summary>
        public const string Identifier = "RebarApplication";

        /// <summary>
        /// <see cref="IComponentSubtypeSemanticProperties"/> configured to represent RebarApplicationComponentSubtype.
        /// </summary>
        private static readonly IComponentSubtypeSemanticProperties _semanticProperties = new ApplicationComponentSubtypeSemanticProperties();

        /// <summary>
        /// These are document types that are allowed under a Rebar application
        /// </summary>
        private static readonly List<BindingKeyword> _supportedFileTypes = new List<BindingKeyword>()
        {
            Function.FunctionDefinitionType,
            TypeDiagramDefinition.TypeDiagramDefinitionType,
        };

        #region ComponentSubtype Members

        /// <inheritdoc/>
        public override IComponentSubtypeSemanticProperties ComponentSubtypeSemanticProperties => _semanticProperties;

        /// <inheritdoc/>
        public override string XmlName => Identifier;

        /// <inheritdoc/>
        public override ComponentType ComponentType => ComponentType.Application;

        /// <inheritdoc/>
        public override IEnumerable<BindingKeyword> SupportedFileTypes => _supportedFileTypes;

        /// <inheritdoc/>
        public override IProvideComponentProperties CreateComponentSubtypeProperties()
        {
            return ApplicationComponentSubtypeProperties.Create(ElementCreateInfo.ForNew);
        }

        /// <inheritdoc/>
        public override void Initialize(ComponentConfiguration componentConfiguration)
        {
        }

        /// <inheritdoc/>
        public override void Cleanup(ComponentConfiguration componentConfiguration)
        {
        }

        /// <inheritdoc/>
        public override bool SupportsUserDefinedCompilerSymbols => true;

        /// <inheritdoc/>
        protected override void AddComponentSubtypeSpecificComponentSymbols(SymbolTable symbolTable, Envoy componentDefinitionEnvoy)
        {
        }

#endregion ComponentSubtype Members

#region IBuildableComponentSubtype Members

        /// <inheritdoc/>
        public override bool SupportsBuildMonitorApi => true;

        /// <inheritdoc/>
        public override string GetOutputTopLevelFilePath(ComponentConfiguration componentConfiguration)
        {
            return componentConfiguration.GetOutputDirectory();
        }

#endregion IBuildableComponentSubtype Members
    }
}
