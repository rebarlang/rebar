using NationalInstruments.Core;
using NationalInstruments.Design;
using NationalInstruments.Shell;

namespace Rebar.Design.TypeDiagram
{
    /// <summary>
    /// <see cref="DocumentEditControlInfo"/> implementation for <see cref="TypeDiagramEditor"/>.
    /// </summary>
    public class TypeDiagramEditorInfo : DocumentEditControlInfo<TypeDiagramEditor>
    {
        /// <summary>
        /// The clipboard format accepted by the type diagram
        /// </summary>
        private static readonly string TypeDiagramClipboardDataFormat = ClipboardFormatHelper.RegisterClipboardFormat(DragDrop.NIDataFormatPrefix + TypeDiagramPaletteLoader.DiagramPaletteIdentifier, "TypeDiagram");

        public TypeDiagramEditorInfo(string uniqueId, TypeDiagramDocument document)
            : base(uniqueId, document, document.TypeDiagramDefinition.Diagram, "editor", TypeDiagramPaletteLoader.DiagramPaletteIdentifier, string.Empty, string.Empty)
        {
            ClipboardDataFormat = TypeDiagramClipboardDataFormat;
        }
    }
}
