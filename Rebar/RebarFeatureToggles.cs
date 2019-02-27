using NationalInstruments.FeatureToggles;

namespace Rebar
{
    [ExportFeatureToggles]
    [ExposeFeatureToggle(RebarFeatureCategory, CellDataTypeDescription, CellDataType)]
    [ExposeFeatureToggle(RebarFeatureCategory, OptionDataTypeDescription, OptionDataType)]
    public sealed class RebarFeatureToggles : FeatureTogglesProvider<RebarFeatureToggles>
    {
        private const string RebarFeatureCategory = "Rebar";
        private const string FeaturePrefix = "Rebar.FeatureToggles.";

        private const string CellDataTypeDescription = "Enable the Cell data type and related nodes";
        private const string OptionDataTypeDescription = "Enable the Option data type and related nodes";

        public const string CellDataType = FeaturePrefix + nameof(CellDataType);
        public const string OptionDataType = FeaturePrefix + nameof(OptionDataType);

        public static bool IsCellDataTypeEnabled => _cellDataType.IsEnabled;
        public static bool IsOptionDataTypeEnabled => _optionDataType.IsEnabled;

        private static readonly FeatureToggleValueCache _cellDataType = CreateFeatureToggleValueCache(CellDataType);
        private static readonly FeatureToggleValueCache _optionDataType = CreateFeatureToggleValueCache(OptionDataType);
    }
}
