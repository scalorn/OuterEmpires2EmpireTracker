# Player Profile System — Spec

> How Player Profiles from Outer Empires 2 should be handled.

## Problem Statement

In Outer Empires 2 , players have profiles that store information about their
- Citizen ID
- Registration Date
- Active Timer
- Total Credits
- Faction
- Public Rank, Title, Grade and XP progress
- Private Rank, Title, Grade and XP progress
- Military Rank, Title, Grade and XP progress
- Ship License Information - which is a list of ship types they can fly based on their ranks.
- Skill Points
- Skills
	- Colony Directory
	    - Human Resources
		- Foreman
	- Colony Founder
	    - Founder
		- Energy Efficiency
		- Builder
	- Colony Operations
	    - Founder
		- Energy Efficiency
		- Builder
	- Colony Operations
	    - Refining Focus
		- Production Focus
		- Extraction Focus
	- Commander
	    - Damage Control
	- Engineer
	    - Engineering Capacity
	- Entrepreneur
	    - Sound As A Pound
		- Self-made Millionaire
		- AAA Healthcare
	- Job Management
	    - Job Opportunities
		- Contract Management
	- Researcher
	    - Research Review
		- Research Methods
		- Research Focus
	- Surveyor
	    - Surveying Methods
		- Scanning Methods
		- Quartermaster
	- Trader
	    - Broker
		

## Design

---

### Phase 1: Maintenance Form

A form that allows players to view and edit their profile information, including their skills and ship licenses.
This form should also allow players to see their current XP progress for each rank and title.

This form should have a list of Profiles on the left side of the form.
The right side should include all the profile information in an editable way.
There should be an Add, Save, Cancel and Delete button at the bottom of the form.

