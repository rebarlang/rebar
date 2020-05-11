using System.Collections.Generic;
using System.Linq;
using NationalInstruments;
using NationalInstruments.CommonModel;
using NationalInstruments.Core;
using NationalInstruments.SourceModel;
using NationalInstruments.SourceModel.Persistence;
using NationalInstruments.VI.SourceModel;

namespace Rebar.SourceModel
{
    public abstract class MatchStructureBase : StackedStructure
    {
        /// <inheritdoc />
        protected override void Initialize(IElementCreateInfo info)
        {
            base.Initialize(info);
            if (info.ForParse)
            {
                // info.FixupRegistrar.AddPostParseFixupOrder(nameof(FixupCaseStructure), CaseDiagramPatternSerializer.CaseDiagramPatternSerializerPostParseFixupName);
                // info.FixupRegistrar.AddPostParseFixupOrder(DataTypeSerializer.DataTypeSerializerPostParseFixupName, CaseStructurePostParseFixupName);
                info.FixupRegistrar.RegisterPostParseFixup(this, FixupMatchStructureBase, nameof(FixupMatchStructureBase));
            }
        }

        /// <summary>
        /// This method is a post-parser fixup method for configuring <see cref="MatchStructureBase"/>s on this structure
        /// after the rest of the structure and its subcomponents is parsed.
        /// </summary>
        /// <remarks>This was copied from CaseStructure.cs in VI. This exists because the serializer for 
        /// StackedStructureTunnel terminals does not record things like the Role of each terminal, since they
        /// can be recovered from which diagram the terminal is associated with. This is the method that
        /// does that reassociation; it has to run after the tunnel and all associated Diagrams have been parsed.</remarks>
        private static void FixupMatchStructureBase(Element element, IElementServices services)
        {
            var matchStructure = (MatchStructureBase)element;
            // EnsureView will make sure all nested diagrams are sized consistently.  This shouldn't technically be necessary if everything was
            // persisted correctly.  However, there were some previous bugs where case diagram bounds weren't persisted correctly.  Also, we may
            // eventually decide not to persist bounds for nested diagrams and just calculate them from the owning structure.  For that to work,
            // we'll always need to call EnsureView.
            matchStructure.EnsureView(EnsureViewHints.Bounds);

            MatchStructureSelectorBase selector = matchStructure.Components.OfType<MatchStructureSelectorBase>().FirstOrDefault();
            if (selector != null)
            {
                var selectorOuterTerminal = selector.BorderNodeTerminals.First();
                selectorOuterTerminal.Primary = true;
                selectorOuterTerminal.Direction = Direction.Input;
                selectorOuterTerminal.Role = BorderNodeTerminalRole.Outer;
                selectorOuterTerminal.Hotspot = TerminalHotspots.Input1;
                matchStructure.AddTerminalAlias(selectorOuterTerminal);

                foreach (var pair in matchStructure.NestedDiagrams.Zip(selector.BorderNodeTerminals.Skip(1)))
                {
                    var selectorInnerTerminal = pair.Value;
                    selectorInnerTerminal.Primary = true;
                    selectorInnerTerminal.Direction = Direction.Output;
                    selectorInnerTerminal.Role = BorderNodeTerminalRole.Inner;
                    selectorInnerTerminal.Hotspot = TerminalHotspots.CreateOutputTerminalHotspot(TerminalSize.Small, selector.Width, 0u);
                    NestedDiagram nestedDiagram = pair.Key;
                    nestedDiagram.AddTerminalAlias(selectorInnerTerminal);
                }
            }

            List<MatchStructureTunnelBase> tunnels = matchStructure.BorderNodes.OfType<MatchStructureTunnelBase>().ToList();
            foreach (var tunnel in tunnels)
            {
                if (tunnel.Terminals.Count() == matchStructure.NestedDiagrams.Count() + 1)
                {
                    var outerTerm = tunnel.BorderNodeTerminals.First();
                    outerTerm.Primary = true;
                    outerTerm.Role = BorderNodeTerminalRole.Outer;
                    if (tunnel.TerminalIdentifiersMatchAliasParent)
                    {
                        matchStructure.AddTerminalAlias(outerTerm);
                    }

                    foreach (var terminalDiagramPair in tunnel.BorderNodeTerminals.Skip(1).Zip(matchStructure.NestedDiagrams))
                    {
                        BorderNodeTerminal innerTerminal = terminalDiagramPair.Key;
                        innerTerminal.Primary = true;
                        innerTerminal.Role = BorderNodeTerminalRole.Inner;
                        NestedDiagram diagram = terminalDiagramPair.Value;
                        diagram.AddTerminalAlias(innerTerminal);
                    }
                }
                else
                {
                    throw new InvalidParseException("Attempting to setup tunnels on MatchStructureTunnelBase ID=" + matchStructure.Identifier.ToParsableString() +
                                                    " at post-parse fixup, MatchStructureTunnelBase has malformed tunnel without enough terminals.");
                }

                // This correctly places the hotspots of the tunnels so that wires that are about to be connected to them
                // during parsing will have the accurate positions and not get diagonal wires
                tunnel.EnsureView(EnsureViewHints.Bounds);
            }
        }

        /// <inheritdoc />
        public override IBorderNodeGuide GetGuide(BorderNode borderNode)
        {
            var max = GetMaxXYForBorderNode(this, borderNode);
            RectangleSides sides = GetSidesForBorderNode(borderNode);
            var height = max.Y + borderNode.Height + OuterBorderThickness.Bottom;
            var width = max.X + borderNode.Width + OuterBorderThickness.Right;
            RectangleBorderNodeGuide guide = new RectangleBorderNodeGuide(
                new SMRect(0, 0, width, height),
                sides,
                BorderNodeDocking.None,
                OuterBorderThickness,
                GetAvoidRects(borderNode));
            guide.EdgeOverflow = StockDiagramGeometries.StandardTunnelOffsetForStructures;
            return guide;
        }

        protected abstract RectangleSides GetSidesForBorderNode(BorderNode borderNode);
    }
}
