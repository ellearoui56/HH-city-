# Real 3D Asset Pack Importer

ZZ CityGen uses a real asset importer instead of placeholder/example geometry. The importer targets real downloadable CC0 city packs and turns imported FBX/OBJ models into Unity prefabs plus `PrefabDatabase`, `AssetCatalog`, license, and import-report assets.

## Real sources

The importer is configured for these real CC0 3D packs:

- Kenney **City Kit (Commercial)**: commercial buildings and skyscrapers.
- Kenney **City Kit (Suburban)**: real suburban house/building models.
- Kenney **City Kit (Industrial)**: factories, warehouses, and industrial buildings.
- Kenney **City Kit (Roads)**: road, barrier, highway, and city-detail models.
- Kenney **Nature Kit**: trees, rocks, foliage, and park/nature models.

Each pack is recorded in the generated `Assets/ZZCityGen/Real3DAssetPacks/Licenses/REAL_ASSET_SOURCES.md` manifest when the importer runs.

## How to import the real assets

1. Open the project in Unity.
2. Optional but recommended: open **Tools > ZZ CityGen > Assets > Real 3D Asset Dashboard** for a single control panel with status, import, rebuild, validation, assignment, CSV export, source links, and cleanup actions.
3. Run **Tools > ZZ CityGen > Assets > Import Real CC0 3D City Packs**.
4. The importer attempts to discover and download ZIP archives from the configured source pages.
5. If a source blocks automated download, use **Tools > ZZ CityGen > Assets > Open Real 3D Asset Sources**, download the ZIP manually, place it in `Assets/ZZCityGen/Real3DAssetPacks/Archives`, and rerun the importer.
6. If the ZIP is already inside the Unity project, select the ZIP and run **Tools > ZZ CityGen > Assets > Copy Selected ZIPs To Real Asset Archives**. Use filenames that include the pack name, such as `city-kit-commercial.zip`, `city-kit-suburban.zip`, `city-kit-industrial.zip`, `city-kit-roads.zip`, or `nature-kit.zip`.
7. The importer extracts real FBX/OBJ models, creates Unity prefab wrappers, and writes:
   - `Assets/ZZCityGen/Real3DAssetPacks/Prefabs`
   - `Assets/ZZCityGen/Real3DAssetPacks/Databases/ZZCityGenRealPrefabDatabase.asset`
   - `Assets/ZZCityGen/Real3DAssetPacks/Databases/ZZCityGenRealAssetCatalog.asset`
   - `Assets/ZZCityGen/Real3DAssetPacks/Reports/ZZCityGenRealAssetImportReport.asset`

## Quality checks and reporting

- Run **Tools > ZZ CityGen > Assets > Validate Real 3D Prefab Database** after importing. It reports missing prefab references, invalid dimensions, and duplicate prefab IDs.
- Run **Tools > ZZ CityGen > Assets > Rebuild Real Databases From Existing Prefabs** if prefabs already exist and you only need to regenerate metadata/report assets.
- Run **Tools > ZZ CityGen > Assets > Assign Real Assets To Selected World Generators** to set the generated `PrefabDatabase` and `AssetCatalog` on selected `WorldGenerator` components, or all scene generators when none are selected.
- Run **Tools > ZZ CityGen > Assets > Export Real Asset Import Report CSV** to create a spreadsheet-friendly report.
- Open `ZZCityGenRealAssetImportReport.asset` to see imported pack status, model counts, prefab counts, source model paths, generated prefab paths, categories, footprints, and heights. Its custom Inspector also links back to the dashboard, CSV export, and asset folder reveal actions.
- Run **Tools > ZZ CityGen > Assets > Reveal Real 3D Asset Folder** to open the generated real asset folder in the operating system file browser.
- Run **Tools > ZZ CityGen > Assets > Clean Generated Real Asset Outputs** to delete extracted/generated outputs while preserving downloaded archives.

## Using the imported prefabs

Assign `ZZCityGenRealPrefabDatabase.asset` to the `WorldGenerator` prefab database field. The city generator will then place the imported real models by district type, footprint, height, priority, and allowed-district metadata.
