using System;
using System.Collections.Generic;
using UnityEngine;
using ZZCityGen.Planning.MasterPlanning;
using ZZCityGen.Planning.TerrainGeneration.Generators;

namespace ZZCityGen.Planning.TerrainGeneration
{
    /// <summary>
    /// محرك توليد التضاريس الرئيسي
    /// يعالج جميع المراحل الـ 13 من عملية التوليد
    /// 
    /// المراحل:
    /// 1. إنشاء HeightMap الأساسي (Perlin Noise)
    /// 2. حماية المدن (Flat Protected Zones)
    /// 3. إنشاء السهول (Plateaus)
    /// 4. إنشاء الجبال (Mountains)
    /// 5. إنشاء التلال (Hills) - الانتقالات السلسة
    /// 6. إنشاء السواحل (Coastlines)
    /// 7. إنشاء الأنهار (Rivers) - تابعة الانحدار الطبيعي
    /// 8. إنشاء البحيرات (Lakes)
    /// 9. تحديد مناطق الغابات (Forest Regions)
    /// 10. حساب قابلية البناء (Buildability Scores)
    /// 11. حساب خريطة الانحدارات (Slope Map)
    /// 12. إخراج بيانات TerrainData.json
    /// 13. نظام التصحيح الذاتي (Self-Correction)
    /// </summary>
    public class TerrainGenerationBuilder
    {
        private readonly int seed;
        private readonly MasterPlanData masterPlan;

        private BaseHeightMapGenerator heightMapGenerator;
        private CityProtectionZoneGenerator protectionGenerator;
        private PlateauMountainHillGenerator plateauGenerator;
        private RiverLakeCoastGenerator waterGenerator;
        private ForestGenerator forestGenerator;
        private BuildabilityAndSlopeAnalyzer analysisGenerator;
        private TerrainValidatorAndCorrector validatorCorrector;

        public TerrainGenerationBuilder(int seed, MasterPlanData masterPlan)
        {
            this.seed = seed;
            this.masterPlan = masterPlan;
            this.heightMapGenerator = new BaseHeightMapGenerator(seed);
            this.protectionGenerator = new CityProtectionZoneGenerator();
            this.plateauGenerator = new PlateauMountainHillGenerator(seed);
            this.waterGenerator = new RiverLakeCoastGenerator(seed);
            this.forestGenerator = new ForestGenerator(seed);
            this.analysisGenerator = new BuildabilityAndSlopeAnalyzer();
            this.validatorCorrector = new TerrainValidatorAndCorrector();
        }

        /// <summary>
        /// بناء كامل بيانات التضاريس بتسلسل المراحل الـ 13
        /// </summary>
        public TerrainGenerationData BuildTerrain(int heightMapResolution = 2048)
        {
            Debug.Log("========================================");
            Debug.Log("[TerrainGenerationBuilder] Starting terrain generation");
            Debug.Log($"[TerrainGenerationBuilder] Seed: {seed}");
            Debug.Log($"[TerrainGenerationBuilder] Resolution: {heightMapResolution}x{heightMapResolution}");
            Debug.Log("========================================");

            try
            {
                var terrainData = new TerrainGenerationData();

                // ============================================
                // المرحلة 1: HeightMap الأساسي
                // ============================================
                Debug.Log("[TerrainGenerationBuilder] Step 1/13: Base heightmap");
                terrainData.heightMap = heightMapGenerator.GenerateBaseHeightMap(
                    masterPlan,
                    heightMapResolution
                );

                // إضافة Perlin Noise للتفاصيل
                heightMapGenerator.ApplyPerlinNoise(terrainData.heightMap, scale: 100f, amplitude: 50f);
                heightMapGenerator.SmoothHeightMap(terrainData.heightMap, iterations: 2);

                // ============================================
                // المرحلة 2: المناطق المحمية
                // ============================================
                Debug.Log("[TerrainGenerationBuilder] Step 2/13: Protected zones");
                terrainData.protectedZones = protectionGenerator.GenerateProtectedZones(masterPlan);
                
                float pixelSize = (masterPlan.worldBounds.maxX - masterPlan.worldBounds.minX) / heightMapResolution;
                Vector2 worldBoundsMin = new Vector2(masterPlan.worldBounds.minX, masterPlan.worldBounds.minZ);

                protectionGenerator.ApplyProtectionToHeightMap(
                    terrainData.heightMap,
                    terrainData.protectedZones,
                    worldBoundsMin,
                    pixelSize
                );

                // ============================================
                // المرحلة 3: السهول
                // ============================================
                Debug.Log("[TerrainGenerationBuilder] Step 3/13: Plains");
                plateauGenerator.GeneratePlains(
                    terrainData.heightMap,
                    masterPlan,
                    worldBoundsMin,
                    pixelSize
                );

                // ============================================
                // المرحلة 4: الجبال
                // ============================================
                Debug.Log("[TerrainGenerationBuilder] Step 4/13: Mountains");
                plateauGenerator.GenerateMountains(
                    terrainData.heightMap,
                    masterPlan,
                    worldBoundsMin,
                    pixelSize
                );

                // ============================================
                // المرحلة 5: التلال
                // ============================================
                Debug.Log("[TerrainGenerationBuilder] Step 5/13: Hills");
                plateauGenerator.GenerateHills(
                    terrainData.heightMap,
                    masterPlan,
                    worldBoundsMin,
                    pixelSize
                );

                terrainData.heightMap.UpdateStats();

                // ============================================
                // المرحلة 6: السواحل
                // ============================================
                Debug.Log("[TerrainGenerationBuilder] Step 6/13: Coastlines");
                Vector2 worldBoundsMax = new Vector2(masterPlan.worldBounds.maxX, masterPlan.worldBounds.maxZ);
                terrainData.coastline = waterGenerator.GenerateCoastline(
                    terrainData.heightMap,
                    masterPlan,
                    worldBoundsMin,
                    pixelSize
                );

                // ============================================
                // المرحلة 7: الأنهار
                // ============================================
                Debug.Log("[TerrainGenerationBuilder] Step 7/13: Rivers");
                terrainData.rivers = waterGenerator.GenerateRivers(
                    terrainData.heightMap,
                    masterPlan,
                    worldBoundsMin,
                    pixelSize
                );

                // ============================================
                // المرحلة 8: البحيرات
                // ============================================
                Debug.Log("[TerrainGenerationBuilder] Step 8/13: Lakes");
                terrainData.lakes = waterGenerator.GenerateLakes(
                    terrainData.heightMap,
                    masterPlan,
                    worldBoundsMin,
                    pixelSize
                );

                // ============================================
                // المرحلة 9: مناطق الغابات
                // ============================================
                Debug.Log("[TerrainGenerationBuilder] Step 9/13: Forests");
                terrainData.forests = forestGenerator.GenerateForests(
                    masterPlan,
                    worldBoundsMin,
                    worldBoundsMax
                );

                // ============================================
                // إنشاء مصفوفة البيانات الإضافية
                // ============================================
                var pixelData = new HeightMapPixel[heightMapResolution, heightMapResolution];
                for (int y = 0; y < heightMapResolution; y++)
                {
                    for (int x = 0; x < heightMapResolution; x++)
                    {
                        pixelData[x, y] = new HeightMapPixel();
                    }
                }

                // ============================================
                // المرحلة 10: قابلية البناء
                // ============================================
                Debug.Log("[TerrainGenerationBuilder] Step 10/13: Buildability");
                analysisGenerator.CalculateBuildability(
                    terrainData.heightMap,
                    terrainData.protectedZones,
                    terrainData.forests,
                    terrainData.lakes,
                    terrainData.rivers,
                    worldBoundsMin,
                    pixelSize,
                    pixelData
                );

                // ============================================
                // المرحلة 11: خريطة الانحدارات
                // ============================================
                Debug.Log("[TerrainGenerationBuilder] Step 11/13: Slopes");
                analysisGenerator.CalculateSlopes(
                    terrainData.heightMap,
                    pixelSize,
                    pixelData
                );

                // ============================================
                // المرحلة 12: حساب الإحصائيات
                // ============================================
                Debug.Log("[TerrainGenerationBuilder] Step 12/13: Statistics");
                CalculateStatistics(terrainData, pixelData, heightMapResolution);

                // ============================================
                // المرحلة 13: التحقق والتصحيح
                // ============================================
                Debug.Log("[TerrainGenerationBuilder] Step 13/13: Validation & Correction");
                
                var validation = validatorCorrector.ValidateTerrainData(
                    terrainData,
                    masterPlan,
                    worldBoundsMin,
                    pixelSize
                );

                if (!validation.isValid)
                {
                    Debug.LogWarning("[TerrainGenerationBuilder] Validation errors found, attempting auto-correction");
                    validatorCorrector.CorrectTerrainIssues(
                        terrainData,
                        terrainData.heightMap,
                        worldBoundsMin,
                        pixelSize
                    );
                }

                Debug.Log("========================================");
                Debug.Log("[TerrainGenerationBuilder] Terrain generation COMPLETE");
                Debug.LogFormat("[TerrainGenerationBuilder] Water coverage: {0:P}", terrainData.waterCoverage);
                Debug.LogFormat("[TerrainGenerationBuilder] Mountain coverage: {0:P}", terrainData.mountainCoverage);
                Debug.LogFormat("[TerrainGenerationBuilder] Forest coverage: {0:P}", terrainData.forestCoverage);
                Debug.LogFormat("[TerrainGenerationBuilder] Avg buildability: {0:P}", terrainData.avgBuildability);
                Debug.LogFormat("[TerrainGenerationBuilder] Elevation range: {0}m to {1}m", 
                    terrainData.heightMap.minElevation, terrainData.heightMap.maxElevation);
                Debug.Log("========================================");

                return terrainData;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[TerrainGenerationBuilder] FAILED: {ex.Message}");
                Debug.LogError($"[TerrainGenerationBuilder] {ex.StackTrace}");
                return null;
            }
        }

        /// <summary>
        /// حساب إحصائيات التضاريس
        /// </summary>
        private void CalculateStatistics(TerrainGenerationData terrainData, HeightMapPixel[,] pixelData, int resolution)
        {
            float buildabilitySum = 0;
            float roadFriendlinessSum = 0;
            int waterCount = 0;
            int forestCount = 0;

            float seaLevel = -50f;

            for (int y = 0; y < resolution; y++)
            {
                for (int x = 0; x < resolution; x++)
                {
                    if (pixelData[x, y] == null) continue;

                    buildabilitySum += pixelData[x, y].buildability;
                    roadFriendlinessSum += pixelData[x, y].roadFriendliness;

                    float height = terrainData.heightMap.GetHeight(x, y);
                    if (height <= seaLevel)
                        waterCount++;

                    if (pixelData[x, y].isForest)
                        forestCount++;
                }
            }

            int totalPixels = resolution * resolution;
            terrainData.avgBuildability = buildabilitySum / totalPixels;
            terrainData.avgRoadFriendliness = roadFriendlinessSum / totalPixels;
            terrainData.waterCoverage = (float)waterCount / totalPixels;
            terrainData.forestCoverage = (float)forestCount / totalPixels;
        }
    }
}
