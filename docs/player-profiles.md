# Player Profiles

The Player Profile form lets you manage your character's skills, ranks, and basic information. Skills affect colony operations like build times, mining efficiency, and research speed.

## Opening the Player Profile Form

Open **Forms → Player Profiles** from the menu bar.

## Profile Basics

The left panel shows a list of all player profiles. The right panel shows details for the selected profile:

- **Name** — Your character name (must be unique across profiles).
- **Faction** — Your character's faction.
- **Total Credits** — Your current credit balance.
- **Skill Points** — Available unspent skill points.

## Importing a Profile from the Game

The fastest way to create or update a profile is to import it directly from the game:

1. In the game browser, open your character's profile panel (the left slideout with ranks, skills, and stats).
2. Select all (Ctrl+A) and copy (Ctrl+C) the page content.
3. In the tracker, open **Forms → Player Profiles** and click **Import**.

The tracker parses your character name, faction, credits, all three rank tracks (level, title, XP), skill points, skill group states, and individual skill levels including training status. If a profile with the same name already exists, it updates the existing one. Otherwise it creates a new profile automatically.

The import also captures your Citizen ID, Registration Date, and Active Time from the profile headline.

## Creating a Profile Manually

1. Click **New** to clear the form.
2. Enter a unique **Name** for the profile.
3. Select or type a **Faction**.
4. Click **Save**.

The new profile appears in the list and can be selected as the current player from the main window toolbar.

## Ranks

The profile tracks three rank categories, each with a current rank level and XP progress:

| Rank | Description |
|------|-------------|
| Public | Your public reputation rank |
| Private | Your private enterprise rank |
| Military | Your military service rank |

For each rank, enter the current rank number, current XP, and XP needed for the next level.

## Skill Groups

Skills are organized into skill groups. Each group must be enabled (checked) before its individual skills become active. The available skill groups are:

| Group | Skills |
|-------|--------|
| Colony Director | Human Resources, Foreman |
| Colony Founder | Founder, Energy Efficiency, Builder |
| Colony Operations | Refining Focus, Production Focus, Extraction Focus |
| Commander | Damage Control |
| Engineer | Engineering Capacity |
| Entrepreneur | Sound As A Pound, Self Made Millionaire, AAA Healthcare |
| Job Management | Job Opportunities, Contract Management |
| Researcher | Research Review, Research Methods, Research Focus |
| Surveyor | Surveying Methods, Scanning Methods, Quartermaster |
| Trader | Broker |

### Managing Skills

Each skill block shows:

- **Skill name** — The name of the skill.
- **Level** — The current skill level.
- **Training status** — Whether the skill is currently being trained.

To train a skill:

1. Enable the skill group by checking its checkbox.
2. Click the training button on the skill block.
3. Only one skill can be trained at a time across all skill groups.

Skill levels affect various game mechanics. For example:

- **Builder** level reduces colony structure build times.
- **Extraction Focus** improves mining output.
- **Refining Focus** improves refining efficiency.
- **Research Focus** speeds up research operations.

## Switching Players

Use the **Current Player** dropdown in the main window toolbar to switch between profiles. All forms will refresh to show data for the selected player.

## Deleting a Profile

1. Select the profile in the list.
2. Click **Delete**.
3. Confirm the deletion.

Deleting a profile also removes all associated data (colonies, blueprints, surveys, routes, and plans).

## Related Topics

- [Getting Started](getting-started.md) — Creating your first profile
- [Colonies](colonies.md) — Skills affect colony structure operations
- [Background Processing](background-processing.md) — Skill training timers are tracked automatically
