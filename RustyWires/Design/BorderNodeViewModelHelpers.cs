using System;
using System.Linq;
using NationalInstruments.Core;
using NationalInstruments.Design;
using NationalInstruments.SourceModel;

namespace RustyWires.Design
{
    internal static class BorderNodeViewModelHelpers
    {
        public static void FindBorderNodePositions(StructureViewModel structureViewModel, out SMRect leftRect, out SMRect rightRect)
        {
            var view = structureViewModel.View;
            double top = 0;
#if FALSE
            var contextMenuInfo = parameter.QueryService<ContextMenuInfo>().FirstOrDefault();
            if (contextMenuInfo != null && !view.IsEmpty)
            {
                top = contextMenuInfo.ClickPosition.GetPosition(view).Y;
            }
#endif

            Structure model = (Structure)structureViewModel.Model;
            top -= (top - model.OuterBorderThickness.Top) % StockDiagramGeometries.GridSize;
            top = Math.Max(top, model.OuterBorderThickness.Top);
            top = Math.Min(top, model.Height - model.OuterBorderThickness.Bottom - StockDiagramGeometries.StandardTunnelHeight);
            SMRect l = new SMRect(-StockDiagramGeometries.StandardTunnelOffsetForStructures, top, StockDiagramGeometries.StandardTunnelWidth,
                StockDiagramGeometries.StandardTunnelHeight);
            SMRect r = new SMRect(model.Width - StockDiagramGeometries.StandardTunnelWidth + StockDiagramGeometries.StandardTunnelOffsetForStructures, top,
                StockDiagramGeometries.StandardTerminalWidth, StockDiagramGeometries.StandardTunnelHeight);
            while (
                model.BorderNodes.Any(
                    node => node.Bounds.Overlaps(l) || node.Bounds.Overlaps(r)))
            {
                l.Y += StockDiagramGeometries.GridSize;
                r.Y += StockDiagramGeometries.GridSize;
            }
            // If we ran out of room looking for a place to put Shift Register, we need to grow our Loop
            if (l.Bottom > model.Height - model.OuterBorderThickness.Bottom)
            {
                model.Height = l.Bottom + StockDiagramGeometries.StandardTunnelHeight;
            }
            leftRect = l;
            rightRect = r;
        }
    }
}
