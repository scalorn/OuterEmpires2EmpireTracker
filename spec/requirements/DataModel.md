# Data Model Requirements

## PropertyBag

**REQ-DM-001** PropertyBag SHALL store string, double, and bool values under string keys.  
**REQ-DM-002** Setting a key that already exists SHALL overwrite the previous value without error.  
**REQ-DM-003** Getting a missing key SHALL return false and the supplied default value unchanged.  
**REQ-DM-004** Getting a key whose stored string cannot be parsed to the requested type SHALL return false.  
**REQ-DM-005** PropertyBag SHALL serialize to a flat JSON object `{ "key": "value" }` with no type wrappers.  
**REQ-DM-006** PropertyBag SHALL deserialize from the same flat JSON format, restoring all key/value pairs.  
**REQ-DM-007** Clear() SHALL remove all entries, leaving an empty dictionary.  
**REQ-DM-008** Remove() SHALL return true and remove the entry when the key exists, false when it does not.

## ItemBag

**REQ-DM-010** ItemBag SHALL store Item instances keyed by their UUID.  
**REQ-DM-011** AddItem() SHALL throw when an item with the same UUID already exists.  
**REQ-DM-012** CountByType(itemType, baseItemTypeID) SHALL return the sum of Quantity across all items matching both fields, using ordinal string comparison.  
**REQ-DM-013** CountByType SHALL return 0 when no items match.  
**REQ-DM-014** FindResource(resource, purity) SHALL return all items of type Resource whose BaseItemTypeID and ResourcePurity match, using ordinal comparison.  
**REQ-DM-015** ItemBag SHALL serialize to a JSON object keyed by UUID and deserialize back to the same state.

## Item and ExtendedName

**REQ-DM-020** Item.ExtendedName for ItemType=Resource SHALL append `(Purity)` when ResourcePurity is non-empty.  
**REQ-DM-021** Item.ExtendedName for ItemType=Commodity SHALL return the Commodity's own ExtendedName looked up by BaseItemTypeID; if not found, return Name.  
**REQ-DM-022** Item.ExtendedName for ItemType=Survey SHALL return `PlanetName (SurveyID)` with `[NickName]` appended when NickName is non-empty, looked up via PlayerContext.findSurvey(BaseItemTypeID); if PlayerContext is null or survey not found, return Name.  
**REQ-DM-023** Item.ExtendedName for ItemType=Blueprint SHALL return `C{Class} Ev({Evolution}) Name (TechLevel) [NickName]` with each segment omitted when its value is zero/null/empty, looked up via PlayerContext.findBlueprint(BaseItemTypeID); if not found, return Name.  
**REQ-DM-024** Item.ExtendedName SHALL be decorated with [JsonIgnore] and not appear in serialized JSON.

## Blueprint

**REQ-DM-030** Blueprint.ExtendedName SHALL return empty string when UUID is null.  
**REQ-DM-031** Blueprint.ExtendedName SHALL include `C{Class}` prefix only when Class > 0.  
**REQ-DM-032** Blueprint.ExtendedName SHALL include `Ev({Evolution})` only when Evolution > 0.  
**REQ-DM-033** Blueprint.ExtendedName SHALL include `(TechLevel)` only when TechLevel is non-null and non-empty.  
**REQ-DM-034** Blueprint.ExtendedName SHALL include `[NickName]` only when NickName is non-empty.  
**REQ-DM-035** Blueprint.ExtendedName SHALL be trimmed — no leading or trailing whitespace.  
**REQ-DM-036** Blueprint.ExtendedName SHALL be decorated with [JsonIgnore].

## Survey and SurveyResource

**REQ-DM-040** Survey.ExtendedName SHALL return `PlanetName (SurveyID)` with `[NickName]` appended when NickName is non-empty.  
**REQ-DM-041** Survey.ExtendedName SHALL return empty string when PlanetName, SurveyID, and NickName are all null or empty.  
**REQ-DM-042** SurveyResource SHALL have Resource, Purity, and Amount string properties.  
**REQ-DM-043** SurveyResource.ExtendedName SHALL return `Resource (Purity) (Amount)/h` omitting segments that are null or empty.

## CountDownTime

**REQ-DM-050** CountDownTime.TimeRemaining getter SHALL return the number of seconds until EndTime from now.  
**REQ-DM-051** CountDownTime.TimeRemaining setter SHALL set EndTime to now + the given seconds.  
**REQ-DM-052** CountDownTime.TimeRemainingString getter SHALL format as `Xd Yh Zm Ws`. Leading zero-value segments (before the first non-zero segment) SHALL be omitted. Once the first non-zero segment has been included, all subsequent lower segments SHALL be shown even if their value is zero (e.g. `1h 0m 30s`, not `1h 30s`). When TimeRemaining <= 0 it SHALL return `"0s"`.  
**REQ-DM-053** CountDownTime.TimeRemainingString setter SHALL parse `Xd Yh Zm Ws` (all segments optional) and set TimeRemaining to the total seconds.  
**REQ-DM-054** CountDownTime.TimeRemaining SHALL be decorated with [JsonIgnore].  
**REQ-DM-055** CountDownTime in repeating mode SHALL track IntervalsPassed as the number of complete intervals elapsed since StartTime.  
**REQ-DM-056** ConsumeIntervals(n) SHALL advance StartTime by n * RepeatIntervalSeconds, reducing IntervalsPassed by n.  
**REQ-DM-057** StartRepeating(intervalSeconds) SHALL set RepeatIntervalSeconds, StartTime=now, EndTime=now+interval.

## LockTracking

**REQ-DM-060** LockItem(processUUID, itemType, baseID, qty) SHALL add qty to the existing lock for that process+item, creating the entry if absent.  
**REQ-DM-061** LockItem SHALL throw ArgumentNullException when processUUID is null or empty.  
**REQ-DM-062** LockItems SHALL call LockItem for each entry in the supplied collection.  
**REQ-DM-063** GetLockedQuantity(itemType, baseID) SHALL return the sum of locked quantities across all processes for that item.  
**REQ-DM-064** GetLocksForProcess(processUUID) SHALL return all ItemLock entries for that process, or an empty list if none exist.  
**REQ-DM-065** ClearLocksForProcess(processUUID) SHALL remove all locks for that process; calling it for an unknown UUID SHALL not throw.  
**REQ-DM-066** LockTracking SHALL serialize to `{ "processUUID": { "ItemType:BaseID": quantity } }` and deserialize back to the same state.

## Static Reference Data (Commodity, ResourceGroup, ResourcePurity, ResourceClass, ItemType, WorkerDetail)

**REQ-DM-070** Each static reference list SHALL have a None/blank entry as the first element.  
**REQ-DM-071** All non-None entries SHALL have a non-empty Name.  
**REQ-DM-072** No two entries in the same list SHALL share the same ID or Name.  
**REQ-DM-073** Non-None entries SHALL be sorted alphabetically by Name.  
**REQ-DM-074** Each list SHALL contain an entry for every value in its corresponding enum.  
**REQ-DM-075** MapByEnum and MapByString SHALL be consistent: for every entry, enum→Name→enum SHALL return the original enum value.  
**REQ-DM-076** ResourceGroup.Synthetic entry SHALL have Synthetic=true; all other entries SHALL have Synthetic=false.  
**REQ-DM-077** ResourcePurity.Refined entry SHALL have Refined=true; all other entries SHALL have Refined=false.  
**REQ-DM-078** WorkerDetail SHALL contain exactly three named entries: BlueCollarDetail, WhiteCollarDetail, SpecialistDetail.  
**REQ-DM-079** WorkerDetail IDs SHALL match the property bag keys used in ColonyStatusCalculator (BlueCollarDetail, WhiteCollarDetail, SpecialistDetail).

## PlayerProfile and Skills

**REQ-DM-080** PlayerProfile.GetSkill(name) SHALL create and return a new PlayerSkill when the key is absent, and return the existing instance on subsequent calls.  
**REQ-DM-081** PlayerProfile.GetSkill(SkillName enum) SHALL use the enum's Description attribute as the dictionary key.  
**REQ-DM-082** PlayerProfile.GetSkillGroup(name) SHALL return false when the key is absent.  
**REQ-DM-083** PlayerProfile.SetSkillGroup(name, value) followed by GetSkillGroup(name) SHALL return value.  
**REQ-DM-084** SkillName and SkillGroupName enum values SHALL each have a non-empty Description attribute.  
**REQ-DM-085** PlayerRank SHALL default Rank, CurrentXP, and NextXP to 0.
