using System.ComponentModel.Composition;
using System.Reflection;
using System.Xml.Linq;
using NationalInstruments.DynamicProperties;
using NationalInstruments.SourceModel.Persistence;

namespace Rebar.RebarTarget.SystemModel
{
    /// <summary>
    /// Implements namespace versioning for elements in this assembly.
    /// </summary>
    [ParsableNamespaceSchema(ParsableNamespaceName, CurrentVersion)]
    [PartCreationPolicy(CreationPolicy.Shared)]
    public sealed class SystemModelNamespaceSchema : NamespaceSchema
    {
        /// <summary>
        /// The xml namespace
        /// </summary>
        public static readonly XNamespace XmlNamespace = XNamespace.Get(ParsableNamespaceName);

        /// <summary>
        /// The current version
        /// </summary>
        public const string CurrentVersion = "1.0.0f0";

        /// <summary>
        /// Default Constructor
        /// </summary>
        public SystemModelNamespaceSchema()
            : base(Assembly.GetExecutingAssembly())
        {
            Version = VersionExtensions.Parse(CurrentVersion);
            OldestCompatibleVersion = VersionExtensions.Parse("1.0.0d0");
        }

        /// <summary>
        /// Namespace name
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "Parsable", Justification = "It's spelled correctly")]
        public const string ParsableNamespaceName = "http://www.ni.com/SystemDesigner/Rebar/SystemModel";

        /// <inheritdoc/>
        public override string NamespaceName => ParsableNamespaceName;

        /// <inheritdoc/>
        public override string FeatureSetName => "Feature Set Name";
    }
}
