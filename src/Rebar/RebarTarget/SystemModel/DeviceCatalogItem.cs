using System.Xml.Linq;
using NationalInstruments.SourceModel;
using NationalInstruments.SystemModel;

namespace Rebar.RebarTarget.SystemModel
{
    /// <summary>
    /// Catalog Item for the top level Rebar device. This controls the structure and visual appearance of the Rebar system diagram target.
    /// </summary>
    /// <remarks>The top level Rebar device contains a Rebar module, represented by <see cref="ModuleCatalogItem"/></remarks>
    [ExportItemToCatalog(typeof(DeviceCatalogItem))]
    public class DeviceCatalogItem : ICatalogItemExport<DeviceKind>
    {
        private const string LocalName = DeviceLocalName;
        private const string DeviceLocalName = "RebarDevice";

        /// <summary>
        /// XName of this catalog item
        /// </summary>
        public static readonly XName Name = XName.Get(LocalName, SystemModelNamespaceSchema.ParsableNamespaceName);

        /// <inheritdoc/>
        public void Build(ICatalogItemBuilder builder)
        {
            var device = BuildDeviceProcess(builder);
            var module = (IProcess)builder.InstantiateCatalogItem(ModuleCatalogItem.Name, ModuleCatalogItem.ModuleLocalName);
            builder.SetValue(module, Symbols.AliasName, "Rebar Device");
            builder.SetValue(module, Symbols.ModelName, "Rebar Device");
            builder.AddSubprocess(device, module);
        }

        private static IProcess BuildDeviceProcess(ICatalogItemBuilder builder)
        {
            var deviceHelper = builder.GetHighLevelHelper<IDeviceHighLevelHelper>();
            var device = deviceHelper.CreateRoot(DeviceKind.Name, DeviceLocalName, DeviceLocalName, new HardwareIdentifier(HardwareIdentifier.NiVendorName, DeviceLocalName));
            builder.SetValue(device, Symbols.AliasName, "Rebar Device Target");
            return device;
        }
    }
}
