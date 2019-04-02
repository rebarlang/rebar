using NationalInstruments.SourceModel;

namespace Rebar.RebarTarget.SystemModel
{
    /// <summary>
    /// Defines the kind for the Rebar module
    /// </summary>
    [ExportKind(typeof(ModuleKind))]
    public class ModuleKind : NationalInstruments.SystemModel.ModuleKind
    {
        /// <summary>
        /// The fully-qualified name of this kind.
        /// </summary>
        public static new readonly KindName Name = KindName.Get("Process.RebarModule", SystemModelNamespaceSchema.ParsableNamespaceName);
    }
}
