# Road Network Generation (Stage 4)

This submodule extends the Stage 3 terrain pipeline with an initial highway network generator.

## What it does

- Loads `MasterPlanData` and `TerrainGenerationData` from JSON.
- Generates highway connections between the capital and planned cities.
- Flags bridges when routes cross low-elevation water.
- Flags tunnels when routes traverse steep slopes.
- Saves the result as `RoadNetwork.json`.

## Files

- `RoadNetworkBuilder.cs`: builds a simple highway network using terrain analysis.
- `RoadNetworkSaveLoadUtility.cs`: persisting road network plans to JSON.
- `RoadNetworkUsageExample.cs`: example workflow that ties master plan, terrain, and road network generation.
