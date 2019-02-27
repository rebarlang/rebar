using System.Xml.Linq;
using NationalInstruments.SourceModel;
using NationalInstruments.SystemModel;
using Rebar.SourceModel;

namespace Rebar.RebarTarget.SystemModel
{
    /// <summary>
    /// Rebar function Catalog Item. This controls the appearance and properties of Rebar functions when on the system designer.
    /// </summary>
    [ExportItemToCatalog(typeof(FunctionCatalogItem), AccessModifier = CatalogAccessModifier.Unpublished)]
    [WithSoftwareMetadata(Function.FunctionDefinitionType, "Function.rfn", alwaysCreateProcess: false)]
    public class FunctionCatalogItem : ICatalogItemExport<FunctionKind>
    {
        /// <summary>
        /// <see cref="Name"/> without a namespace
        /// </summary>
        private const string LocalName = "RebarFunctionCatalogItem";

        /// <summary>
        /// The unique identifier of this item in the catalog
        /// </summary>
        public static readonly XName Name = XName.Get(LocalName, SystemModelNamespaceSchema.ParsableNamespaceName);

        /// <inheritdoc/>
        public void Build(ICatalogItemBuilder builder)
        {
            ICatalogTree tree = builder.CreateCatalogItem(LocalName);
            IProcess functionProcess = builder.CreateRootProcess<FunctionKind>(tree, tree.Name);
            builder.SetValue(functionProcess, Symbols.DocumentType, Function.FunctionDefinitionType);
            builder.SetValue(functionProcess, Symbols.TopLevel, true);
        }
    }
}
