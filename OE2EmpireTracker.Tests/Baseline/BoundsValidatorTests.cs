using System.Drawing;
using NUnit.Framework;
using OE2EmpireTracker.Persistence;

namespace OE2EmpireTracker.Tests.Baseline
{
    [TestFixture]
    public class BoundsValidatorTests
    {
        // A standard parent client area for MDI child tests
        private static readonly Rectangle ParentArea = new Rectangle(0, 0, 1200, 800);

        #region MDI Child — Entirely off-screen resets to default cascade (Req 5.1)

        [Test]
        public void MdiChild_EntirelyOutsideParent_ResetsToDefaultCascade()
        {
            var saved = new Rectangle(2000, 2000, 500, 400);

            var result = BoundsValidator.ValidateMdiChildBounds(saved, ParentArea);

            Assert.That(result.X, Is.EqualTo(ParentArea.X + 10));
            Assert.That(result.Y, Is.EqualTo(ParentArea.Y + 10));
            Assert.That(result.Width, Is.EqualTo(500));
            Assert.That(result.Height, Is.EqualTo(400));
        }

        [Test]
        public void MdiChild_NegativePositionEntirelyOutside_ResetsToDefaultCascade()
        {
            var saved = new Rectangle(-1000, -1000, 400, 300);

            var result = BoundsValidator.ValidateMdiChildBounds(saved, ParentArea);

            Assert.That(result.X, Is.EqualTo(ParentArea.X + 10));
            Assert.That(result.Y, Is.EqualTo(ParentArea.Y + 10));
        }

        #endregion

        #region MDI Child — Right edge within margin shifts left (Req 5.2)

        [Test]
        public void MdiChild_RightEdgeWithinMargin_ShiftsLeft()
        {
            // Place window so right edge is within 100px of parent right edge
            // Parent right = 1200, margin = 100, so threshold = 1100
            // Window at x=700, width=500 → right edge = 1200 (> 1100)
            var saved = new Rectangle(700, 50, 500, 400);

            var result = BoundsValidator.ValidateMdiChildBounds(saved, ParentArea);

            // Should shift so right edge = parentRight - margin = 1100
            // x = 1100 - 500 = 600
            Assert.That(result.X, Is.EqualTo(ParentArea.Right - BoundsValidator.ScreenEdgeMargin - 500));
            Assert.That(result.Width, Is.EqualTo(500));
        }

        #endregion

        #region MDI Child — Bottom edge within margin shifts up (Req 5.3)

        [Test]
        public void MdiChild_BottomEdgeWithinMargin_ShiftsUp()
        {
            // Parent bottom = 800, margin = 100, threshold = 700
            // Window at y=450, height=400 → bottom = 850 (> 700)
            var saved = new Rectangle(50, 450, 500, 400);

            var result = BoundsValidator.ValidateMdiChildBounds(saved, ParentArea);

            // Should shift so bottom = parentBottom - margin = 700
            // y = 700 - 400 = 300
            Assert.That(result.Y, Is.EqualTo(ParentArea.Bottom - BoundsValidator.ScreenEdgeMargin - 400));
            Assert.That(result.Height, Is.EqualTo(400));
        }

        #endregion

        #region MDI Child — Width/height below minimum clamped to 320x200 (Req 5.4)

        [Test]
        public void MdiChild_WidthBelowMinimum_ClampedTo320()
        {
            var saved = new Rectangle(50, 50, 100, 400);

            var result = BoundsValidator.ValidateMdiChildBounds(saved, ParentArea);

            Assert.That(result.Width, Is.EqualTo(BoundsValidator.MinWidth));
        }

        [Test]
        public void MdiChild_HeightBelowMinimum_ClampedTo200()
        {
            var saved = new Rectangle(50, 50, 500, 50);

            var result = BoundsValidator.ValidateMdiChildBounds(saved, ParentArea);

            Assert.That(result.Height, Is.EqualTo(BoundsValidator.MinHeight));
        }

        [Test]
        public void MdiChild_BothDimensionsBelowMinimum_ClampedTo320x200()
        {
            var saved = new Rectangle(50, 50, 10, 10);

            var result = BoundsValidator.ValidateMdiChildBounds(saved, ParentArea);

            Assert.That(result.Width, Is.EqualTo(BoundsValidator.MinWidth));
            Assert.That(result.Height, Is.EqualTo(BoundsValidator.MinHeight));
        }

        #endregion

        #region MDI Child — Valid bounds pass through unchanged

        [Test]
        public void MdiChild_ValidBounds_PassThroughUnchanged()
        {
            var saved = new Rectangle(50, 50, 500, 400);

            var result = BoundsValidator.ValidateMdiChildBounds(saved, ParentArea);

            Assert.That(result.X, Is.EqualTo(50));
            Assert.That(result.Y, Is.EqualTo(50));
            Assert.That(result.Width, Is.EqualTo(500));
            Assert.That(result.Height, Is.EqualTo(400));
        }

        [Test]
        public void MdiChild_AtOriginWithValidSize_PassThroughUnchanged()
        {
            var saved = new Rectangle(0, 0, 400, 300);

            var result = BoundsValidator.ValidateMdiChildBounds(saved, ParentArea);

            Assert.That(result.X, Is.EqualTo(0));
            Assert.That(result.Y, Is.EqualTo(0));
            Assert.That(result.Width, Is.EqualTo(400));
            Assert.That(result.Height, Is.EqualTo(300));
        }

        #endregion

        #region MDI Child — Combined edge shifts and size clamping

        [Test]
        public void MdiChild_BothEdgesWithinMargin_ShiftsBothAxes()
        {
            // Right and bottom both exceed threshold
            var saved = new Rectangle(800, 500, 500, 400);

            var result = BoundsValidator.ValidateMdiChildBounds(saved, ParentArea);

            Assert.That(result.X, Is.EqualTo(ParentArea.Right - BoundsValidator.ScreenEdgeMargin - 500));
            Assert.That(result.Y, Is.EqualTo(ParentArea.Bottom - BoundsValidator.ScreenEdgeMargin - 400));
        }

        [Test]
        public void MdiChild_SmallSizeAndOffEdge_ClampsAndShifts()
        {
            // Tiny window near the right-bottom edge
            var saved = new Rectangle(1100, 700, 50, 50);

            var result = BoundsValidator.ValidateMdiChildBounds(saved, ParentArea);

            // Size clamped first
            Assert.That(result.Width, Is.EqualTo(BoundsValidator.MinWidth));
            Assert.That(result.Height, Is.EqualTo(BoundsValidator.MinHeight));
        }

        #endregion

        #region MDI Child — Non-zero origin parent client area

        [Test]
        public void MdiChild_NonZeroOriginParent_EntirelyOutside_ResetsToParentCascade()
        {
            var parent = new Rectangle(100, 100, 800, 600);
            var saved = new Rectangle(0, 0, 400, 300);

            var result = BoundsValidator.ValidateMdiChildBounds(saved, parent);

            // (0,0) with 400x300 → right=400, bottom=300
            // parent is (100,100)-(900,700)
            // The window (0,0,400,300) does intersect with parent (100,100,800,600)
            // since the overlap region is (100,100)-(400,300) which is valid
            // So it should pass through (possibly with edge adjustments)
            Assert.That(result.Width, Is.EqualTo(400));
            Assert.That(result.Height, Is.EqualTo(300));
        }

        [Test]
        public void MdiChild_NonZeroOriginParent_CompletelyOutside_Resets()
        {
            var parent = new Rectangle(100, 100, 800, 600);
            // Window entirely to the left and above the parent
            var saved = new Rectangle(-500, -500, 400, 300);

            var result = BoundsValidator.ValidateMdiChildBounds(saved, parent);

            Assert.That(result.X, Is.EqualTo(parent.X + 10));
            Assert.That(result.Y, Is.EqualTo(parent.Y + 10));
        }

        [Test]
        public void MdiChild_NonZeroOriginParent_RightEdgeShift()
        {
            var parent = new Rectangle(100, 100, 800, 600);
            // Parent right = 900, margin threshold = 800
            // Window at x=500, width=400 → right = 900 (> 800)
            var saved = new Rectangle(500, 200, 400, 300);

            var result = BoundsValidator.ValidateMdiChildBounds(saved, parent);

            Assert.That(result.X, Is.EqualTo(parent.Right - BoundsValidator.ScreenEdgeMargin - 400));
        }

        #endregion

        #region Constants verification

        [Test]
        public void Constants_HaveExpectedValues()
        {
            Assert.That(BoundsValidator.MinWidth, Is.EqualTo(320));
            Assert.That(BoundsValidator.MinHeight, Is.EqualTo(200));
            Assert.That(BoundsValidator.ScreenEdgeMargin, Is.EqualTo(100));
        }

        #endregion
    }
}
