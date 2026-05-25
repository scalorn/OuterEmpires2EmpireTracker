# Colony Planner (Web)

The Colony Planner is a public web page at `/planner` that lets you plan colony structures without logging in. It's useful for experimenting with build orders before committing in-game.

## Adding Structures

Use the flatpack dropdown at the top to search and add structures to your plan. Each structure starts in the "Staged" state and gets a position in the build queue.

## Structure States

Click the state buttons on each structure to toggle between:

- **Staged** — not yet built
- **Built** — constructed but not powered on
- **Online** — fully operational

The colony status panel updates in real time as you change states, showing power, habitation, food, entertainment, and warehouse capacity.

## Reordering Structures

Use the **▲** and **▼** buttons on each structure to move it up or down in the build queue. This lets you control the exact construction sequence.

Rules:
- The Colony Command Centre always stays at position 1 — it can't be moved, and nothing can move above it
- Buttons are disabled when a move isn't possible (already at top/bottom, only one structure, etc.)

## Optimize Build Order

Click **Optimize Build Order** to let the server figure out the best construction sequence. The optimizer reorders your structures so that resource constraints (power, habitation, food, entertainment) are satisfied at every build step — no deficits mid-construction.

The button is disabled when your plan has fewer than 2 structures. While the optimizer is working, a spinner shows and the button is disabled. If something goes wrong, an error message appears and your plan stays unchanged.

The optimizer may insert support structures (extra reactors, habitation blocks, etc.) into the sequence to satisfy constraints. Your local plan only reorders existing structures — inserted ones are handled server-side for the build steps display.

## Colony Status

The status panel shows running totals as structures are built:

- **Power** — provided vs required
- **Habitation** — provision vs required (workers need housing)
- **Food** — provision vs required
- **Entertainment** — provided vs required
- **Warehouse** — capacity vs required (resource storage)

## Clearing the Plan

Click **Clear Plan** to remove all structures and start fresh. A confirmation dialog prevents accidental clears.

## Notes

- The planner is entirely local — nothing is saved to the server
- No login required — anyone can use it
- The optimizer endpoint is public and stateless
