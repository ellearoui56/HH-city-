using System;
using System.Collections.Generic;
using UnityEngine;

namespace ZZCityGen.Planning.MasterPlanning
{
    /// <summary>
    /// أنواع التضاريس الأساسية
    /// Basic terrain types
    /// </summary>
    public enum TerrainZoneType
    {
        Mountain,      // جبال
        Hill,          // تلال
        Plateau,       // هضبة
        Flat,          // سهول
        Valley,        // وادي
        Water,         // ماء (بحار/بحيرات)
        Wetland        // أراضي رطبة
    }

    /// <summary>
    /// منطقة تضاريس محددة في العالم
    /// A defined terrain zone in the world
    /// </summary>
    [Serializable]
    public class TerrainZoneData
    {
        public TerrainZoneType type;
        public Vector2 center;
        public float radiusMeters;
        
        /// <summary>
        /// متوسط الارتفاع المتوقع بالمتر
        /// </summary>
        public float averageHeightMeters;
        
        /// <summary>
        /// الانحدار المتوسط (0-1)
        /// Average slope (0-1)
        /// </summary>
        public float averageSlope;
        
        /// <summary>
        /// النسبة المئوية للمنطقة المشغولة
        /// </summary>
        public float percentageOfWorld;

        /// <summary>
        /// هل هذه المنطقة صالحة للبناء؟
        /// </summary>
        public bool isBuildable => type != TerrainZoneType.Mountain && 
                                    type != TerrainZoneType.Water &&
                                    averageSlope < 0.3f;

        public TerrainZoneData() { }

        public TerrainZoneData(TerrainZoneType type, Vector2 center, float radiusMeters, 
                               float avgHeight, float slope, float percentage)
        {
            this.type = type;
            this.center = center;
            this.radiusMeters = radiusMeters;
            this.averageHeightMeters = avgHeight;
            this.averageSlope = slope;
            this.percentageOfWorld = percentage;
        }
    }

    /// <summary>
    /// تحليل كامل للتضاريس المستقبلية
    /// Complete terrain analysis
    /// </summary>
    [Serializable]
    public class TerrainAnalysisData
    {
        public List<TerrainZoneData> zones = new List<TerrainZoneData>();
        
        /// <summary>
        /// إجمالي مساحة البناء الممكنة (%)
        /// </summary>
        public float buildableAreaPercentage;
        
        /// <summary>
        /// متوسط الارتفاع الكلي
        /// </summary>
        public float averageWorldHeight;
        
        /// <summary>
        /// أعلى نقطة
        /// </summary>
        public float maxElevation;
        
        /// <summary>
        /// أقل نقطة
        /// </summary>
        public float minElevation;

        /// <summary>
        /// نسبة المناطق المائية
        /// </summary>
        public float waterPercentage;

        public TerrainAnalysisData()
        {
            zones = new List<TerrainZoneData>();
        }

        public TerrainZoneData GetMostBuildableZone()
        {
            TerrainZoneData best = null;
            float bestScore = -1;

            foreach (var zone in zones)
            {
                if (!zone.isBuildable) continue;

                // نقاط أفضل للمناطق المسطحة والمنخفضة الانحدار
                float score = (1 - zone.averageSlope) * zone.percentageOfWorld;
                if (score > bestScore)
                {
                    bestScore = score;
                    best = zone;
                }
            }

            return best;
        }
    }
}
