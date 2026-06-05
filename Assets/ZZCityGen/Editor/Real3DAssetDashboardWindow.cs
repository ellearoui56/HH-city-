using UnityEditor;
using UnityEngine;
using ZZCityGen.Data;

namespace ZZCityGen.Editor
{
    public sealed class Real3DAssetDashboardWindow : EditorWindow
    {
        private Vector2 scroll;

        [MenuItem("Tools/ZZ CityGen/Assets/Real 3D Asset Dashboard")]
        public static void Open()
        {
            GetWindow<Real3DAssetDashboardWindow>("Real 3D Assets");
        }

        private void OnGUI()
        {
            scroll = EditorGUILayout.BeginScrollView(scroll);
            DrawHeader();
            DrawStatus();
            DrawActions();
            DrawReport();
            EditorGUILayout.EndScrollView();
        }

        private static void DrawHeader()
        {
            EditorGUILayout.LabelField("ZZ CityGen Real 3D Assets", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("Import real CC0 FBX/OBJ city assets, build placement databases, validate metadata, and assign the generated database to WorldGenerator components.", MessageType.Info);
        }

        private static void DrawStatus()
        {
            var prefabDatabase = Real3DAssetPackImporter.LoadRealPrefabDatabase();
            var assetCatalog = Real3DAssetPackImporter.LoadRealAssetCatalog();
            var report = Real3DAssetPackImporter.LoadImportReport();

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Status", EditorStyles.boldLabel);
            EditorGUILayout.LabelField("Archive ZIPs", Real3DAssetPackImporter.CountLocalArchives().ToString());
            EditorGUILayout.LabelField("Generated Prefabs", Real3DAssetPackImporter.CountGeneratedPrefabs().ToString());
            EditorGUILayout.LabelField("Prefab Database Entries", prefabDatabase != null ? prefabDatabase.prefabs.Count.ToString() : "Missing");
            EditorGUILayout.LabelField("Asset Catalog Entries", assetCatalog != null ? assetCatalog.assets.Count.ToString() : "Missing");
            EditorGUILayout.LabelField("Last Import", report != null ? report.importedAtUtc : "No report yet");
        }

        private static void DrawActions()
        {
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Actions", EditorStyles.boldLabel);

            if (GUILayout.Button("Import Real CC0 3D City Packs"))
            {
                Real3DAssetPackImporter.ImportRealCc0CityPacks();
            }

            if (GUILayout.Button("Copy Selected ZIPs To Archive Folder"))
            {
                Real3DAssetPackImporter.CopySelectedZipsToArchiveFolder();
            }

            if (GUILayout.Button("Rebuild Databases From Existing Prefabs"))
            {
                Real3DAssetPackImporter.RebuildRealDatabasesFromExistingPrefabs();
            }

            if (GUILayout.Button("Validate Real Prefab Database"))
            {
                Real3DAssetPackImporter.ValidateRealPrefabDatabase();
            }

            if (GUILayout.Button("Assign Real Assets To Selected/Scene WorldGenerators"))
            {
                Real3DAssetPackImporter.AssignRealAssetsToSelectedWorldGenerators();
            }

            if (GUILayout.Button("Export Import Report CSV"))
            {
                Real3DAssetPackImporter.ExportImportReportCsv();
            }

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Open Sources"))
            {
                Real3DAssetPackImporter.OpenRealAssetSources();
            }

            if (GUILayout.Button("Reveal Folder"))
            {
                Real3DAssetPackImporter.RevealRealAssetFolder();
            }
            EditorGUILayout.EndHorizontal();

            if (GUILayout.Button("Clean Generated Outputs (Keep Archives)"))
            {
                Real3DAssetPackImporter.CleanGeneratedRealAssetOutputs();
            }
        }

        private static void DrawReport()
        {
            var report = Real3DAssetPackImporter.LoadImportReport();
            if (report == null)
            {
                return;
            }

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Latest Import Report", EditorStyles.boldLabel);
            EditorGUILayout.LabelField("Total Models", report.totalModelCount.ToString());
            EditorGUILayout.LabelField("Total Prefabs", report.totalPrefabCount.ToString());

            foreach (var pack in report.packs)
            {
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                EditorGUILayout.LabelField(pack.displayName, EditorStyles.boldLabel);
                EditorGUILayout.LabelField("Status", pack.status);
                EditorGUILayout.LabelField("Models / Prefabs", $"{pack.modelCount} / {pack.prefabCount}");
                if (!string.IsNullOrEmpty(pack.warning))
                {
                    EditorGUILayout.HelpBox(pack.warning, MessageType.Warning);
                }
                EditorGUILayout.EndVertical();
            }
        }
    }
}
