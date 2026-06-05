using System;
using System.Collections.Generic;
using UnityEngine;

namespace ZZCityGen.Planning.MasterPlanning
{
    /// <summary>
    /// أنواع المناخ الرئيسية
    /// Main climate types
    /// </summary>
    public enum ClimateType
    {
        Temperate,      // معتدل
        Tropical,       // استوائي
        Arid,           // صحراوي
        Mediterranean,  // متوسطي
        Alpine,         // جبلي
        Tundra          // تندرا
    }

    /// <summary>
    /// منطقة مناخية محددة
    /// A defined climate region
    /// </summary>
    [Serializable]
    public class ClimateRegionData
    {
        public ClimateType type;
        public Vector2 center;
        public float radiusMeters;
        
        /// <summary>
        /// النسبة المئوية للعالم
        /// </summary>
        public float percentageOfWorld;
        
        /// <summary>
        /// متوسط درجة الحرارة السنوية (مئوية)
        /// </summary>
        public float averageTemperature;
        
        /// <summary>
        /// معدل الأمطار السنوي (ملم)
        /// </summary>
        public float annualRainfallMm;
        
        /// <summary>
        /// فترة النمو (أيام)
        /// </summary>
        public float growingSeasonDays;

        public ClimateRegionData() { }

        public ClimateRegionData(ClimateType type, Vector2 center, float radiusMeters,
                                float percentage, float temperature, float rainfall, float season)
        {
            this.type = type;
            this.center = center;
            this.radiusMeters = radiusMeters;
            this.percentageOfWorld = percentage;
            this.averageTemperature = temperature;
            this.annualRainfallMm = rainfall;
            this.growingSeasonDays = season;
        }

        /// <summary>
        /// هل هذا المناخ مناسب للزراعة؟
        /// </summary>
        public bool isSuitableForAgriculture => 
            type != ClimateType.Arid && 
            type != ClimateType.Tundra &&
            growingSeasonDays > 120;
    }

    /// <summary>
    /// تحليل المناخ الكامل للعالم
    /// Complete climate analysis
    /// </summary>
    [Serializable]
    public class ClimateAnalysisData
    {
        public List<ClimateRegionData> regions = new List<ClimateRegionData>();
        
        /// <summary>
        /// المناخ السائد
        /// </summary>
        public ClimateType dominantClimate;
        
        /// <summary>
        /// نسبة المناطق الزراعية الممكنة
        /// </summary>
        public float agriculturePotentialPercentage;
        
        /// <summary>
        /// متوسط درجة الحرارة العالمي
        /// </summary>
        public float globalAverageTemperature;

        public ClimateAnalysisData()
        {
            regions = new List<ClimateRegionData>();
        }

        public ClimateRegionData GetMostAgriculturalRegion()
        {
            ClimateRegionData best = null;
            float bestScore = -1;

            foreach (var region in regions)
            {
                if (!region.isSuitableForAgriculture) continue;

                // نقاط أفضل لفترات نمو أطول ومطر معتدل
                float score = region.growingSeasonDays * (1 - Mathf.Clamp01(Mathf.Abs(region.annualRainfallMm - 600) / 600));
                if (score > bestScore)
                {
                    bestScore = score;
                    best = region;
                }
            }

            return best;
        }
    }
}
