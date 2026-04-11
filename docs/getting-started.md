# Getting Started

This guide walks you through the initial setup of OE2 Empire Tracker and the general workflow for managing your Outer Empires 2 empire.

## First Launch

When you open the application for the first time, you will see the main window with an empty workspace. The main window is an MDI (Multiple Document Interface) container — all feature forms open as child windows inside it.

### Creating Your First Player Profile

Before you can manage colonies, blueprints, or surveys, you need to create a player profile:

1. Open **Forms → Player Profiles** from the menu bar.
2. Type your character name in the **Name** field.
3. Select your faction from the **Faction** dropdown.
4. Click **Save**.

Your profile is now the active player. All data you create (colonies, blueprints, surveys, routes) will be associated with this profile.

### Switching Between Players

If you manage multiple characters, use the **Current Player** dropdown in the main window toolbar to switch between profiles. All child forms will refresh to show data for the selected player.

## General Workflow

A typical session with OE2 Empire Tracker follows this pattern:

1. **Import data** — Copy colony or survey HTML from the game browser, then use the Import button in the relevant form to parse it into the tracker.
2. **Review and edit** — Browse your colonies, blueprints, and surveys. Adjust structure settings, update commodity requests, or add notes.
3. **Plan deliveries** — Create delivery routes between your colonies and use auto-fill to generate delivery plans based on colony needs.
4. **Execute deliveries** — Open the Delivery Execution form to walk through each stop, checking off items as you deliver them in-game.
5. **Monitor activity** — Use the Colony Activity form to see all active timers (mining, refining, research, manufacturing) across your empire.

## Data Storage

The application stores all data in two JSON files:

- **PlayerData.json** — Your player-specific data: profiles, colonies, blueprints, surveys, delivery routes, and plans.
- **BaselineData.json** — Shared game data: blueprint types, ship classes, tech levels, resources, and commodities.

These files are saved automatically when you make changes. You can also use **File → Save As** to save to a different location, or **File → Open** to load a previously saved file.

## Opening Forms

Use the **Forms** menu to open any feature form:

| Menu Item | Form | Description |
|-----------|------|-------------|
| Colonies | FormColony | Manage colony structures and warehouses |
| Blueprints | FormBlueprint | View and edit blueprint data |
| Surveys | FormSurvey | Import and view planet surveys |
| Delivery Routes | FormDeliveryRoute | Plan delivery routes and stops |
| Delivery Execution | FormDeliveryExecution | Execute delivery plans step by step |
| Player Profiles | FormPlayerProfile | Manage player skills and ranks |
| Colony Activity | FormColonyActivity | View all active timers across colonies |
| Colony Daily Build | FormColonyDailyBuild | Quick-build staged structures along a route |

You can open multiple instances of the same form. Each instance gets a window number (shown in the title bar) and its position is saved independently.

## Keyboard Shortcuts

| Shortcut | Action |
|----------|--------|
| Ctrl+F1 | Open Help — Contents |
| F1 | Context-sensitive help for the active form |

## Next Steps

- [Colonies](colonies.md) — Learn how to import and manage colonies
- [Player Profiles](player-profiles.md) — Set up your skills and ranks
- [Blueprints](blueprints.md) — Import and track your blueprints
