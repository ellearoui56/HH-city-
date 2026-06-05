using System;
using UnityEngine;
using ZZCityGen.Planning.MasterPlanning;

namespace ZZCityGen.Planning.TerrainGeneration.Generators
{
    /// <summary>
    /// المرحلة 3.1: إنشاء HeightMap أساسي
    /// الخطوة الأولى: ننشئ شبكة ضخمة من الارتفاعات
    /// 
    /// مثال:
    /// 2048 x 2048 أو 4096 x 4096
    /// كل نقطة = Height Value
    ///   0 = بحر
    ///   100 = سهل
    ///   500 = تل
    ///   2000 = جبل
    /// </summary>
    public class BaseHeightMapGenerator
    {
        private readonly System.Random random;

        public BaseHeightMapGenerator(int seed)
        {
            this.random = new System.Random(seed);
        }

        /// <summary>
        /// توليد HeightMap أساسي بناءً على Master Plan
        /// </summary>
        public HeightMapData GenerateBaseHeightMap(
            MasterPlanData masterPlan,
            int resolution = 2048)
        {
            Debug.Log($"[BaseHeightMapGenerator] Generating base height map ({resolution}x{resolution})");

            // حساب حجم البكسل الواحد
            float worldSize = masterPlan.worldBounds.maxX - masterPlan.worldBounds.minX;
            float pixelSize = worldSize / resolution;

            var heightMap = new HeightMapData(resolution, resolution, pixelSize);

            // ملء HeightMap بقيم أساسية
            float seaLevel = -50f;

            for (int y = 0; y < resolution; y++)
            {
                for (int x = 0; x < resolution; x++)
                {
                    // تحويل إحداثيات الخريطة إلى إحداثيات العالم
                    float worldX = masterPlan.worldBounds.minX + (x + 0.5f) * pixelSize;
                    float worldZ = masterPlan.worldBounds.minZ + (y + 0.5f) * pixelSize;
                    var worldPos = new Vector2(worldX, worldZ);

                    // حساب الارتفاع الأساسي
                    float height = CalculateBaseHeight(worldPos, masterPlan, seaLevel);

                    heightMap.SetHeight(x, y, height);
                }
            }

            heightMap.UpdateStats();

            Debug.Log($"[BaseHeightMapGenerator] Height range: {heightMap.minElevation}m to {heightMap.maxElevation}m");

            return heightMap;
        }

        /// <summary>
        /// حساب الارتفاع الأساسي في موقع معين
        /// بناءً على تحليل التضاريس من Master Plan
        /// </summary>
        private float CalculateBaseHeight(Vector2 worldPos, MasterPlanData masterPlan, float seaLevel)
        {
            // ============================================
            // 1. البحث عن منطقة التضاريس في هذا الموقع
            // ============================================
            var zone = GetTerrainZoneAt(worldPos, masterPlan);

            if (zone == null)
                return seaLevel;

            // ============================================
            // 2. حساب الارتفاع بناءً على نوع المنطقة
            // ============================================
            float baseHeight = zone.averageHeightMeters;

            // ============================================
            // 3. إضافة تغيير عشوائي بسيط
            // ============================================
            float noise = (float)random.NextDouble() * 50 - 25;  // ±25 متر

            return baseHeight + noise;
        }

        /// <summary>
        /// الحصول على منطقة التضاريس في موقع معين
        /// </summary>
        private TerrainZoneData GetTerrainZoneAt(Vector2 position, MasterPlanData masterPlan)
        {
            TerrainZoneData closest = null;
            float minDistance = float.MaxValue;

            foreach (var zone in masterPlan.terrainAnalysis.zones)
            {
                float distance = Vector2.Distance(position, zone.center);
                if (distance < minDistance)
                {
                    minDistance = distance;
                    closest = zone;
                }
            }

            return closest;
        }

        /// <summary>
        /// استخدام Perlin Noise لإنشاء تضاريس أكثر طبيعية
        /// </summary>
        public void ApplyPerlinNoise(HeightMapData heightMap, float scale = 100f, float amplitude = 50f)
        {
            Debug.Log("[BaseHeightMapGenerator] Applying Perlin noise");

            float offsetX = (float)random.NextDouble() * 10000;
            float offsetZ = (float)random.NextDouble() * 10000;

            for (int y = 0; y < heightMap.height; y++)
            {
                for (int x = 0; x < heightMap.width; x++)
                {
                    float sampleX = (x + offsetX) / scale;
                    float sampleZ = (y + offsetZ) / scale;

                    float noise = Mathf.PerlinNoise(sampleX, sampleZ);
                    float noiseHeight = noise * amplitude;

                    float currentHeight = heightMap.GetHeight(x, y);
                    heightMap.SetHeight(x, y, currentHeight + noiseHeight);
                }
            }

            heightMap.UpdateStats();
        }

        /// <summary>
        /// تطبيق تمويه (Smoothing) لتقليل الفروقات الحادة
        /// </summary>
        public void SmoothHeightMap(HeightMapData heightMap, int iterations = 3)
        {
            Debug.Log($"[BaseHeightMapGenerator] Smoothing height map ({iterations} iterations)");

            for (int iter = 0; iter < iterations; iter++)
            {
                var smoothed = new float[heightMap.width * heightMap.height];

                for (int y = 0; y < heightMap.height; y++)
                {
                    for (int x = 0; x < heightMap.width; x++)
                    {
                        float sum = 0;
                        int count = 0;

                        // حساب المتوسط من 9 نقاط حول النقطة الحالية
                        for (int dy = -1; dy <= 1; dy++)
                        {
                            for (int dx = -1; dx <= 1; dx++)
                            {
                                int nx = x + dx;
                                int ny = y + dy;

                                if (nx >= 0 && nx < heightMap.width && ny >= 0 && ny < heightMap.height)
                                {
                                    sum += heightMap.GetHeight(nx, ny);
                                    count++;
                                }
                            }
                        }

                        smoothed[y * heightMap.width + x] = sum / count;
                    }
                }

                // نسخ البيانات الممسحة
                for (int i = 0; i < smoothed.Length; i++)
                {
                    heightMap.heightData[i] = smoothed[i];
                }
            }

            heightMap.UpdateStats();
        }
    }
}
