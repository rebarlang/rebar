using NationalInstruments.FeatureToggles;

namespace Rebar
{
    [ExportFeatureToggles]
    [ExposeFeatureToggle(RebarFeatureCategory, CellDataTypeDescription, CellDataType)]
    [ExposeFeatureToggle(RebarFeatureCategory, OptionDataTypeDescription, OptionDataType)]
    [ExposeFeatureToggle(RebarFeatureCategory, RebarTargetDescription, RebarTarget)]
    public sealed class RebarFeatureToggles : FeatureTogglesProvider<RebarFeatureToggles>
    {
        private const string RebarFeatureCategory = "Rebar";
        private const string FeaturePrefix = "Rebar.FeatureToggles.";

        private const string CellDataTypeDescription = "Enable the Cell data type and related nodes";
        private const string OptionDataTypeDescription = "Enable the Option data type and related nodes";
        private const string RebarTargetDescription = "Enable the Rebar execution target";

        public const string CellDataType = FeaturePrefix + nameof(CellDataType);
        public const string OptionDataType = FeaturePrefix + nameof(OptionDataType);
        public const string RebarTarget = FeaturePrefix + nameof(RebarTarget);

        public static bool IsCellDataTypeEnabled => _cellDataType.IsEnabled;
        public static bool IsOptionDataTypeEnabled => _optionDataType.IsEnabled;
        public static bool IsRebarTargetEnabled => _rebarTarget.IsEnabled;

        private static readonly FeatureToggleValueCache _cellDataType = CreateFeatureToggleValueCache(CellDataType);
        private static readonly FeatureToggleValueCache _optionDataType = CreateFeatureToggleValueCache(OptionDataType);
        private static readonly FeatureToggleValueCache _rebarTarget = CreateFeatureToggleValueCache(RebarTarget);
    }
}
