using System.ComponentModel.Composition;
using System.Reflection;
using NationalInstruments.DynamicProperties;
using NationalInstruments.SourceModel.Persistence;

namespace RustyWires.SourceModel
{
    [ParsableNamespaceSchema(RustyWiresFunction.ParsableNamespaceName, CurrentVersion)]
    [PartCreationPolicy(CreationPolicy.Shared)]
    public class RustyWiresNamespaceSchema : NamespaceSchema
    {
        public const string CurrentVersion = "0.1.0f0";

        public RustyWiresNamespaceSchema()
            : base(Assembly.GetExecutingAssembly())
        {
            Version = VersionExtensions.Parse(CurrentVersion);
            OldestCompatibleVersion = VersionExtensions.Parse("0.1.0f0");
        }

        public override string FeatureSetName => "RustyWires";

        public override string NamespaceName => RustyWiresFunction.ParsableNamespaceName;
    }
}
