# Level JSON schema (invariant 2: levels are pure data)

Level files are TextAssets at `Assets/Resources/Levels/<id>.json`. Adding a level = adding one file; `LevelValidator` (the invariant-2 guard) rejects anything malformed, dangling, or impossible-by-construction, and a test validates every shipped file.

```jsonc
{
  "id": "level-042",          // required, matches filename
  "name": "Rush Hour",        // optional display name (defaults to id)
  "columns": 11,              // board width in cells, 5..30
  "medal": {                  // seconds; strictly gold < silver < bronze
    "gold": 24.0, "silver": 31.0, "bronze": 40.0
  },                          // authored by MedalCalibrator (M5) from diagonal-free bot solves
  "startColumn": 5,           // spawn column on the bank row
  "bays": [1, 5, 9],          // goal-bay columns on the goal row; ≥1, unique, in range
  "rows": [                   // TOP-DOWN: rows[0] = goal, last = bank
    { "kind": "goal" },
    { "kind": "road",         // any LaneKindDef id (road|river|swamp|tracks|bike|walkway|grass|concrete|goal|bank)
      "dir": "right",         // left|right — required when the row moves
      "speed": 2.2,           // cells/second — required when the row moves
      "objects": [            // repeating trains of lane objects
        { "pieceId": "truck", // any LaneObjectDef id; role must fit the lane kind
          "offset": 0,        // cells: first instance position at tick 0
          "spacing": 7.5,     // cells between instance starts; 0 = single instance; must be ≥ size+1
          "phase": 0 }        // cycle phase ticks (turtle submerge / gator mouth)
      ] },
    { "kind": "grass",
      "obstructions": [       // safe-ground rows only
        { "pieceId": "tree", "column": 2 } ] },
    { "kind": "walkway", "dir": "left", "speed": 1.5 },   // conveyor: speed+dir, no objects
    { "kind": "bank" }
  ]
}
```

## Semantics (enforced by the validator)
- Lane-kind roles: `DeadlyTraffic` rows carry `Kill` objects; `Water` rows carry `Rideable`/`StaticSafe`; `CrashTraffic` rows carry `Crashable`; conveyors and safe rows carry none.
- Object kinematics are deterministic: `x(tick) = offset + dirSign · speed · tick/60`, wrapped over `columns + 2·margin` (margin = ceil(max object size) + 1). `phase` shifts rideability cycles, never position.
- All piece ids resolve through `PieceRegistry` — unknown ids are validation errors, not runtime surprises.

## Units
Cells (board squares) and seconds. The sim runs at 60 ticks/second; `phase` and def cycle fields are in ticks.
