using System.Xml.Linq;
using NationalInstruments.MocCommon.Components.SourceModel;
using NationalInstruments.SourceModel;
using NationalInstruments.SourceModel.Persistence;
using Rebar.SourceModel;

namespace Rebar.RebarTarget
{
    /// <summary>
    /// Implements the <see cref="NationalInstruments.ComponentEditor.SourceModel.IProvideComponentProperties"/> interface for <see cref="ApplicationComponentSubtype"/>.
    /// </summary>
    internal class ApplicationComponentSubtypeProperties : Content, IProvideComponentProperties
    {
        private const string XmlElementNameString = "ApplicationOutputTypeProperties";

        private ApplicationComponentSubtypeProperties()
        {
        }

        /// <inheritdoc/>
        public bool IsPersisted => true;

        /// <inheritdoc/>
        public override XName XmlElementName => XName.Get(XmlElementNameString, Function.ParsableNamespaceName);

        /// <summary>
        /// Factory method for creating Rebar application component properties
        /// </summary>
        /// <param name="createInfo">the <see cref="IElementCreateInfo"/> for creating this element</param>
        /// <returns>A new <see cref="ApplicationComponentSubtypeProperties"/></returns>
        [XmlParserFactoryMethod(XmlElementNameString, Function.ParsableNamespaceName)]
        public static ApplicationComponentSubtypeProperties Create(IElementCreateInfo createInfo)
        {
            var applicationProperties = new ApplicationComponentSubtypeProperties();
            applicationProperties.Initialize(createInfo);
            return applicationProperties;
        }
    }
}
