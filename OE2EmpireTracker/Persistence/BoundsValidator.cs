using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace OE2EmpireTracker.Persistence
{
    /// <summary>
    /// Pure-function bounds validation for restored window positions.
    /// Ensures windows remain visible and reasonably sized after restore.
    /// </summary>
    public static class BoundsValidator
    {
        public const int MinWidth = 320;
        public const int MinHeight = 200;
        public const int ScreenEdgeMargin = 100;

        /// <summary>
        /// Validates and adjusts saved bounds for the main window against all active screens.
        /// Clamps size to minimums, shifts position if too close to edges, and resets to
        /// primary monitor default if entirely off-screen.
        /// </summary>
        public static Rectangle ValidateMainWindowBounds(Rectangle savedBounds)
        {
            // 1. Clamp size to minimums (Req 2.4)
            int width = savedBounds.Width < MinWidth ? MinWidth : savedBounds.Width;
            int height = savedBounds.Height < MinHeight ? MinHeight : savedBounds.Height;

            var result = new Rectangle(savedBounds.X, savedBounds.Y, width, height);

            // 2. Compute the union of all screen working areas (Visible_Screen_Area)
            Rectangle visibleArea = GetVisibleScreenArea();

            // 3. If entirely off-screen, reset to primary monitor default (Req 2.1)
            if (!result.IntersectsWith(visibleArea))
            {
                Rectangle primary = Screen.PrimaryScreen.WorkingArea;
                return new Rectangle(primary.X + ScreenEdgeMargin, primary.Y + ScreenEdgeMargin, width, height);
            }

            // 4. Shift left if right edge is within ScreenEdgeMargin of visible area right edge (Req 2.2)
            int rightEdge = result.Right;
            int visibleRight = visibleArea.Right;
            if (rightEdge > visibleRight - ScreenEdgeMargin)
            {
                result.X = visibleRight - ScreenEdgeMargin - result.Width;
            }

            // 5. Shift up if bottom edge is within ScreenEdgeMargin of visible area bottom edge (Req 2.3)
            int bottomEdge = result.Bottom;
            int visibleBottom = visibleArea.Bottom;
            if (bottomEdge > visibleBottom - ScreenEdgeMargin)
            {
                result.Y = visibleBottom - ScreenEdgeMargin - result.Height;
            }

            return result;
        }

        /// <summary>
        /// Validates and adjusts saved bounds for an MDI child against the parent client area.
        /// Clamps size to minimums, shifts position if too close to edges, and resets to
        /// default cascade position if entirely outside the parent.
        /// </summary>
        public static Rectangle ValidateMdiChildBounds(Rectangle savedBounds, Rectangle parentClientArea)
        {
            // 1. Clamp size to minimums (Req 5.4)
            int width = savedBounds.Width < MinWidth ? MinWidth : savedBounds.Width;
            int height = savedBounds.Height < MinHeight ? MinHeight : savedBounds.Height;

            var result = new Rectangle(savedBounds.X, savedBounds.Y, width, height);

            // 2. If entirely outside parent client area, reset to default cascade position (Req 5.1)
            if (!result.IntersectsWith(parentClientArea))
            {
                return new Rectangle(parentClientArea.X + 10, parentClientArea.Y + 10, width, height);
            }

            // 3. Shift left if right edge is within ScreenEdgeMargin of parent right edge (Req 5.2)
            int rightEdge = result.Right;
            int parentRight = parentClientArea.Right;
            if (rightEdge > parentRight - ScreenEdgeMargin)
            {
                result.X = parentRight - ScreenEdgeMargin - result.Width;
            }

            // 4. Shift up if bottom edge is within ScreenEdgeMargin of parent bottom edge (Req 5.3)
            int bottomEdge = result.Bottom;
            int parentBottom = parentClientArea.Bottom;
            if (bottomEdge > parentBottom - ScreenEdgeMargin)
            {
                result.Y = parentBottom - ScreenEdgeMargin - result.Height;
            }

            return result;
        }

        /// <summary>
        /// Returns the bounding rectangle that encloses all screen working areas.
        /// </summary>
        private static Rectangle GetVisibleScreenArea()
        {
            var screens = Screen.AllScreens;
            if (screens.Length == 0)
            {
                return Screen.PrimaryScreen.WorkingArea;
            }

            Rectangle union = screens[0].WorkingArea;
            for (int i = 1; i < screens.Length; i++)
            {
                union = Rectangle.Union(union, screens[i].WorkingArea);
            }
            return union;
        }
    }
}
