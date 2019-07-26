using NationalInstruments.Core;
using NationalInstruments.Design;
using NationalInstruments.Shell;

namespace Rebar.Design
{
    /// <summary>
    /// <see cref="DocumentEditControlInfo"/> implementation for <see cref="FunctionDiagramEditor"/>.
    /// </summary>
    public class FunctionDiagramEditorInfo : DocumentEditControlInfo<FunctionDiagramEditor>
    {
        /// <summary>
        /// The clipboard format accepted by this sketch diagram
        /// </summary>
        private static readonly string FunctionClipboardDataFormat = ClipboardFormatHelper.RegisterClipboardFormat(DragDrop.NIDataFormatPrefix + FunctionDiagramPaletteLoader.DiagramPaletteIdentifier, "RebarFunctionDiagram");

        public FunctionDiagramEditorInfo(string uniqueId, FunctionDocument document)
            : base(uniqueId, document, document.Function.Diagram, "editor", FunctionDiagramPaletteLoader.DiagramPaletteIdentifier, string.Empty, string.Empty)
        {
            ClipboardDataFormat = FunctionClipboardDataFormat;
        }
    }
}
