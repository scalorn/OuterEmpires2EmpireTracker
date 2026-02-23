* Tracker Information Sharing
  * Faction Name
    * Google Sheet URL (faction level data)
    * Read/Write or Read-Only
  * Player Name
    * Google Sheet URL (player specific data)
    * Read/Write or Read-Only

* Player Information
  * Profession Levels
  * Skill Levels

* Colonies
  * Flatpacks built
  * Flatpack build plan
  * Flatpacks staged
  * Workers assigned to flatpacks
  * Mining (with survey)
  * Refining
  * Assets
  * Auto build plan tools. given what is there and where you want to go you should build flatpacks in this sequence.
* Ships configuration
  * This will be important for setting up things like commodity pickup, flatpack deliveries, etc.
* Station Assets
* Survey Reports
* Blueprints
* Commodity Delivery Report
* Manual Refining Report
* Flatpack Building Report
* Flatpack Delivery Report
* Resource Delivery Report
  * This is I want to build X of Y. These are the resources I need to put in place.  More a worksheet.


Common Database Design
  DDB instance per faction member.
  Faction leader owns the DB.
  When the faction leader adds a member it creates the table and grants permissions to their AWS user.
  That allows them to read/write their own data. But not all the data.
File swapping design
  This assumes everything is in memory and is saved to a JSON? ION? file.
  You can send your file to your faction leader.
  The faction leader can import the file to update a member.

Faction leader needs a JSON file that can contain multiple members.

