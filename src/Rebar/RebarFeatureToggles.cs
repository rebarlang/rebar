using NationalInstruments.Core;
using NationalInstruments.FeatureToggles;

namespace Rebar
{
    [ExportFeatureToggles]
    [ExposeFeatureToggle(typeof(RebarFeatureToggles), CellDataType, CodeReadiness.Release)]
    [ExposeFeatureToggle(typeof(RebarFeatureToggles), OptionDataType, CodeReadiness.Release)]
    [ExposeFeatureToggle(typeof(RebarFeatureToggles), RebarTarget, CodeReadiness.Release)]
    [ExposeFeatureToggle(typeof(RebarFeatureToggles), OutputNode, CodeReadiness.Release)]
    [ExposeFeatureToggle(typeof(RebarFeatureToggles), VectorAndSliceTypes, CodeReadiness.Release)]
    [ExposeFeatureToggle(typeof(RebarFeatureToggles), VisualizeVariableIdentity, CodeReadiness.Release)]
    public sealed class RebarFeatureToggles : FeatureTogglesProvider<RebarFeatureToggles>
    {
        private const string RebarFeatureCategory = "Rebar";
        private const string FeaturePrefix = "Rebar.FeatureToggles.";

        public const string CellDataType = FeaturePrefix + nameof(CellDataType);
        public const string OptionDataType = FeaturePrefix + nameof(OptionDataType);
        public const string RebarTarget = FeaturePrefix + nameof(RebarTarget);
        public const string OutputNode = FeaturePrefix + nameof(OutputNode);
        public const string VectorAndSliceTypes = FeaturePrefix + nameof(VectorAndSliceTypes);
        public const string VisualizeVariableIdentity = FeaturePrefix + nameof(VisualizeVariableIdentity);

        public static bool IsCellDataTypeEnabled => _cellDataType.IsEnabled;
        public static bool IsOptionDataTypeEnabled => _optionDataType.IsEnabled;
        public static bool IsRebarTargetEnabled => _rebarTarget.IsEnabled;
        public static bool IsOutputNodeEnabled => _outputNode.IsEnabled;
        public static bool IsVectorAndSliceTypesEnabled => _vectorAndSliceTypes.IsEnabled;
        public static bool IsVisualizeVariableIdentityEnabled => _visualizeVariableIdentity.IsEnabled;

        private static readonly FeatureToggleValueCache _cellDataType = CreateFeatureToggleValueCache(CellDataType);
        private static readonly FeatureToggleValueCache _optionDataType = CreateFeatureToggleValueCache(OptionDataType);
        private static readonly FeatureToggleValueCache _rebarTarget = CreateFeatureToggleValueCache(RebarTarget);
        private static readonly FeatureToggleValueCache _outputNode = CreateFeatureToggleValueCache(OutputNode);
        private static readonly FeatureToggleValueCache _vectorAndSliceTypes = CreateFeatureToggleValueCache(VectorAndSliceTypes);
        private static readonly FeatureToggleValueCache _visualizeVariableIdentity = CreateFeatureToggleValueCache(VisualizeVariableIdentity);
    }
}
