using UnityEngine;
using UnityEditor;
using System;
using System.Collections.Generic;
using ZZCityGen.Planning.MasterPlanning;

namespace ZZCityGen.Planning.TerrainGeneration.Editor
{
    /// <summary>
    /// نافذة محرر Unity لتوليد التضاريس
    /// </summary>
    public class TerrainGeneratorWindow : EditorWindow
    {
        private const string MENU_ITEM_PATH = "ZZCityGen/Terrain Generator";

        // متغيرات الحالة
        private int heightMapResolution = 2048;
        private int seed = 12345;
        private string generationStatus = "جاهز للبدء";
        private bool isGenerating = false;

        // متغيرات التحميل/الحفظ
        private string masterPlanPath = "";
        private string outputPath = "";

        // نتائج التوليد
        private TerrainGenerationData lastGeneratedTerrain = null;
        private TerrainAnalysisData lastGeneratedAnalysis = null;
        private List<string> generationLog = new List<string>();

        // القابلية للطي
        private bool showGenerationSettings = true;
        private bool showAdvancedSettings = false;
        private bool showResults = false;
        private bool showLog = false;

        // ألوان للواجهة
        private Color successColor = Color.green;
        private Color warningColor = Color.yellow;
        private Color errorColor = Color.red;

        [MenuItem(MENU_ITEM_PATH)]
        public static void ShowWindow()
        {
            GetWindow<TerrainGeneratorWindow>("Terrain Generator");
        }

        private void OnEnable()
        {
            masterPlanPath = System.IO.Path.Combine(Application.persistentDataPath, "MasterPlanData.json");
            outputPath = Application.persistentDataPath;
        }

        private void OnGUI()
        {
            GUILayout.Label("محرك توليد التضاريس - Terrain Generation Engine", EditorStyles.largeLabel);
            GUILayout.Space(10);

            // ============================================
            // قسم الإعدادات الأساسية
            // ============================================
            showGenerationSettings = EditorGUILayout.Foldout(showGenerationSettings, "إعدادات التوليد", true);
            if (showGenerationSettings)
            {
                EditorGUI.indentLevel++;

                // دقة HeightMap
                GUILayout.Label("دقة HeightMap (الأداء vs الجودة)", EditorStyles.boldLabel);
                int[] resolutionOptions = { 512, 1024, 2048, 4096 };
                string[] resolutionLabels = { "512x512 (سريع)", "1024x1024 (عادي)", "2048x2048 (جيد)", "4096x4096 (عالي)" };
                int selectedResolution = EditorGUILayout.Popup("الدقة", System.Array.IndexOf(resolutionOptions, heightMapResolution), resolutionLabels);
                if (selectedResolution >= 0)
                    heightMapResolution = resolutionOptions[selectedResolution];

                GUILayout.Space(5);

                // البذرة (Seed)
                seed = EditorGUILayout.IntField("البذرة (Seed)", seed);
                GUILayout.Label("نفس البذرة = نفس التضاريس دائماً", EditorStyles.miniLabel);

                GUILayout.Space(5);

                // المسارات
                GUILayout.Label("المسارات", EditorStyles.boldLabel);
                masterPlanPath = EditorGUILayout.TextField("مسار Master Plan:", masterPlanPath);
                outputPath = EditorGUILayout.TextField("مسار الحفظ:", outputPath);

                EditorGUI.indentLevel--;
                GUILayout.Space(10);
            }

            // ============================================
            // قسم الإعدادات المتقدمة
            // ============================================
            showAdvancedSettings = EditorGUILayout.Foldout(showAdvancedSettings, "الإعدادات المتقدمة", true);
            if (showAdvancedSettings)
            {
                EditorGUI.indentLevel++;
                GUILayout.Label("سيتم إضافة خيارات متقدمة قريباً", EditorStyles.miniLabel);
                EditorGUI.indentLevel--;
                GUILayout.Space(10);
            }

            // ============================================
            // الزر الرئيسي - التوليد
            // ============================================
            GUI.enabled = !isGenerating;
            GUI.backgroundColor = Color.cyan;

            if (GUILayout.Button("توليد التضاريس", GUILayout.Height(40)))
            {
                StartTerrainGeneration();
            }

            GUI.backgroundColor = Color.white;
            GUI.enabled = true;

            GUILayout.Space(10);

            // ============================================
            // عرض الحالة
            // ============================================
            GUILayout.Label("الحالة:", EditorStyles.boldLabel);

            // لون الحالة
            if (generationStatus.Contains("خطأ") || generationStatus.Contains("Error"))
                GUI.backgroundColor = errorColor;
            else if (generationStatus.Contains("تحذير") || generationStatus.Contains("Warning"))
                GUI.backgroundColor = warningColor;
            else if (generationStatus.Contains("نجح") || generationStatus.Contains("Complete"))
                GUI.backgroundColor = successColor;

            EditorGUILayout.TextArea(generationStatus, GUILayout.Height(60));
            GUI.backgroundColor = Color.white;

            GUILayout.Space(10);

            // ============================================
            // النتائج
            // ============================================
            showResults = EditorGUILayout.Foldout(showResults, "النتائج", true);
            if (showResults && lastGeneratedTerrain != null)
            {
                EditorGUI.indentLevel++;

                GUILayout.Label("HeightMap", EditorStyles.boldLabel);
                GUILayout.Label($"الدقة: {lastGeneratedTerrain.heightMap.width}x{lastGeneratedTerrain.heightMap.height}");
                GUILayout.Label($"حجم البكسل: {lastGeneratedTerrain.heightMap.pixelSizeMeters}m");
                GUILayout.Label($"نطاق الارتفاع: {lastGeneratedTerrain.heightMap.minElevation}m - {lastGeneratedTerrain.heightMap.maxElevation}m");

                GUILayout.Space(5);

                GUILayout.Label("المميزات المائية", EditorStyles.boldLabel);
                GUILayout.Label($"الأنهار: {lastGeneratedTerrain.rivers.Count}");
                GUILayout.Label($"البحيرات: {lastGeneratedTerrain.lakes.Count}");
                GUILayout.Label($"نقاط الساحل: {lastGeneratedTerrain.coastline.coastlinePoints.Count}");

                GUILayout.Space(5);

                GUILayout.Label("الغطاء النباتي", EditorStyles.boldLabel);
                GUILayout.Label($"مناطق الغابات: {lastGeneratedTerrain.forests.Count}");

                GUILayout.Space(5);

                GUILayout.Label("الإحصائيات", EditorStyles.boldLabel);
                GUILayout.Label($"قابلية البناء: {lastGeneratedTerrain.avgBuildability:P}");
                GUILayout.Label($"صلاحية الطريق: {lastGeneratedTerrain.avgRoadFriendliness:P}");
                GUILayout.Label($"تغطية الماء: {lastGeneratedTerrain.waterCoverage:P}");
                GUILayout.Label($"تغطية الغابات: {lastGeneratedTerrain.forestCoverage:P}");
                GUILayout.Label($"تغطية الجبال: {lastGeneratedTerrain.mountainCoverage:P}");

                EditorGUI.indentLevel--;
                GUILayout.Space(10);
            }

            // ============================================
            // السجل
            // ============================================
            showLog = EditorGUILayout.Foldout(showLog, "السجل", true);
            if (showLog)
            {
                EditorGUI.indentLevel++;

                // شريط التمرير للسجل
                GUILayout.Label($"السجل ({generationLog.Count} رسائل)");
                foreach (string logEntry in generationLog)
                {
                    EditorGUILayout.SelectableLabel(logEntry, GUILayout.Height(20));
                }

                EditorGUI.indentLevel--;
            }

            // تحديث الواجهة في الوقت الفعلي
            Repaint();
        }

        private void StartTerrainGeneration()
        {
            isGenerating = true;
            generationLog.Clear();
            generationStatus = "جاري التوليد...";

            try
            {
                // تحميل Master Plan
                AddLog("جاري تحميل Master Plan...");
                var masterPlan = MasterPlanSaveLoadUtility.LoadMasterPlanData(masterPlanPath);

                if (masterPlan == null)
                {
                    throw new Exception("فشل تحميل Master Plan. تحقق من المسار.");
                }

                AddLog($"✓ Master Plan محمل بنجاح ({masterPlan.cities.Count} مدن)");

                // توليد التضاريس
                AddLog($"جاري توليد التضاريس بدقة {heightMapResolution}x{heightMapResolution}...");
                var builder = new TerrainGenerationBuilder(seed, masterPlan);
                lastGeneratedTerrain = builder.BuildTerrain(heightMapResolution);

                if (lastGeneratedTerrain == null)
                {
                    throw new Exception("فشل توليد التضاريس");
                }

                AddLog("✓ التضاريس تم توليدها بنجاح");

                // الحفظ
                AddLog("جاري حفظ البيانات...");
                TerrainSaveLoadUtility.SaveTerrainData(lastGeneratedTerrain, outputPath);
                AddLog("✓ البيانات محفوظة بنجاح");

                generationStatus = $"✓ التوليد نجح!\n\n" +
                    $"الارتفاع: {lastGeneratedTerrain.heightMap.minElevation}m - {lastGeneratedTerrain.heightMap.maxElevation}m\n" +
                    $"الأنهار: {lastGeneratedTerrain.rivers.Count} | البحيرات: {lastGeneratedTerrain.lakes.Count}\n" +
                    $"قابلية البناء: {lastGeneratedTerrain.avgBuildability:P}";

                showResults = true;
            }
            catch (Exception ex)
            {
                AddLog($"✗ خطأ: {ex.Message}");
                generationStatus = $"✗ خطأ في التوليد:\n{ex.Message}";
            }
            finally
            {
                isGenerating = false;
            }
        }

        private void AddLog(string message)
        {
            generationLog.Add($"[{DateTime.Now:HH:mm:ss}] {message}");
            Debug.Log($"[TerrainGenerator] {message}");
        }
    }
}
