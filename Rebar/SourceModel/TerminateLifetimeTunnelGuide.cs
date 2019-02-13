using System;
using System.Collections.Generic;
using System.Linq;
using NationalInstruments.Core;
using NationalInstruments.SourceModel;

namespace Rebar.SourceModel
{
    /// <summary>
    /// <see cref="IBorderNodeGuide"/> for TerminateLifetimeTunnels.
    /// </summary>
    internal sealed class TerminateLifetimeTunnelGuide : IBorderNodeGuide
    {
        private readonly SMRect _rect;
        private readonly LoopTerminateLifetimeTunnel _terminateLifetimeTunnel; // TODO: need a common type for TerminateLifetimeTunnels
        private readonly IEnumerable<SMRect> _avoidRects;

        private SMThickness StructureThickness => AdornedStructure.OuterBorderThickness;

        private Structure AdornedStructure => _terminateLifetimeTunnel.Structure;

        private Tunnel PairedBeginLifetimeTunnel => (Tunnel)_terminateLifetimeTunnel.BeginLifetimeTunnel;

        /// <summary>
        /// How much should the BorderNodeGeometry be "pushed" over the edge of the guide.
        /// Since shift registers can be only on left/right, only in X direction
        /// </summary>
        public float EdgeOverflow { get; set; }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "r", Justification = "TODO")]
        public TerminateLifetimeTunnelGuide(SMRect r, LoopTerminateLifetimeTunnel rightRegister, SMThickness border, IEnumerable<SMRect> avoidRects)
        {
            // go ahead and coerce rect, because we can't go over borders.
            _rect = r;
            _rect.Y += border.Top;
            _rect.Height = Math.Max(_rect.Height - border.Top - border.Bottom, 0);

            _terminateLifetimeTunnel = rightRegister;

            _avoidRects = avoidRects ?? new List<SMRect>();
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1725:ParameterNamesShouldMatchBaseDeclaration", MessageId = "0#", Justification = "TODO")]
        public BorderNodeGeometry ConstrainBounds(BorderNodeGeometry geom, RectDifference oldMinusNew)
        {
            // ShiftRegisters cannot change docking, so first force the geometry to dock, this coerces crazy X values
            SMRect coercedInput = EnforceDocking(geom);

            // Now we are going to iterate up and down the side of the structure, looking for a location in which
            // both the left and the right registers are clear. We start with the right being the coerced input
            // and the left being the actual left shift register's bounds _except_ it's Y, which we want to move in sync with it's pair
            SMRect rightBounds = coercedInput;
            SMRect leftBounds = PairedBeginLifetimeTunnel.Bounds;
            leftBounds.Y = rightBounds.Y;

            // Before we start scanning, make sure our current location doesn't just work, if it does we're done
            if (IsGood(leftBounds, rightBounds))
            {
                return new BorderNodeGeometry(rightBounds, BorderNodeDocking.Right);
            }

            const float DistanceToIterateBy = StockDiagramGeometries.GridSize;
            SMRect rightSideUpIteration = rightBounds;
            rightSideUpIteration.Y -= DistanceToIterateBy;

            SMRect leftSideUpIteration = leftBounds;
            leftSideUpIteration.Y -= DistanceToIterateBy;

            SMRect rightSideDownIteration = rightBounds;
            rightSideDownIteration.Y += DistanceToIterateBy;

            SMRect leftSideDownIteration = leftBounds;
            leftSideDownIteration.Y += DistanceToIterateBy;

            bool favorUp = oldMinusNew.Y > 0 || oldMinusNew.X > 0;
            bool favorDown = oldMinusNew.Y < 0 || oldMinusNew.X < 0;
            bool favorNeither = !favorUp && !favorDown;
            // prevent overlap. Starting from our bounds calculation, move outward to find closest. Make sure we don't leave structure.
            while (true)
            {
                if (favorUp || favorNeither)
                {
                    if (IsGood(leftSideUpIteration, rightSideUpIteration))
                    {
                        return new BorderNodeGeometry(rightSideUpIteration, BorderNodeDocking.Right);
                    }
                    if (!IsInBounds(leftSideUpIteration, rightSideUpIteration))
                    {
                        favorUp = false;
                    }
                    rightSideUpIteration.Y -= DistanceToIterateBy;
                    leftSideUpIteration.Y -= DistanceToIterateBy;
                }
                if (favorDown || favorNeither)
                {
                    if (IsGood(leftSideDownIteration, rightSideDownIteration))
                    {
                        return new BorderNodeGeometry(rightSideDownIteration, BorderNodeDocking.Right);
                    }
                    if (!IsInBounds(leftSideDownIteration, rightSideDownIteration))
                    {
                        favorDown = false;
                    }
                    rightSideDownIteration.Y += DistanceToIterateBy;
                    leftSideDownIteration.Y += DistanceToIterateBy;
                }

                if (favorNeither && !IsInBounds(leftSideUpIteration, rightSideUpIteration) && !IsInBounds(leftSideDownIteration, rightSideDownIteration))
                {
                    // Give up, neither is within bounds.
                    break;
                }

                favorNeither = !favorUp && !favorDown;
            }
            return new BorderNodeGeometry(rightBounds, BorderNodeDocking.Right);
        }

        private bool IsInBounds(SMRect leftRect, SMRect rightRect)
        {
            // To handle collision, we need to fake out our rect size
            // to add the EdgeOverflow, else contains will fail and we'll land
            // on top of other avoid zones
            SMRect leftAdjustedRect = _rect;
            SMRect rightAdjustedRect = _rect;
            rightAdjustedRect.X -= EdgeOverflow;
            rightAdjustedRect.Width += EdgeOverflow * 2;

            IBorderNodeGuide guide = AdornedStructure.GetGuide(PairedBeginLifetimeTunnel);
            leftAdjustedRect.X -= guide.EdgeOverflow;
            leftAdjustedRect.Width += guide.EdgeOverflow * 2;
            return leftAdjustedRect.Contains(leftRect) && rightAdjustedRect.Contains(rightRect);
        }

        private bool IsGood(SMRect leftRect, SMRect rightRect)
        {
            if (!IsInBounds(leftRect, rightRect))
            {
                return false;
            }
            else if (_avoidRects.Any(r => r.Overlaps(rightRect) || ((PairedBeginLifetimeTunnel.Owner != null) && r.Overlaps(leftRect))))
            {
                // We need to check PairedLeftShiftRegister.Owner above because when you convert a tunnel to a right shift register, the
                // paired left shift register gets added then removed immediately from the structure.  The final left shift register doesn't
                // actually get created/added until the user left clicks on an existing tunnel to replace it or somewhere else to create a
                // new left shift register.  However, if we don't exclude the non-parented left shift register in our calculation, we might
                // end up moving the right shift register when we don't need to.  We don't need to worry about doing the same thing for the
                // left shift register because movement of both shift registers is tied to the right shift register.
                return false;
            }
            return true;
        }

        /// <summary>
        ///  Make sure it keeps its docking.
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("StyleCop.CSharp.DocumentationRules", "SA1611:ElementParametersMustBeDocumented", Justification = "TODO DOC")]
        public SMRect EnforceDocking(BorderNodeGeometry geometry)
        {
            float constrainedY = Math.Max(StructureThickness.Top, geometry.Bounds.Y);
            // _rect.Bottom already accounts for StructureThickness - see the constructor.
            constrainedY = Math.Min(constrainedY, _rect.Bottom - geometry.Bounds.Height);
            switch (geometry.Docking)
            {
                case BorderNodeDocking.Left:
                    return new SMRect(-EdgeOverflow, constrainedY, geometry.Bounds.Width, geometry.Bounds.Height);
                case BorderNodeDocking.Right:
                    return new SMRect(_rect.Width - geometry.Bounds.Width + EdgeOverflow, constrainedY, geometry.Bounds.Width, geometry.Bounds.Height);
                default:
                    break;
            }
            return geometry.Bounds;
        }
    }
}
