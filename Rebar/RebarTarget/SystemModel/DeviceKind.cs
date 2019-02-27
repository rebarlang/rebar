using NationalInstruments.SourceModel;

namespace Rebar.RebarTarget.SystemModel
{
    /// <summary>
    /// Defines the kind for the Rebar device.
    /// </summary>
    [ExportKind(typeof(DeviceKind))]
    public class DeviceKind : NationalInstruments.SystemModel.DeviceKind
    {
        /// <summary>
        /// The fully-qualified name of this kind.
        /// </summary>
        public static new readonly KindName Name = KindName.Get("Process.RebarDevice", SystemModelNamespaceSchema.ParsableNamespaceName);
    }
}
