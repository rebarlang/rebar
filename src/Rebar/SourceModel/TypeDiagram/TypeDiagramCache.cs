using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using NationalInstruments.DataTypes;
using NationalInstruments.DynamicProperties;
using NationalInstruments.SourceModel;
using NationalInstruments.SourceModel.Persistence;

namespace Rebar.SourceModel.TypeDiagram
{
    internal class TypeDiagramCache : BasicModelCache, IDataTypeReferenceOwner
    {
        private const string ElementName = "TypeDiagramCache";

        public const string DataTypePropertyName = "TypeDiagramDataType";

        /// <summary>
        /// DataType Property Symbol
        /// </summary>
        public static readonly PropertySymbol DataTypePropertySymbol =
            ExposeStaticProperty<TypeDiagramCache>(
                nameof(DataType),
                obj => obj.DataType,
                (obj, value) => { obj.DataType = (NIType)value; },
                PropertySerializers.DataTypeSerializerDeferringDataTypeReferenceTableToParseComplete,
                NIType.Unset);

        [XmlParserFactoryMethod(ElementName, Function.ParsableNamespaceName)]
        public static TypeDiagramCache CreateTypeDiagramCache(IElementCreateInfo elementCreateInfo)
        {
            var typeDiagramCache = new TypeDiagramCache();
            typeDiagramCache.Initialize(elementCreateInfo);
            return typeDiagramCache;
        }

        public override XName XmlElementName => XName.Get(ElementName, Function.ParsableNamespaceName);

        public NIType DataType { get; private set; }

        protected override void InitializeFromModel(Element model)
        {
            // Adapted from TypeDefinitionCache
            var typeDefinition = (TypeDiagramDefinition)model;

            DataType = typeDefinition.DataType;

            // PopulateCacheDependenciesFromLoadedModel(model);

            IsDirty = false;
            IsValid = true;
        }

        internal IEnumerable<string> OnModelEdits(TransactionEventArgs args)
        {
            // Adapted from TypeDefinitionCache
            var exportedPropertyChanges = new HashSet<string>();

            // If the ReferenceElement is null, it's likely because this is the transaction that closed the loaded file.
            if (AssociatedEnvoy.ReferenceElement != null && AssociatedEnvoy.ReferenceElement.TransactionManager.IsDirty)
            {
                IsDirty = true;
            }

            bool updateType = args.Transactions.Any(IsDataTypeChange);
            if (updateType)
            {
                NIType newDataType = ((TypeDiagramDefinition)AssociatedEnvoy.ReferenceDefinition).DataType;
                UpdateDataType(newDataType);
                exportedPropertyChanges.Add(DataTypePropertyName);
            }

            return exportedPropertyChanges;
        }

        private void UpdateDataType(NIType newDataType)
        {
            var typeDefinition = (TypeDiagramDefinition)AssociatedEnvoy.ReferenceDefinition;
            var oldType = DataType;
            if (oldType != newDataType)
            {
                using (var transaction = TransactionManager.BeginTransaction("data type property change.", TransactionPurpose.NonUser))
                {
                    DataType = newDataType;
                    transaction.Commit();
                }
            }
        }

        private static bool IsDataTypeChange(TransactionItem transaction)
        {
            // In addition to checking DataTypePropertyName, check if deferring exports changed notifications has been enabled or disabled
            return transaction.TargetElement is TypeDiagramDefinition &&
                   transaction.PropertyName == TypeDiagramDefinition.DataTypePropertyName;
        }

        public NIType GetOwnedDataType(PropertySymbol symbol)
        {
            return DataType;
        }

        public void SetOwnedDataType(NIType dataType, PropertySymbol symbol)
        {
            DataType = dataType;
        }
    }
}
