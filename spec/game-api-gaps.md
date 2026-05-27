# Game API Gaps and Findings

Research conducted 2026-05-27 using the GameApiFullDiscoveryTests discovery tool.

## Asset typeC Codes (Confirmed)

The `/v1/assets/locations/{id}?locationType={type}` endpoint returns cargo items with a `typeC` field. The complete mapping:

| typeC | Meaning | Count (observed) | Examples |
|-------|---------|-----------------|----------|
| `A` | Ammunition | 5 | 2cm Coilgun Munitions, 10cm Railgun Munitions |
| `Bp` | Blueprint | 164 | Rock Sled, RAK-ML5 Mining Laser (MilSpec) |
| `C` | Refined Resource | 12 unique | Acidic Inorganics, Alkaline Earth Metals, S1. Translivermoric Exotics |
| `Cr` | Crate (container) | 44 | C2 Ship Stuff, BP - Flatpacks, Commodities |
| `F` | Flatpack | 15 | Flatpack: Entertainment Centre, Flatpack: Warehouse |
| `R` | Resource (raw) | 402 | Heavy Post-Trans Metals (Unrefined, Med Purity) |
| `S` | Ship Part | 57 | Rock Sled, AMX-MM Reactor Core (MilSpec) |
| `Sc` | Scan/Survey | 373 | Survey Report: Zeh Vazoran I (E4C6939) |
| `Sh` | Share | 7 | Share: Space 3, Share: The Threshold |
| `W` | Worker | 151 | White Collar Detail, Specialist Detail |

### Key Distinction: `R` vs `C`

- `R` = Raw unrefined resources WITH purity grades (e.g. "Heavy Post-Trans Metals (Unrefined, Med Purity)")
- `C` = Refined/processed resources WITHOUT purity (e.g. "Acidic Inorganics", "S1. Translivermoric Exotics")
- Neither `R` nor `C` contains the 209 manufactured commodities (like "Advanced Biolubricants", "AGI Archives")

## GAP: Crate Contents Not Accessible

**Status:** Reported to dev, awaiting clarification.

### Problem

Crates (`typeC=Cr`) appear in the station/ship cargo list with an `amount` field showing how many items they contain, but their contents cannot be enumerated through the API.

### Evidence

At Alef Hestrixia UPC Station (locationId=171916):
- The "Commodities" crate (`cargoItemId=1604938`) shows `amount=209`, `mass=14,847,350`, `volume=2,969,470`
- The station's flat cargo list has 279 items total, but ZERO `typeC=C` commodity items
- This means the 209 commodities are inside the crate but not returned in the station's cargo response

### What We Tried

Called `/v1/assets/locations/1604938?locationType=Cr`:
- Response: `{ "success": true, "data": { "cargo": [], "ships": [] } }`
- The API accepts the request (no 404) but returns empty cargo
- The `cargoItemId` may not be the correct identifier for this endpoint

### Impact

- Cannot enumerate what's inside any crate
- The 209 manufactured commodities are invisible to the API
- Station cargo responses only show items NOT inside crates
- Total asset inventory is incomplete without crate contents

### Crates Observed at Alef Hestrixia

| cargoItemId | Name | Amount | Notes |
|-------------|------|--------|-------|
| 1604936 | C2 Ship Stuff | 0 | Empty |
| 1604937 | BP - Flatpacks | 25 | |
| 1604938 | Commodities | 209 | All manufactured commodities |
| 1605057 | C3 Ship Stuff | 0 | Empty |
| 1605058 | C6 Ship Stuff | 0 | Empty |
| 1605059 | C5 Ship Stuff | 0 | Empty |
| 1605060 | Resources - Refined | 32 | |
| 1605061 | Resources - Unrefined High | 27 | |
| 1605062 | Resources - Unrefined Med | 19 | |
| 1605063 | Resources - Unefined Low | 24 | |
| 1605193 | Surveys - OLD | 238 | |
| 1653872 | BP - Munitions | 195 | |
| 1654032-38 | BP - C2 through C8 Ship Stuff | 36-199 | |

### Questions for Dev

1. Is there an endpoint to enumerate crate contents?
2. Should `/v1/assets/locations/{cargoItemId}?locationType=Cr` work? If so, what ID should be used?
3. Are crate contents intentionally excluded from the parent location's cargo response?
4. Is there a plan to expose crate contents in the API?

## Location Types (Confirmed)

The `/v1/assets/locations` endpoint returns locations with these `locationType` values:

| locationType | Meaning | Count (observed) |
|--------------|---------|-----------------|
| `St` | Station | 9 |
| `Co` | Colony | 74 |
| `Sh` | Ship | 1 |

## Pagination Support (Confirmed)

| Endpoint | Has Pagination | Fields |
|----------|---------------|--------|
| `/v1/banking/transactions` | Yes | `totalRecords`, `offset`, `limit` (default 50) |
| `/v1/mail` | Yes | `totalRecords`, `offset`, `limit` (default 50) |
| `/v1/killmails` | Yes | `totalRecords`, `offset`, `limit` (default 25) |
| `/v1/assets/locations` | No | Returns all locations in one response |
| `/v1/colonies` | No | Returns all colonies in one response |

## Discovery Run Statistics (2026-05-27)

- **Total API calls:** 2,940
- **Succeeded:** 2,940
- **Failed:** 0
- **Skipped:** 1 (kill mails - empty list)
- **Duration:** 41 minutes at 30 req/min rate limit
- **Data captured:**
  - 74 colonies × 4 endpoints = 296 calls
  - 33,195 banking transactions (664 pages)
  - 1,846 mail messages (37 pages + 1,846 detail calls)
  - 83 asset location details
  - Character profile + skills
  - Ship configuration + cargo
  - Banking balance
  - Jobs accepted
  - Kill mails (empty)
