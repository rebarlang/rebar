using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using NationalInstruments;
using NationalInstruments.Core;
using NationalInstruments.DataTypes;
using NationalInstruments.SourceModel;
using Rebar.Common;
using Rebar.SourceModel;

namespace Rebar.Design
{
    [ExportTypeKeywordProvider(DataTypes.RebarTypeKeyword)]
    [PartCreationPolicy(CreationPolicy.Shared)]
    public class ReferenceTypeKeywordProvider : ITypeKeywordProvider
    {
        public const string RebarKeyword = "Rebar";

        public IEnumerable<BindingKeyword> GetKeywords(NIType type)
        {
            if (type.IsImmutableReferenceType() || type.IsMutableReferenceType())
            {
                return new BindingKeyword(RebarKeyword).ToEnumerable();
            }
            return null;
        }
    }

    [ExportTypeServiceProvider(typeof(ITypeAssetProvider))]
    [BindsToKeyword(ReferenceTypeKeywordProvider.RebarKeyword)]
    [BindsToKeyword(Function.FunctionMocIdentifier)]
    [PartCreationPolicy(CreationPolicy.Shared)]
    public class ReferenceTypeServiceProvider : ITypeServiceProvider
    {
        [Import]
        public StockDiagramUIResources StockResources { get; set; }

        public QueryResult<T> QueryService<T>(NIType type) where T : class
        {
            if (typeof(T) == typeof(ITypeAssetProvider))
            {
                if (type.IsRebarReferenceType())
                {
                    NIType innerType = type.GetUnderlyingTypeFromRebarType();
                    ITypeAssetProvider innerTypeAssetProvider = StockResources.GetTypeAssets(null, innerType);
                    int innerTypeDimensionality = StockDiagramUIResources.TypeToArraySize(innerType);
                    ITypeAssetProvider outerTypeAssetProvider = type.IsMutableReferenceType()
                        ? (ITypeAssetProvider)new MutableReferenceTypeAssetProvider(innerTypeAssetProvider, innerTypeDimensionality)
                        : new ImmutableReferenceTypeAssetProvider(innerTypeAssetProvider, innerTypeDimensionality);
                    return new QueryResult<T>(outerTypeAssetProvider as T);
                }
                if (type.IsLockingCellType())
                {
                    return new QueryResult<T>(new GenericReferenceTypeAssetProvider("Locking Cell") as T);
                }
                if (type.IsNonLockingCellType())
                {
                    return new QueryResult<T>(new GenericReferenceTypeAssetProvider("Nonlocking Cell") as T);
                }
                if (type.IsIteratorType())
                {
                    return new QueryResult<T>(new GenericReferenceTypeAssetProvider("Iterator") as T);
                }
                if (type.IsVectorType())
                {
                    return new QueryResult<T>(new GenericReferenceTypeAssetProvider("Vector") as T);
                }
            }
            return new QueryResult<T>();
        }
    }

    internal class GenericReferenceTypeAssetProvider : NationalInstruments.SourceModel.TypeAssetProvider
    {
        public GenericReferenceTypeAssetProvider(string name)
            : base(
                typeof(PlatformFrameworkResourceKey),
                "Resources/Reference",
                StockTypeAssets.ReferenceAndPathTypeColor,
                name)
        {
        }
    }

    internal class VariableIdentityTypeAssetProvider : NationalInstruments.SourceModel.TypeAssetProvider
    {
        public static PlatformColor[] Colors = new[]
        {
            PlatformColor.FromArgb(0xFF, 0x80, 0x00, 0x00),
            PlatformColor.FromArgb(0xFF, 0x9A, 0x63, 0x24),
            PlatformColor.FromArgb(0xFF, 0x80, 0x80, 0x00),
            PlatformColor.FromArgb(0xFF, 0x46, 0x99, 0x90),
            PlatformColor.FromArgb(0xFF, 0x00, 0x00, 0x75),
            PlatformColor.FromArgb(0xFF, 0x00, 0x00, 0x00),
            PlatformColor.FromArgb(0xFF, 0xE6, 0x19, 0x4B),
            PlatformColor.FromArgb(0xFF, 0xF5, 0x82, 0x31),
            PlatformColor.FromArgb(0xFF, 0xFF, 0xE1, 0x19),
            PlatformColor.FromArgb(0xFF, 0xBF, 0xEF, 0x45),
            PlatformColor.FromArgb(0xFF, 0x3C, 0xB4, 0x4B),
            PlatformColor.FromArgb(0xFF, 0x42, 0xD4, 0xF4),
            PlatformColor.FromArgb(0xFF, 0x43, 0x63, 0xD8),
            PlatformColor.FromArgb(0xFF, 0x91, 0x1E, 0xB4),
            PlatformColor.FromArgb(0xFF, 0xF0, 0x32, 0xE6),
            PlatformColor.FromArgb(0xFF, 0xA9, 0xA9, 0xA9),
        };

        public static PlatformColor GetColor(int id)
        {
            return Colors[id % Colors.Length];
        }

        private readonly PlatformColor _variableIdentityColor;

        public VariableIdentityTypeAssetProvider(string name, PlatformColor variableIdentityColor)
            : base(
                typeof(PlatformFrameworkResourceKey),
                "Resources/Reference",
                variableIdentityColor,
                name)
        {
            _variableIdentityColor = variableIdentityColor;
        }

        public override WireRenderInfoEnumerable GetWireRenderInfo(int dimensionality)
        {
            var baseRenderInfo = base.GetWireRenderInfo(dimensionality).Items.First();
            var brush = PlatformBrush.CreateSolidColorBrush(_variableIdentityColor);
            baseRenderInfo.HorizontalBrush = brush;
            baseRenderInfo.VerticalBrush = brush;
            return new WireRenderInfoEnumerable()
            {
                Items = baseRenderInfo.ToEnumerable()
            };
        }
    }

    internal abstract class TypeAssetProvider : NationalInstruments.SourceModel.TypeAssetProvider
    {
        private readonly ITypeAssetProvider _innerTypeAssets;
        private readonly int _innerTypeDimensionality;

        protected TypeAssetProvider(ITypeAssetProvider innerTypeAssetProvider, int innerTypeDimensionality, string resourceKey, string displayName)
            : base(
                typeof(TypeAssetProvider),
                resourceKey,
                innerTypeAssetProvider.SolidColorAsColor,
                displayName)
        {
            _innerTypeAssets = innerTypeAssetProvider;
            _innerTypeDimensionality = innerTypeDimensionality;
        }

        public override WireRenderInfoEnumerable GetWireRenderInfo(int dimensionality)
        {
            WireRenderInfoEnumerable innerData = _innerTypeAssets.GetWireRenderInfo(dimensionality);
            WireRenderInfoEnumerable myData = base.GetWireRenderInfo(dimensionality);
            return new WireRenderInfoEnumerable()
            {
                Items = myData.Items.Concat(innerData.Items)
            };
        }

        protected override SMSize WireRenderSize(SMSize defaultSize, StrokeOrientation orientation)
        {
            WireRenderInfoEnumerable innerData = _innerTypeAssets.GetWireRenderInfo(_innerTypeDimensionality);
            double thickness = innerData.Items.First().Thickness;
            return orientation == StrokeOrientation.Horizontal
                ? new SMSize(defaultSize.Width, defaultSize.Height + thickness)
                : new SMSize(defaultSize.Width + thickness, defaultSize.Height);
        }
    }

    internal class MutableReferenceTypeAssetProvider : TypeAssetProvider
    {
        public MutableReferenceTypeAssetProvider(ITypeAssetProvider innerTypeAssets, int innerTypeDimensionality)
            : base(
                innerTypeAssets,
                innerTypeDimensionality,
                "Resources/MutableReference",
                "Mutable Reference")
        {
        }
    }

    internal class ImmutableReferenceTypeAssetProvider : TypeAssetProvider
    {
        public ImmutableReferenceTypeAssetProvider(ITypeAssetProvider innerTypeAssets, int innerTypeDimensionality)
            : base(
                innerTypeAssets,
                innerTypeDimensionality,
                "Resources/ImmutableReference",
                "Immutable Reference")
        {
        }
    }

    internal class MutableValueTypeAssetProvider : TypeAssetProvider
    {
        public MutableValueTypeAssetProvider(ITypeAssetProvider innerTypeAssets, int innerTypeDimensionality)
            : base(
                innerTypeAssets,
                innerTypeDimensionality,
                "Resources/MutableValue",
                "Mutable Value")
        {
        }
    }

    internal class ImmutableValueTypeAssetProvider : TypeAssetProvider
    {
        public ImmutableValueTypeAssetProvider(ITypeAssetProvider innerTypeAssets, int innerTypeDimensionality)
            : base(
                innerTypeAssets,
                innerTypeDimensionality,
                "Resources/ImmutableValue",
                "Immutable Value")
        {
        }
    }
}
