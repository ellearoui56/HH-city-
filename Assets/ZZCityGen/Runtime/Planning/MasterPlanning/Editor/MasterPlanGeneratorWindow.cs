using UnityEngine;
using ZZCityGen.Planning.MasterPlanning;

namespace ZZCityGen.Planning.MasterPlanning.Editor
{
    /// <summary>
    /// محرر يسمح بتوليد خطة التخطيط من واجهة Unity
    /// 
    /// الاستخدام:
    /// 1. انسخ هذا الملف إلى مجلد Editor
    /// 2. في Unity, اذهب إلى Window > Master Plan Generator
    /// 3. املأ الحقول واضغط "Generate Master Plan"
    /// </summary>
    public class MasterPlanGeneratorWindow : EditorWindow
    {
        private int seed = 12345;
        private string worldName = "New World";
        private float worldSizeKm = 200f;
        private MasterPlanData lastGeneratedPlan;
        private Vector2 scrollPosition;

        [MenuItem("Window/City Generator/Master Plan Generator")]
        public static void ShowWindow()
        {
            GetWindow<MasterPlanGeneratorWindow>("Master Plan");
        }

        private void OnGUI()
        {
            GUILayout.Label("Master Plan Generator - Stage 2", EditorStyles.boldLabel);
            GUILayout.Space(10);

            // ============================================
            // Input Section
            // ============================================
            GUILayout.Label("Configuration", EditorStyles.boldLabel);
            seed = EditorGUILayout.IntField("Seed", seed);
            worldName = EditorGUILayout.TextField("World Name", worldName);
            worldSizeKm = EditorGUILayout.FloatField("World Size (km)", worldSizeKm);

            GUILayout.Space(10);

            // ============================================
            // Generate Button
            // ============================================
            if (GUILayout.Button("Generate Master Plan", GUILayout.Height(40)))
            {
                GenerateMasterPlan();
            }

            GUILayout.Space(10);

            // ============================================
            // Results Section
            // ============================================
            if (lastGeneratedPlan != null)
            {
                GUILayout.Label("Generation Results", EditorStyles.boldLabel);
                DisplayResults();
            }
        }

        private void GenerateMasterPlan()
        {
            Debug.Log($"[MasterPlanGeneratorWindow] Generating master plan...");
            Debug.Log($"  Seed: {seed}");
            Debug.Log($"  World Name: {worldName}");
            Debug.Log($"  World Size: {worldSizeKm}km");

            var builder = new MasterPlanBuilder(seed, worldName, worldSizeKm);
            lastGeneratedPlan = builder.BuildMasterPlan();

            if (builder.IsValid())
            {
                Debug.Log("✅ Master plan generated successfully!");
                
                // Save to JSON
                SaveMasterPlanToJson();
            }
            else
            {
                Debug.LogError("❌ Master plan generation failed!");
            }
        }

        private void SaveMasterPlanToJson()
        {
            string json = JsonUtility.ToJson(lastGeneratedPlan, true);
            string filePath = "Assets/MasterPlan.json";
            System.IO.File.WriteAllText(filePath, json);
            Debug.Log($"✅ Saved to: {filePath}");
        }

        private void DisplayResults()
        {
            scrollPosition = GUILayout.BeginScrollView(scrollPosition);

            // ============================================
            // World Stats
            // ============================================
            GUILayout.Label("World Statistics", EditorStyles.boldLabel);
            GUILayout.Label($"Area: {lastGeneratedPlan.worldAreaSquareKm:F2} km²");
            GUILayout.Label($"Bounds: X[{lastGeneratedPlan.worldBounds.minX}-{lastGeneratedPlan.worldBounds.maxX}]");

            GUILayout.Space(5);

            // ============================================
            // Terrain Stats
            // ============================================
            GUILayout.Label("Terrain", EditorStyles.boldLabel);
            GUILayout.Label($"Buildable Area: {lastGeneratedPlan.terrainAnalysis.buildableAreaPercentage}%");
            GUILayout.Label($"Max Elevation: {lastGeneratedPlan.terrainAnalysis.maxElevation}m");
            GUILayout.Label($"Zones: {lastGeneratedPlan.terrainAnalysis.zones.Count}");

            GUILayout.Space(5);

            // ============================================
            // Climate Stats
            // ============================================
            GUILayout.Label("Climate", EditorStyles.boldLabel);
            GUILayout.Label($"Dominant: {lastGeneratedPlan.climateAnalysis.dominantClimate}");
            GUILayout.Label($"Agriculture: {lastGeneratedPlan.climateAnalysis.agriculturePotentialPercentage:F1}%");

            GUILayout.Space(5);

            // ============================================
            // Cities Stats
            // ============================================
            GUILayout.Label("Cities", EditorStyles.boldLabel);
            GUILayout.Label($"Total: {lastGeneratedPlan.totalCities}");
            foreach (var city in lastGeneratedPlan.cities)
            {
                GUILayout.Label($"  • {city.name} ({city.archetype}): {city.targetPopulation:N0}");
            }

            GUILayout.Space(5);

            // ============================================
            // Infrastructure Stats
            // ============================================
            GUILayout.Label("Infrastructure", EditorStyles.boldLabel);
            GUILayout.Label($"Airports: {lastGeneratedPlan.totalAirports}");
            GUILayout.Label($"Ports: {lastGeneratedPlan.totalPorts}");
            GUILayout.Label($"Planned Roads: {lastGeneratedPlan.totalPlannedRoadsKm:F2}km");

            GUILayout.Space(5);

            // ============================================
            // Population Stats
            // ============================================
            GUILayout.Label("Population", EditorStyles.boldLabel);
            GUILayout.Label($"Total: {lastGeneratedPlan.totalPlannedPopulation:N0}");
            GUILayout.Label($"Density: {lastGeneratedPlan.GetGlobalPopulationDensity():F2} people/km²");

            GUILayout.Space(5);

            // ============================================
            // Validation Stats
            // ============================================
            GUILayout.Label("Validation", EditorStyles.boldLabel);
            string validStatus = lastGeneratedPlan.validation.isValid ? "✅ Valid" : "❌ Invalid";
            GUILayout.Label($"Status: {validStatus}");
            GUILayout.Label($"Errors: {lastGeneratedPlan.validation.errors.Count}");
            GUILayout.Label($"Warnings: {lastGeneratedPlan.validation.warnings.Count}");

            if (lastGeneratedPlan.validation.errors.Count > 0)
            {
                GUILayout.Label("Errors:", EditorStyles.boldLabel);
                foreach (var error in lastGeneratedPlan.validation.errors)
                {
                    EditorGUILayout.HelpBox(error, MessageType.Error);
                }
            }

            if (lastGeneratedPlan.validation.warnings.Count > 0)
            {
                GUILayout.Label("Warnings:", EditorStyles.boldLabel);
                foreach (var warning in lastGeneratedPlan.validation.warnings)
                {
                    EditorGUILayout.HelpBox(warning, MessageType.Warning);
                }
            }

            GUILayout.EndScrollView();
        }
    }
}

#if !UNITY_EDITOR
// Fallback for non-editor code
public class EditorWindow : MonoBehaviour { }
public class EditorGUILayout
{
    public static int IntField(string label, int value) => value;
    public static string TextField(string label, string value) => value;
    public static float FloatField(string label, float value) => value;
}
public class EditorStyles
{
    public static GUIStyle boldLabel => GUI.skin.label;
}
#endif
