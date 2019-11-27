using System.Collections.Generic;
using NationalInstruments.Core;
using NationalInstruments.Dfir.Component;
using Rebar.RebarTarget.SystemModel;
using Rebar.SourceModel;
using Rebar.SourceModel.TypeDiagram;

namespace Rebar.RebarTarget
{
    /// <summary>
    /// <see cref="IComponentSubtypeSemanticProperties"/> implementation for Rebar Applications.
    /// </summary>
    public sealed class ApplicationComponentSubtypeSemanticProperties : ComponentSubtypeSemanticPropertiesBase
    {
        private static readonly IEnumerable<string> _supportedPlatforms = new[] { ModuleCatalogItem.PlatformName };

        /// <inheritdoc />
        public override string ComponentSubtypeDisplayName => "Rebar Application";

        /// <inheritdoc />
        public override ComponentCardinality TopLevelCardinality => ComponentCardinality.OneOrMore;

        /// <inheritdoc />
        public override ComponentCardinality ExportCardinality => ComponentCardinality.Zero;

        /// <inheritdoc />
        protected override IEnumerable<string> SupportedPlatformsWhitelist => _supportedPlatforms;

        /// <inheritdoc/>
        public override bool CanBeExported(BindingKeyword modelDefinitionType)
        {
            return false;
        }

        /// <inheritdoc/>
        public override bool CanBeTopLevel(BindingKeyword modelDefinitionType)
        {
            return modelDefinitionType.LocalName == Function.FunctionDefinitionType
                || modelDefinitionType.LocalName == TypeDiagramDefinition.TypeDiagramDefinitionType;
        }

        /// <inheritdoc/>
        public override bool CanBeAlwaysIncluded(BindingKeyword modelDefinitionType)
        {
            return true;
        }
    }
}
