using System;
using UnityEngine;
using System.IO;
using ZZCityGen.Planning.MasterPlanning;

namespace ZZCityGen.Planning.TerrainGeneration
{
    /// <summary>
    /// أمثلة عملية لاستخدام محرك توليد التضاريس
    /// </summary>
    public class TerrainGenerationUsageExample
    {
        /// <summary>
        /// مثال 1: الاستخدام الأساسي
        /// توليد تضاريس من Master Plan بسهولة
        /// </summary>
        public static void Example1_BasicUsage()
        {
            Debug.Log("========== EXAMPLE 1: Basic Usage ==========");

            // تحميل Master Plan
            string masterPlanPath = Path.Combine(Application.persistentDataPath, "MasterPlanData.json");
            var masterPlan = MasterPlanSaveLoadUtility.LoadMasterPlanData(masterPlanPath);

            if (masterPlan == null)
            {
                Debug.LogError("Master plan not found!");
                return;
            }

            // توليد التضاريس
            int seed = 12345;
            int resolution = 2048;

            var builder = new TerrainGenerationBuilder(seed, masterPlan);
            var terrainData = builder.BuildTerrain(resolution);

            if (terrainData != null)
            {
                // حفظ النتائج
                TerrainSaveLoadUtility.SaveTerrainData(terrainData, Application.persistentDataPath);
                Debug.Log("[Example 1] Terrain generation successful!");
            }
        }

        /// <summary>
        /// مثال 2: التحكم بدقة HeightMap
        /// دقات مختلفة تؤثر على التفاصيل والأداء
        /// </summary>
        public static void Example2_DifferentResolutions()
        {
            Debug.Log("========== EXAMPLE 2: Different Resolutions ==========");

            string masterPlanPath = Path.Combine(Application.persistentDataPath, "MasterPlanData.json");
            var masterPlan = MasterPlanSaveLoadUtility.LoadMasterPlanData(masterPlanPath);

            if (masterPlan == null) return;

            // اختبار دقات مختلفة
            int[] resolutions = { 512, 1024, 2048, 4096 };

            foreach (int resolution in resolutions)
            {
                Debug.Log($"[Example 2] Generating terrain at {resolution}x{resolution}...");

                var builder = new TerrainGenerationBuilder(12345, masterPlan);
                var terrainData = builder.BuildTerrain(resolution);

                if (terrainData != null)
                {
                    Debug.LogFormat("[Example 2] {0}x{0} - Elevation: {1}m to {2}m, Buildability: {3:P}",
                        resolution,
                        terrainData.heightMap.minElevation,
                        terrainData.heightMap.maxElevation,
                        terrainData.avgBuildability
                    );
                }
            }
        }

        /// <summary>
        /// مثال 3: الوصول إلى بيانات التضاريس
        /// استخراج المعلومات المحددة للتحليل
        /// </summary>
        public static void Example3_AccessingTerrainData()
        {
            Debug.Log("========== EXAMPLE 3: Accessing Terrain Data ==========");

            string terrainPath = TerrainSaveLoadUtility.GetTerrainDataPath();
            var terrainData = TerrainSaveLoadUtility.LoadTerrainData(terrainPath);

            if (terrainData == null) return;

            // ============================================
            // الوصول إلى HeightMap
            // ============================================
            Debug.LogFormat("[Example 3] HeightMap: {0}x{1} pixels, {2}m per pixel",
                terrainData.heightMap.width,
                terrainData.heightMap.height,
                terrainData.heightMap.pixelSizeMeters
            );

            // ============================================
            // الوصول إلى المناطق المحمية
            // ============================================
            Debug.Log("[Example 3] Protected zones:");
            foreach (var zone in terrainData.protectedZones)
            {
                Debug.LogFormat("  - Zone at {0}: radius {1}m", zone.center, zone.radiusMeters);
            }

            // ============================================
            // الوصول إلى الأنهار
            // ============================================
            Debug.Log("[Example 3] Rivers:");
            foreach (var river in terrainData.rivers)
            {
                Debug.LogFormat("  - {0}: {1} points, {2}m wide",
                    river.name,
                    river.pathPoints.Count,
                    river.width
                );
            }

            // ============================================
            // الوصول إلى البحيرات
            // ============================================
            Debug.Log("[Example 3] Lakes:");
            foreach (var lake in terrainData.lakes)
            {
                Debug.LogFormat("  - {0}: center at {1}, radius {2}m, type: {3}",
                    lake.name,
                    lake.center,
                    lake.radiusMeters,
                    lake.lakeType
                );
            }

            // ============================================
            // الوصول إلى الغابات
            // ============================================
            Debug.Log("[Example 3] Forests:");
            foreach (var forest in terrainData.forests)
            {
                Debug.LogFormat("  - {0}: center at {1}, density {2:P}",
                    forest.name,
                    forest.center,
                    forest.density
                );
            }
        }

        /// <summary>
        /// مثال 4: تحليل خصائص الأرض
        /// حساب خصائص معينة لمواقع محددة
        /// </summary>
        public static void Example4_LandAnalysis()
        {
            Debug.Log("========== EXAMPLE 4: Land Analysis ==========");

            string terrainPath = TerrainSaveLoadUtility.GetTerrainDataPath();
            var terrainData = TerrainSaveLoadUtility.LoadTerrainData(terrainPath);

            if (terrainData == null) return;

            // ============================================
            // حساب الإحصائيات العامة
            // ============================================
            Debug.LogFormat("[Example 4] Overall Statistics:");
            Debug.LogFormat("  - Water coverage: {0:P}", terrainData.waterCoverage);
            Debug.LogFormat("  - Mountain coverage: {0:P}", terrainData.mountainCoverage);
            Debug.LogFormat("  - Forest coverage: {0:P}", terrainData.forestCoverage);
            Debug.LogFormat("  - Avg buildability: {0:P}", terrainData.avgBuildability);
            Debug.LogFormat("  - Avg road friendliness: {0:P}", terrainData.avgRoadFriendliness);

            // ============================================
            // تحليل ارتفاع معين
            // ============================================
            int testX = terrainData.heightMap.width / 2;
            int testY = terrainData.heightMap.height / 2;
            float testHeight = terrainData.heightMap.GetHeight(testX, testY);

            Debug.LogFormat("[Example 4] Point analysis at ({0}, {1}):", testX, testY);
            Debug.LogFormat("  - Height: {0}m", testHeight);

            // ============================================
            // تحليل منطقة معينة
            // ============================================
            int regionSize = 100;
            float avgRegionHeight = 0;
            int regionPixels = 0;

            for (int y = testY - regionSize; y < testY + regionSize; y++)
            {
                for (int x = testX - regionSize; x < testX + regionSize; x++)
                {
                    if (x >= 0 && x < terrainData.heightMap.width && y >= 0 && y < terrainData.heightMap.height)
                    {
                        avgRegionHeight += terrainData.heightMap.GetHeight(x, y);
                        regionPixels++;
                    }
                }
            }

            if (regionPixels > 0)
            {
                avgRegionHeight /= regionPixels;
                Debug.LogFormat("[Example 4] Region analysis: avg height {0}m over {1}x{1} pixels", 
                    avgRegionHeight, regionSize * 2);
            }
        }

        /// <summary>
        /// مثال 5: المقارنة بين بذور مختلفة
        /// نفس Master Plan، بذور مختلفة = تضاريس مختلفة
        /// </summary>
        public static void Example5_DifferentSeeds()
        {
            Debug.Log("========== EXAMPLE 5: Different Seeds ==========");

            string masterPlanPath = Path.Combine(Application.persistentDataPath, "MasterPlanData.json");
            var masterPlan = MasterPlanSaveLoadUtility.LoadMasterPlanData(masterPlanPath);

            if (masterPlan == null) return;

            int[] seeds = { 111, 222, 333, 444, 555 };
            int resolution = 1024;

            Debug.Log("[Example 5] Generating terrain with different seeds:");

            foreach (int seed in seeds)
            {
                var builder = new TerrainGenerationBuilder(seed, masterPlan);
                var terrainData = builder.BuildTerrain(resolution);

                if (terrainData != null)
                {
                    Debug.LogFormat("[Example 5] Seed {0}:",
                        seed
                    );
                    Debug.LogFormat("  - Elevation range: {0}m - {1}m",
                        terrainData.heightMap.minElevation,
                        terrainData.heightMap.maxElevation
                    );
                    Debug.LogFormat("  - Rivers: {0}, Lakes: {1}, Forests: {2}",
                        terrainData.rivers.Count,
                        terrainData.lakes.Count,
                        terrainData.forests.Count
                    );
                }
            }
        }

        /// <summary>
        /// مثال 6: معالجة الأخطاء
        /// التعامل مع الحالات الخاصة والأخطاء المحتملة
        /// </summary>
        public static void Example6_ErrorHandling()
        {
            Debug.Log("========== EXAMPLE 6: Error Handling ==========");

            // محاولة تحميل Master Plan غير الموجود
            string invalidPath = Path.Combine(Application.persistentDataPath, "NonExistent.json");
            var masterPlan = MasterPlanSaveLoadUtility.LoadMasterPlanData(invalidPath);

            if (masterPlan == null)
            {
                Debug.LogWarning("[Example 6] Master plan load failed (expected)");
            }

            // المحاولة مجدداً بمسار صحيح
            string correctPath = Path.Combine(Application.persistentDataPath, "MasterPlanData.json");
            masterPlan = MasterPlanSaveLoadUtility.LoadMasterPlanData(correctPath);

            if (masterPlan != null)
            {
                // يمكن المتابعة
                Debug.Log("[Example 6] Master plan loaded successfully");

                var builder = new TerrainGenerationBuilder(999, masterPlan);
                var terrainData = builder.BuildTerrain(1024);

                if (terrainData != null)
                {
                    Debug.Log("[Example 6] Terrain generation successful");
                }
                else
                {
                    Debug.LogError("[Example 6] Terrain generation failed");
                }
            }
        }

        /// <summary>
        /// مثال 7: سير عمل كامل من البداية
        /// من Master Plan إلى التضاريس المحفوظة
        /// </summary>
        public static void Example7_CompleteWorkflow()
        {
            Debug.Log("========== EXAMPLE 7: Complete Workflow ==========");

            // الخطوة 1: تحميل Master Plan
            Debug.Log("[Example 7] Step 1: Loading master plan...");
            string masterPlanPath = Path.Combine(Application.persistentDataPath, "MasterPlanData.json");
            var masterPlan = MasterPlanSaveLoadUtility.LoadMasterPlanData(masterPlanPath);

            if (masterPlan == null)
            {
                Debug.LogError("[Example 7] Failed to load master plan");
                return;
            }

            Debug.Log($"[Example 7] Master plan loaded: {masterPlan.cities.Count} cities, world size {masterPlan.worldBounds.areaSquareKm:F2} km²");

            // الخطوة 2: توليد التضاريس
            Debug.Log("[Example 7] Step 2: Generating terrain...");
            var builder = new TerrainGenerationBuilder(seed: 42, masterPlan);
            var terrainData = builder.BuildTerrain(heightMapResolution: 2048);

            if (terrainData == null)
            {
                Debug.LogError("[Example 7] Failed to generate terrain");
                return;
            }

            // الخطوة 3: التحقق والتصحيح
            Debug.Log("[Example 7] Step 3: Validating terrain...");
            var validatorCorrector = new TerrainValidatorAndCorrector();
            var validation = validatorCorrector.ValidateTerrainData(
                terrainData,
                masterPlan,
                new Vector2(masterPlan.worldBounds.minX, masterPlan.worldBounds.minZ),
                terrainData.heightMap.pixelSizeMeters
            );

            if (!validation.isValid)
            {
                Debug.LogWarning($"[Example 7] Found {validation.errors.Count} errors, attempting correction...");
                validatorCorrector.CorrectTerrainIssues(
                    terrainData,
                    terrainData.heightMap,
                    new Vector2(masterPlan.worldBounds.minX, masterPlan.worldBounds.minZ),
                    terrainData.heightMap.pixelSizeMeters
                );
            }

            // الخطوة 4: حفظ النتائج
            Debug.Log("[Example 7] Step 4: Saving terrain data...");
            TerrainSaveLoadUtility.SaveTerrainData(terrainData, Application.persistentDataPath);

            // الخطوة 5: طباعة الملخص
            Debug.Log("[Example 7] Step 5: Summary");
            Debug.LogFormat("[Example 7] ✓ Terrain generation complete!");
            Debug.LogFormat("[Example 7] ✓ Elevation: {0}m to {1}m",
                terrainData.heightMap.minElevation,
                terrainData.heightMap.maxElevation
            );
            Debug.LogFormat("[Example 7] ✓ Rivers: {0} | Lakes: {1} | Forests: {2}",
                terrainData.rivers.Count,
                terrainData.lakes.Count,
                terrainData.forests.Count
            );
            Debug.LogFormat("[Example 7] ✓ Buildability: {0:P} | Water: {1:P} | Mountains: {2:P}",
                terrainData.avgBuildability,
                terrainData.waterCoverage,
                terrainData.mountainCoverage
            );
        }
    }
}
