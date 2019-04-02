using System.ComponentModel.Composition;
using System.Reflection;
using NationalInstruments.DynamicProperties;
using NationalInstruments.SourceModel.Persistence;

namespace Rebar.SourceModel
{
    [ParsableNamespaceSchema(Function.ParsableNamespaceName, CurrentVersion)]
    [PartCreationPolicy(CreationPolicy.Shared)]
    public class NamespaceSchema : NationalInstruments.SourceModel.Persistence.NamespaceSchema
    {
        public const string CurrentVersion = "0.1.0f0";

        public NamespaceSchema()
            : base(Assembly.GetExecutingAssembly())
        {
            Version = VersionExtensions.Parse(CurrentVersion);
            OldestCompatibleVersion = VersionExtensions.Parse("0.1.0f0");
        }

        public override string FeatureSetName => "Rebar";

        public override string NamespaceName => Function.ParsableNamespaceName;
    }
}
