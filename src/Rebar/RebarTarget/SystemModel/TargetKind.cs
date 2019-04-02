using NationalInstruments.SourceModel;

namespace Rebar.RebarTarget.SystemModel
{
    /// <summary>
    /// Defines the kind for Rebar target.
    /// </summary>
    [ExportKind(typeof(TargetKind))]
    public class TargetKind : NationalInstruments.SystemModel.TargetKind
    {
        /// <summary>
        /// The fully-qualified name of this kind.
        /// </summary>
        public static new readonly KindName Name = KindName.Get("Process.RebarTarget", SystemModelNamespaceSchema.ParsableNamespaceName);
    }
}
