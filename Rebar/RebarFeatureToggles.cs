using NationalInstruments.FeatureToggles;

namespace Rebar
{
    [ExportFeatureToggles]
    public sealed class RebarFeatureToggles : FeatureTogglesProvider<RebarFeatureToggles>
    {
        private const string RebarFeatureCategory = "Rebar";
        private const string FeaturePrefix = "Rebar.FeatureToggles.";
    }
}
