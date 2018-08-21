using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using NationalInstruments;
using NationalInstruments.Core;
using NationalInstruments.DataTypes;
using NationalInstruments.SourceModel;
using RustyWires.SourceModel;

namespace RustyWires.Design
{
    [ExportTypeKeywordProvider("RustyWiresReference")]
    [PartCreationPolicy(CreationPolicy.Shared)]
    public class RustyWiresTypeKeywordProvider : ITypeKeywordProvider
    {
        public const string RustyWiresKeyword = "RustyWires";

        public IEnumerable<BindingKeyword> GetKeywords(NIType type)
        {
            if (type.IsImmutableReferenceType() || type.IsMutableReferenceType())
            {
                return new BindingKeyword(RustyWiresKeyword).ToEnumerable();
            }
            return null;
        }
    }

    [ExportTypeServiceProvider(typeof(ITypeAssetProvider))]
    [BindsToKeyword(RustyWiresTypeKeywordProvider.RustyWiresKeyword)]
    [BindsToKeyword(RustyWiresFunction.RustyWiresMocIdentifier)]
    [PartCreationPolicy(CreationPolicy.Shared)]
    public class ReferenceTypeServiceProvider : ITypeServiceProvider
    {
        [Import]
        public StockDiagramUIResources StockResources { get; set; }

        public QueryResult<T> QueryService<T>(NIType type) where T : class
        {
            if (typeof(T) == typeof(ITypeAssetProvider))
            {
                ITypeAssetProvider innerTypeAssetProvider;
                if (type.IsMutableReferenceType())
                {
                    NIType innerType = type.GetUnderlyingTypeFromRustyWiresType();
                    innerTypeAssetProvider = StockResources.GetTypeAssets(null, innerType);
                    int innerTypeDimensionality = StockDiagramUIResources.TypeToArraySize(innerType);
                    return new QueryResult<T>(new MutableReferenceTypeAssetProvider(innerTypeAssetProvider, innerTypeDimensionality) as T);
                }
                if (type.IsImmutableReferenceType())
                {
                    NIType innerType = type.GetUnderlyingTypeFromRustyWiresType();
                    innerTypeAssetProvider = StockResources.GetTypeAssets(null, innerType);
                    int innerTypeDimensionality = StockDiagramUIResources.TypeToArraySize(innerType);
                    return new QueryResult<T>(new ImmutableReferenceTypeAssetProvider(innerTypeAssetProvider, innerTypeDimensionality) as T);
                }
                if (type.IsMutableValueType())
                {
                    NIType innerType = type.GetUnderlyingTypeFromRustyWiresType();
                    innerTypeAssetProvider = StockResources.GetTypeAssets(null, innerType);
                    int innerTypeDimensionality = StockDiagramUIResources.TypeToArraySize(innerType);
                    return new QueryResult<T>(new MutableValueTypeAssetProvider(innerTypeAssetProvider, innerTypeDimensionality) as T);
                }
                if (type.IsImmutableValueType())
                {
                    NIType innerType = type.GetUnderlyingTypeFromRustyWiresType();
                    innerTypeAssetProvider = StockResources.GetTypeAssets(null, innerType);
                    int innerTypeDimensionality = StockDiagramUIResources.TypeToArraySize(innerType);
                    return new QueryResult<T>(new ImmutableValueTypeAssetProvider(innerTypeAssetProvider, innerTypeDimensionality) as T);
                }
                if (type.IsLockingCellType())
                {
                    return new QueryResult<T>(new CellTypeAssetProvider("Locking Cell") as T);
                }
                if (type.IsNonLockingCellType())
                {
                    return new QueryResult<T>(new CellTypeAssetProvider("Nonlocking Cell") as T);
                }
            }
            return new QueryResult<T>();
        }
    }

    internal class CellTypeAssetProvider : TypeAssetProvider
    {
        public CellTypeAssetProvider(string name)
            : base(
                typeof(PlatformFrameworkResourceKey),
                "Resources/Reference",
                StockTypeAssets.ReferenceAndPathTypeColor,
                name)
        {
        }
    }

    internal abstract class RustyWiresTypeAssetProvider : TypeAssetProvider
    {
        private readonly ITypeAssetProvider _innerTypeAssets;
        private readonly int _innerTypeDimensionality;

        protected RustyWiresTypeAssetProvider(ITypeAssetProvider innerTypeAssetProvider, int innerTypeDimensionality, string resourceKey, string displayName)
            : base(
                typeof(RustyWiresTypeAssetProvider),
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

    internal class MutableReferenceTypeAssetProvider : RustyWiresTypeAssetProvider
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

    internal class ImmutableReferenceTypeAssetProvider : RustyWiresTypeAssetProvider
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

    internal class MutableValueTypeAssetProvider : RustyWiresTypeAssetProvider
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

    internal class ImmutableValueTypeAssetProvider : RustyWiresTypeAssetProvider
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
