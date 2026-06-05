using System;
using System.Collections.Generic;
using UnityEngine;

namespace ZZCityGen.Planning.MasterPlanning.Generators
{
    /// <summary>
    /// المرحلة 2.2: تحليل التضاريس المستقبلية
    /// Analyzes and generates terrain zones
    /// </summary>
    public class TerrainAnalysisGenerator
    {
        private readonly System.Random random;

        public TerrainAnalysisGenerator(int seed)
        {
            this.random = new System.Random(seed);
        }

        /// <summary>
        /// توليد خريطة ارتفاعات مبدئية وتقسيم المناطق
        /// مثال: 
        ///   - الشمال = جبال
        ///   - الوسط = سهول
        ///   - الجنوب = ساحل
        /// </summary>
        public TerrainAnalysisData GenerateTerrainAnalysis(WorldBoundsData bounds)
        {
            var analysis = new TerrainAnalysisData();

            // حساب الأبعاد
            float worldWidth = bounds.maxX - bounds.minX;
            float worldHeight = bounds.maxZ - bounds.minZ;
            float centerX = (bounds.minX + bounds.maxX) / 2f;
            float centerZ = (bounds.minZ + bounds.maxZ) / 2f;

            // ============================================
            // 1. المناطق الجبلية (الشمال)
            // ============================================
            var mountains = new TerrainZoneData(
                TerrainZoneType.Mountain,
                new Vector2(centerX, bounds.maxZ * 0.7f),  // في الشمال
                worldWidth * 0.3f,
                2500,  // متوسط ارتفاع عالي
                0.6f,  // انحدار حاد
                0.15f  // 15% من العالم
            );
            analysis.zones.Add(mountains);

            // ============================================
            // 2. التلال العالية (شمال الوسط)
            // ============================================
            var hills = new TerrainZoneData(
                TerrainZoneType.Hill,
                new Vector2(centerX, centerZ + worldHeight * 0.2f),
                worldWidth * 0.4f,
                1200,  // متوسط ارتفاع معتدل
                0.35f,
                0.20f  // 20% من العالم
            );
            analysis.zones.Add(hills);

            // ============================================
            // 3. السهول المسطحة (الوسط)
            // ============================================
            var flatlands = new TerrainZoneData(
                TerrainZoneType.Flat,
                new Vector2(centerX, centerZ),
                worldWidth * 0.5f,
                150,   // ارتفاع منخفض
                0.05f, // انحدار خفيف جداً
                0.35f  // 35% من العالم (الأكبر)
            );
            analysis.zones.Add(flatlands);

            // ============================================
            // 4. الأودية (جنوب الوسط)
            // ============================================
            var valleys = new TerrainZoneData(
                TerrainZoneType.Valley,
                new Vector2(centerX, centerZ - worldHeight * 0.2f),
                worldWidth * 0.35f,
                50,
                0.15f,
                0.15f  // 15% من العالم
            );
            analysis.zones.Add(valleys);

            // ============================================
            // 5. السواحل والمناطق المائية (الجنوب)
            // ============================================
            var water = new TerrainZoneData(
                TerrainZoneType.Water,
                new Vector2(centerX, bounds.minZ * 0.8f),
                worldWidth * 0.25f,
                -50,   // ماء
                0.0f,
                0.15f  // 15% من العالم
            );
            analysis.zones.Add(water);

            // ============================================
            // حساب الإحصائيات العامة
            // ============================================
            analysis.buildableAreaPercentage = CalculateBuildableArea(analysis);
            analysis.waterPercentage = water.percentageOfWorld;
            analysis.maxElevation = mountains.averageHeightMeters;
            analysis.minElevation = water.averageHeightMeters;
            analysis.averageWorldHeight = (flatlands.averageHeightMeters * flatlands.percentageOfWorld +
                                          hills.averageHeightMeters * hills.percentageOfWorld +
                                          valleys.averageHeightMeters * valleys.percentageOfWorld) / 
                                         (flatlands.percentageOfWorld + hills.percentageOfWorld + valleys.percentageOfWorld);

            Debug.Log("[TerrainAnalysisGenerator] Terrain analysis complete:");
            Debug.Log($"  Buildable Area: {analysis.buildableAreaPercentage}%");
            Debug.Log($"  Water Coverage: {analysis.waterPercentage}%");
            Debug.Log($"  Max Elevation: {analysis.maxElevation}m");
            Debug.Log($"  Min Elevation: {analysis.minElevation}m");

            return analysis;
        }

        /// <summary>
        /// حساب نسبة المناطق الصالحة للبناء
        /// </summary>
        private float CalculateBuildableArea(TerrainAnalysisData analysis)
        {
            float buildable = 0;
            foreach (var zone in analysis.zones)
            {
                if (zone.isBuildable)
                    buildable += zone.percentageOfWorld;
            }
            return buildable;
        }

        /// <summary>
        /// البحث عن أفضل موقع للسهول المسطحة
        /// </summary>
        public TerrainZoneData GetBestFlatZone(TerrainAnalysisData analysis)
        {
            return analysis.GetMostBuildableZone();
        }

        /// <summary>
        /// الحصول على جميع مناطق البناء الممكنة
        /// </summary>
        public List<TerrainZoneData> GetAllBuildableZones(TerrainAnalysisData analysis)
        {
            var buildable = new List<TerrainZoneData>();
            foreach (var zone in analysis.zones)
            {
                if (zone.isBuildable)
                    buildable.Add(zone);
            }
            return buildable;
        }
    }
}
