using NationalInstruments.ComponentEditor.SourceModel;
using NationalInstruments.Core;
using NationalInstruments.ExternalCode.SourceModel;
using NationalInstruments.SourceModel.Envoys;
using Rebar.SourceModel;
using Rebar.SourceModel.TypeDiagram;

namespace Rebar.RebarTarget.SystemModel
{
    /// <summary>
    /// Implementation of <see cref="ICreateItemsInScopeRules"/> for the Rebar target. This is necessary
    /// to allow Rebar functions and other documents to be added to components under the Rebar target.
    /// </summary>
    internal class CreateItemInScopeRulesService : ICreateItemsInScopeRules
    {
        /// <inheritdoc />
        public bool DisallowEditDocumentType(BindingKeyword modelDefinitionType, BindingKeyword overridingModelDefinitionType)
        {
            return !CanHandleType(modelDefinitionType, overridingModelDefinitionType);
        }

        /// <summary>
        /// Determines if the Rebar target supports a given model definition type.
        /// </summary>
        /// <param name="modelDefinitionType">The model type.</param>
        /// <param name="overridingModelDefinitionType">The overriding model type.</param>
        /// <returns>True if it's a supported type, otherwise false.</returns>
        private static bool CanHandleType(BindingKeyword modelDefinitionType, BindingKeyword overridingModelDefinitionType)
        {
            modelDefinitionType = overridingModelDefinitionType.IsEmpty() ? modelDefinitionType : overridingModelDefinitionType;

#if FALSE
            if (CodeReadinessSupport.AllowReadiness(CodeReadiness.Incomplete))
            {
                if (modelDefinitionType == ExternalCode.SourceModel.SharedLibraryDefinition.ModelDefinitionTypeKeyword)
                {
                    return true;
                }
            }
#endif

            return modelDefinitionType == Function.FunctionDefinitionType
                || modelDefinitionType == TypeDiagramDefinition.TypeDiagramDefinitionType
                || modelDefinitionType == SharedLibraryDefinition.ModelDefinitionTypeKeyword
                || modelDefinitionType == ComponentDefinition.ModelDefinitionTypeKeyword
                // || modelDefinitionType == GTypeDefinition.ModelDefinitionTypeString
                // || modelDefinitionType == PaletteDocumentType.PaletteDocumentTypeString
                || modelDefinitionType == string.Empty; // Model definition type is empty when adding an item to a component under a target
        }
    }
}
