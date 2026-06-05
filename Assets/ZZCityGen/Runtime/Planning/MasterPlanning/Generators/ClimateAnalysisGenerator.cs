using System;
using System.Collections.Generic;
using UnityEngine;

namespace ZZCityGen.Planning.MasterPlanning.Generators
{
    /// <summary>
    /// المرحلة 2.3: تحديد المناخ العالمي
    /// Generates climate zones and analysis
    /// </summary>
    public class ClimateAnalysisGenerator
    {
        private readonly System.Random random;

        public ClimateAnalysisGenerator(int seed)
        {
            this.random = new System.Random(seed);
        }

        /// <summary>
        /// توليد مناطق مناخية
        /// مثال:
        ///   - 40% متوسطي
        ///   - 30% غابات
        ///   - 30% سهول
        /// </summary>
        public ClimateAnalysisData GenerateClimateAnalysis(WorldBoundsData bounds, TerrainAnalysisData terrain)
        {
            var analysis = new ClimateAnalysisData();

            float centerX = (bounds.minX + bounds.maxX) / 2f;
            float centerZ = (bounds.minZ + bounds.maxZ) / 2f;
            float worldWidth = bounds.maxX - bounds.minX;
            float worldHeight = bounds.maxZ - bounds.minZ;

            // ============================================
            // توزيع المناخ حسب الموقع والارتفاع
            // ============================================

            // 1. مناخ معتدل (الوسط العلوي) - 35%
            var temperate = new ClimateRegionData(
                ClimateType.Temperate,
                new Vector2(centerX, centerZ + worldHeight * 0.15f),
                worldWidth * 0.4f,
                0.35f,
                15f,        // متوسط درجة الحرارة
                800f,       // معدل الأمطار
                180f        // فترة النمو
            );
            analysis.regions.Add(temperate);

            // 2. مناخ متوسطي (الجنوب الأوسط) - 30%
            var mediterranean = new ClimateRegionData(
                ClimateType.Mediterranean,
                new Vector2(centerX, centerZ - worldHeight * 0.1f),
                worldWidth * 0.35f,
                0.30f,
                18f,        // درجة حرارة أعلى
                550f,       // أمطار أقل
                220f        // فترة نمو أطول
            );
            analysis.regions.Add(mediterranean);

            // 3. مناخ استوائي (الشرق) - 15%
            var tropical = new ClimateRegionData(
                ClimateType.Tropical,
                new Vector2(centerX + worldWidth * 0.25f, centerZ),
                worldWidth * 0.25f,
                0.15f,
                25f,        // ساخن
                2000f,      // أمطار غزيرة
                365f        // نمو طول السنة
            );
            analysis.regions.Add(tropical);

            // 4. مناخ جبلي/ألبي (الشمال) - 12%
            var alpine = new ClimateRegionData(
                ClimateType.Alpine,
                new Vector2(centerX, bounds.maxZ * 0.7f),
                worldWidth * 0.25f,
                0.12f,
                5f,         // بارد جداً
                1200f,      // ثلج وأمطار
                60f         // فترة نمو قصيرة
            );
            analysis.regions.Add(alpine);

            // 5. مناخ صحراوي (الغرب) - 8%
            var arid = new ClimateRegionData(
                ClimateType.Arid,
                new Vector2(centerX - worldWidth * 0.25f, centerZ),
                worldWidth * 0.2f,
                0.08f,
                28f,        // ساخن جداً
                150f,       // جاف جداً
                90f         // فترة نمو قصيرة
            );
            analysis.regions.Add(arid);

            // ============================================
            // حساب الإحصائيات العامة
            // ============================================
            analysis.dominantClimate = ClimateType.Temperate;  // المناخ الأكثر انتشاراً
            analysis.agriculturePotentialPercentage = 
                (temperate.percentageOfWorld + mediterranean.percentageOfWorld + tropical.percentageOfWorld) * 100;

            // حساب متوسط درجة الحرارة العالمية
            analysis.globalAverageTemperature = 
                (temperate.averageTemperature * temperate.percentageOfWorld +
                 mediterranean.averageTemperature * mediterranean.percentageOfWorld +
                 tropical.averageTemperature * tropical.percentageOfWorld +
                 alpine.averageTemperature * alpine.percentageOfWorld +
                 arid.averageTemperature * arid.percentageOfWorld) / 
                (temperate.percentageOfWorld + mediterranean.percentageOfWorld + tropical.percentageOfWorld +
                 alpine.percentageOfWorld + arid.percentageOfWorld);

            Debug.Log("[ClimateAnalysisGenerator] Climate analysis complete:");
            Debug.Log($"  Dominant Climate: {analysis.dominantClimate}");
            Debug.Log($"  Agriculture Potential: {analysis.agriculturePotentialPercentage:F1}%");
            Debug.Log($"  Global Average Temperature: {analysis.globalAverageTemperature:F1}°C");

            return analysis;
        }

        /// <summary>
        /// البحث عن أفضل منطقة زراعية
        /// </summary>
        public ClimateRegionData GetBestAgriculturalRegion(ClimateAnalysisData analysis)
        {
            return analysis.GetMostAgriculturalRegion();
        }

        /// <summary>
        /// الحصول على جميع المناطق الصالحة للزراعة
        /// </summary>
        public List<ClimateRegionData> GetAgriculturalRegions(ClimateAnalysisData analysis)
        {
            var agricultural = new List<ClimateRegionData>();
            foreach (var region in analysis.regions)
            {
                if (region.isSuitableForAgriculture)
                    agricultural.Add(region);
            }
            return agricultural;
        }

        /// <summary>
        /// البحث عن مناطق معينة حسب النوع
        /// </summary>
        public List<ClimateRegionData> GetRegionsByType(ClimateAnalysisData analysis, ClimateType type)
        {
            var regions = new List<ClimateRegionData>();
            foreach (var region in analysis.regions)
            {
                if (region.type == type)
                    regions.Add(region);
            }
            return regions;
        }
    }
}
