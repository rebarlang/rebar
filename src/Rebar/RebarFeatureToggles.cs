using NationalInstruments.Core;
using NationalInstruments.FeatureToggles;

namespace Rebar
{
    [ExportFeatureToggles]
    [ExposeUserVisibleFeatureToggle(typeof(RebarFeatureToggles), CellDataType, CodeReadiness.Release)]
    [ExposeUserVisibleFeatureToggle(typeof(RebarFeatureToggles), RebarTarget, CodeReadiness.Release)]
    [ExposeUserVisibleFeatureToggle(typeof(RebarFeatureToggles), OutputNode, CodeReadiness.Release)]
    [ExposeUserVisibleFeatureToggle(typeof(RebarFeatureToggles), VectorAndSliceTypes, CodeReadiness.Release)]
    [ExposeUserVisibleFeatureToggle(typeof(RebarFeatureToggles), VisualizeVariableIdentity, CodeReadiness.Release)]
    [ExposeUserVisibleFeatureToggle(typeof(RebarFeatureToggles), StringDataType, CodeReadiness.Release)]
    [ExposeUserVisibleFeatureToggle(typeof(RebarFeatureToggles), AllIntegerTypes, CodeReadiness.Release)]
    [ExposeUserVisibleFeatureToggle(typeof(RebarFeatureToggles), FileHandleDataType, CodeReadiness.Release)]
    [ExposeUserVisibleFeatureToggle(typeof(RebarFeatureToggles), OptionPatternStructure, CodeReadiness.Release)]
    [ExposeUserVisibleFeatureToggle(typeof(RebarFeatureToggles), ParametersAndCalls, CodeReadiness.Release)]
    [ExposeUserVisibleFeatureToggle(typeof(RebarFeatureToggles), NotifierType, CodeReadiness.Release)]
    [ExposeUserVisibleFeatureToggle(typeof(RebarFeatureToggles), Panics, CodeReadiness.Release)]
    public sealed class RebarFeatureToggles : FeatureTogglesProvider<RebarFeatureToggles>
    {
        private const string RebarFeatureCategory = "Rebar";
        private const string FeaturePrefix = "Rebar.FeatureToggles.";

        public const string CellDataType = FeaturePrefix + nameof(CellDataType);
        public const string RebarTarget = FeaturePrefix + nameof(RebarTarget);
        public const string OutputNode = FeaturePrefix + nameof(OutputNode);
        public const string VectorAndSliceTypes = FeaturePrefix + nameof(VectorAndSliceTypes);
        public const string VisualizeVariableIdentity = FeaturePrefix + nameof(VisualizeVariableIdentity);
        public const string StringDataType = FeaturePrefix + nameof(StringDataType);
        public const string AllIntegerTypes = FeaturePrefix + nameof(AllIntegerTypes);
        public const string FileHandleDataType = FeaturePrefix + nameof(FileHandleDataType);
        public const string OptionPatternStructure = FeaturePrefix + nameof(OptionPatternStructure);
        public const string ParametersAndCalls = FeaturePrefix + nameof(ParametersAndCalls);
        public const string NotifierType = FeaturePrefix + nameof(NotifierType);
        public const string Panics = FeaturePrefix + nameof(Panics);

        public static bool IsCellDataTypeEnabled => _cellDataType.IsEnabled;
        public static bool IsRebarTargetEnabled => _rebarTarget.IsEnabled;
        public static bool IsOutputNodeEnabled => _outputNode.IsEnabled;
        public static bool IsVectorAndSliceTypesEnabled => _vectorAndSliceTypes.IsEnabled;
        public static bool IsVisualizeVariableIdentityEnabled => _visualizeVariableIdentity.IsEnabled;
        public static bool IsStringDataTypeEnabled => _stringDataType.IsEnabled;
        public static bool IsAllIntegerTypesEnabled => _allIntegerTypes.IsEnabled;
        public static bool IsFileHandleDataTypeEnabled => _fileHandleDataType.IsEnabled;
        public static bool IsOptionPatternStructureEnabled => _optionPatternStructure.IsEnabled;
        public static bool IsParametersAndCallsEnabled => _parametersAndCalls.IsEnabled;
        public static bool IsNotifierTypeEnabled => _notifierType.IsEnabled;
        public static bool IsPanicsEnabled => _panics.IsEnabled;

        private static readonly FeatureToggleValueCache _cellDataType = CreateFeatureToggleValueCache(CellDataType);
        private static readonly FeatureToggleValueCache _rebarTarget = CreateFeatureToggleValueCache(RebarTarget);
        private static readonly FeatureToggleValueCache _outputNode = CreateFeatureToggleValueCache(OutputNode);
        private static readonly FeatureToggleValueCache _vectorAndSliceTypes = CreateFeatureToggleValueCache(VectorAndSliceTypes);
        private static readonly FeatureToggleValueCache _visualizeVariableIdentity = CreateFeatureToggleValueCache(VisualizeVariableIdentity);
        private static readonly FeatureToggleValueCache _stringDataType = CreateFeatureToggleValueCache(StringDataType);
        private static readonly FeatureToggleValueCache _allIntegerTypes = CreateFeatureToggleValueCache(AllIntegerTypes);
        private static readonly FeatureToggleValueCache _fileHandleDataType = CreateFeatureToggleValueCache(FileHandleDataType);
        private static readonly FeatureToggleValueCache _optionPatternStructure = CreateFeatureToggleValueCache(OptionPatternStructure);
        private static readonly FeatureToggleValueCache _parametersAndCalls = CreateFeatureToggleValueCache(ParametersAndCalls);
        private static readonly FeatureToggleValueCache _notifierType = CreateFeatureToggleValueCache(NotifierType);
        private static readonly FeatureToggleValueCache _panics = CreateFeatureToggleValueCache(Panics);
    }
}
