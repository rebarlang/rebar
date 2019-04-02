using NationalInstruments.SourceModel;
using NationalInstruments.SystemModel;

namespace Rebar.RebarTarget.SystemModel
{
    /// <summary>
    /// Defines the kind for Rebar functions.
    /// </summary>
    [ExportKind(typeof(FunctionKind))]
    public class FunctionKind : SoftwareKind
    {
        /// <summary>
        /// <see cref="Name"/> without a namespace
        /// </summary>
        private const string LocalName = "Process.RebarFunction";

        /// <summary>
        /// The fully-qualified name of this kind.
        /// </summary>
        public static new readonly KindName Name = KindName.Get(LocalName, SystemModelNamespaceSchema.ParsableNamespaceName);
    }
}
