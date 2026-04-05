# Product Overview

OE2EmpireTracker is a Windows desktop application for tracking and managing player empires in the game "Outer Empires 2."

It helps players manage:
- Colonies (structures, mining rigs, refineries, research labs, manufactories, commodity factories)
- Blueprints (scanning, evolution, manufacturing)
- Surveys (planet resource surveys parsed from HTML)
- Player profiles (skills, ranks, professions)
- Delivery routes and plans (commodity logistics across colonies)
- Timed processing (mining cycles, refining, research, manufacturing countdowns)

The app simulates game mechanics locally — processing colony structures on timer intervals, tracking resource inventories, and computing colony status. Data is persisted to JSON files (`PlayerData.json` for player-specific data, `BaselineData.json` for shared game data).

The domain model reflects the game's economy: resources are mined at various purities, refined (including synthetic tiers), used in manufacturing or commodity production, and delivered between colonies via ship routes.
