# Design: MDI Window Menu

## Implementation
- `windowToolStripMenuItem` added to `menuStrip1.Items` between `editToolStripMenuItem` and `helpToolStripMenuItem`
- `menuStrip1.MdiWindowListItem = windowToolStripMenuItem` enables automatic child form listing
- Cascade/Tile Horizontal/Tile Vertical call `LayoutMdi()` with the appropriate `MdiLayout` enum
- A `ToolStripSeparator` separates layout commands from the auto-populated child list
