# Mining Rig Structures

**REQ-COL-050** When a structure's blueprint type is MiningRig, the structure control SHALL display survey selection and resource selection combos.  
**REQ-COL-051** The survey combo SHALL be filtered to surveys matching the colony's PlanetName.  
**REQ-COL-052** Selecting a survey SHALL populate the resource combo with that survey's resources.  
**REQ-COL-053** Clicking Start SHALL begin a repeating countdown timer for the mining process.  
**REQ-COL-054** The countdown display SHALL update every second while the timer is running.  
**REQ-COL-055** Clicking Done SHALL call Colony.ProcessColony(), stop the timer, and clear the process state.  
**REQ-COL-056** Colony.ProcessColony() SHALL calculate mined quantity per interval as `floor(Amount + MiningLeftOvers)`, accumulate the fractional remainder in MiningLeftOvers, add the integer quantity to the matching resource item in the colony warehouse, and consume the processed intervals.  
**REQ-COL-056a** MiningLeftOvers SHALL be reset to 0 when MiningSurvey or MiningSurveyResource changes on a structure.  
**REQ-COL-056b** The mined quantity per interval SHALL be multiplied by `(1 + ExtractionFocusLevel * 0.01)` where ExtractionFocusLevel is the Extraction Focus skill level of the player who owns the colony. This requires multi-player support (see REQ-ARCH-070 series).
