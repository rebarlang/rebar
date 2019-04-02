using System.Xml.Linq;
using NationalInstruments.SourceModel;
using NationalInstruments.SystemModel;

namespace Rebar.RebarTarget.SystemModel
{
    /// <summary>
    /// Catalog Item for the Rebar Applications module within the Rebar device.
    /// </summary>
    /// <remarks>The Rebar module contains a Target process</remarks>
    [ExportItemToCatalog(typeof(ModuleCatalogItem), AccessModifier = CatalogAccessModifier.Unpublished)]
    [WithInternalsMetadata(typeof(TargetKind))]
    public class ModuleCatalogItem : ICatalogItemExport<ModuleKind>
    {
        private const string LocalName = ModuleLocalName;
        private const string TargetLocalName = "RebarTarget";

        /// <summary>
        /// The local name of this catalog item
        /// </summary>
        internal const string ModuleLocalName = "RebarModule";

        /// <summary>
        /// XName of this catalog item
        /// </summary>
        public static readonly XName Name = XName.Get(LocalName, SystemModelNamespaceSchema.ParsableNamespaceName);

        /// <inheritdoc/>
        public void Build(ICatalogItemBuilder builder)
        {
            IProcess target = BuildTargetProcess(builder);
            IProcess module = BuildModuleProcess(builder);
            IModuleHighLevelHelper moduleHelper = builder.GetHighLevelHelper<IModuleHighLevelHelper>();
            moduleHelper.AddInternalsProcess(module, target);
        }

        private static IProcess BuildModuleProcess(ICatalogItemBuilder builder)
        {
            var moduleHelper = builder.GetHighLevelHelper<IModuleHighLevelHelper>();
            var productIdentifier = new HardwareIdentifier(HardwareIdentifier.NiVendorName, "Rebar Module");
            var module = moduleHelper.CreateRoot(ModuleKind.Name, "Rebar Module", LocalName, productIdentifier);
            return module;
        }

        private static IProcess BuildTargetProcess(ICatalogItemBuilder builder)
        {
            IProcess targetProcess = builder.CreateOrphanedProcess<TargetKind>(TargetLocalName);
            builder.SetValue(targetProcess, PlatformSymbols.PlatformType, PlatformXName);

            builder.SetValue(targetProcess, Symbols.TopLevelCardinality, Cardinality.Many);
            string targetNameInTargetPickerDropDown = "Rebar Target";
            builder.SetValue(targetProcess, Symbols.AliasName, targetNameInTargetPickerDropDown);
            builder.SetValue(targetProcess, Symbols.Hidden, true); // This prevents the target from adding another software section
            return targetProcess;
        }

        public const string PlatformName = "RebarPlatform";

        public static readonly XName PlatformXName = SystemModelNamespaceSchema.XmlNamespace + PlatformName;
    }
}
