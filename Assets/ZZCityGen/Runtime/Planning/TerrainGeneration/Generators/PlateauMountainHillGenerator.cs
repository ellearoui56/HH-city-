using System;
using UnityEngine;
using ZZCityGen.Planning.MasterPlanning;

namespace ZZCityGen.Planning.TerrainGeneration.Generators
{
    /// <summary>
    /// المراحل 3.3، 3.4، 3.5: إنشاء الطبقات الرئيسية
    /// - 3.3: السهول (Plateaus)
    /// - 3.4: الجبال (Mountains)
    /// - 3.5: التلال (Hills)
    /// </summary>
    public class PlateauMountainHillGenerator
    {
        private readonly System.Random random;

        public PlateauMountainHillGenerator(int seed)
        {
            this.random = new System.Random(seed);
        }

        /// <summary>
        /// المرحلة 3.3: إنشاء السهول
        /// معظم المدن تحتاج أرضاً مسطحة
        /// </summary>
        public void GeneratePlains(
            HeightMapData heightMap,
            MasterPlanData masterPlan,
            Vector2 worldBoundsMin,
            float pixelSize)
        {
            Debug.Log("[PlateauMountainHillGenerator] Generating plains");

            // البحث عن أفضل منطقة سهول من Master Plan
            var flatZone = masterPlan.terrainAnalysis.GetMostBuildableZone();
            if (flatZone == null)
                return;

            // كل مدينة تحتاج إلى سهول حولها
            for (int y = 0; y < heightMap.height; y++)
            {
                for (int x = 0; x < heightMap.width; x++)
                {
                    float worldX = worldBoundsMin.x + (x + 0.5f) * pixelSize;
                    float worldZ = worldBoundsMin.y + (y + 0.5f) * pixelSize;
                    var worldPos = new Vector2(worldX, worldZ);

                    // البحث عن أقرب مدينة
                    float minDistanceToCity = float.MaxValue;
                    if (masterPlan.capital != null)
                    {
                        minDistanceToCity = Mathf.Min(minDistanceToCity, 
                            Vector2.Distance(worldPos, masterPlan.capital.position));
                    }

                    foreach (var city in masterPlan.cities)
                    {
                        minDistanceToCity = Mathf.Min(minDistanceToCity,
                            Vector2.Distance(worldPos, city.position));
                    }

                    // السهول حول المدن
                    if (minDistanceToCity < 8000)  // 8 كم من أقرب مدينة
                    {
                        float height = flatZone.averageHeightMeters;
                        float noise = (float)random.NextDouble() * 20 - 10;
                        heightMap.SetHeight(x, y, height + noise);
                    }
                }
            }

            heightMap.UpdateStats();
        }

        /// <summary>
        /// المرحلة 3.4: إنشاء الجبال
        /// الجبال توضع في المناطق المحددة من Master Plan
        /// </summary>
        public void GenerateMountains(
            HeightMapData heightMap,
            MasterPlanData masterPlan,
            Vector2 worldBoundsMin,
            float pixelSize)
        {
            Debug.Log("[PlateauMountainHillGenerator] Generating mountains");

            var mountainZones = masterPlan.terrainAnalysis.zones.FindAll(z => z.type == TerrainZoneType.Mountain);

            foreach (var mountainZone in mountainZones)
            {
                // إنشاء جبل بشكل هرم
                for (int y = 0; y < heightMap.height; y++)
                {
                    for (int x = 0; x < heightMap.width; x++)
                    {
                        float worldX = worldBoundsMin.x + (x + 0.5f) * pixelSize;
                        float worldZ = worldBoundsMin.y + (y + 0.5f) * pixelSize;
                        var worldPos = new Vector2(worldX, worldZ);

                        float distanceToMountain = Vector2.Distance(worldPos, mountainZone.center);

                        if (distanceToMountain < mountainZone.radiusMeters)
                        {
                            // الارتفاع يقل كلما ابتعدنا عن مركز الجبل
                            float ratio = 1 - (distanceToMountain / mountainZone.radiusMeters);
                            float height = mountainZone.averageHeightMeters * ratio * ratio;  // شكل هرمي

                            // إضافة تفاصيل صخرية
                            float noise = Mathf.PerlinNoise(
                                worldX / 1000f + mountainZone.center.x / 1000f,
                                worldZ / 1000f + mountainZone.center.y / 1000f
                            ) * 200 - 100;

                            float currentHeight = heightMap.GetHeight(x, y);
                            heightMap.SetHeight(x, y, Mathf.Max(currentHeight, height + noise));
                        }
                    }
                }
            }

            heightMap.UpdateStats();
        }

        /// <summary>
        /// المرحلة 3.5: إنشاء التلال
        /// التلال تربط السهول بالجبال بشكل سلس
        /// </summary>
        public void GenerateHills(
            HeightMapData heightMap,
            MasterPlanData masterPlan,
            Vector2 worldBoundsMin,
            float pixelSize)
        {
            Debug.Log("[PlateauMountainHillGenerator] Generating hills");

            var hillZones = masterPlan.terrainAnalysis.zones.FindAll(z => z.type == TerrainZoneType.Hill);

            foreach (var hillZone in hillZones)
            {
                for (int y = 0; y < heightMap.height; y++)
                {
                    for (int x = 0; x < heightMap.width; x++)
                    {
                        float worldX = worldBoundsMin.x + (x + 0.5f) * pixelSize;
                        float worldZ = worldBoundsMin.y + (y + 0.5f) * pixelSize;
                        var worldPos = new Vector2(worldX, worldZ);

                        float distanceToHill = Vector2.Distance(worldPos, hillZone.center);

                        if (distanceToHill < hillZone.radiusMeters)
                        {
                            // التلال أقل من الجبال وأعلى من السهول
                            float ratio = 1 - (distanceToHill / hillZone.radiusMeters);
                            float height = hillZone.averageHeightMeters * ratio;

                            // تفاصيل تلول
                            float noise = Mathf.PerlinNoise(
                                worldX / 500f,
                                worldZ / 500f
                            ) * 100 - 50;

                            float currentHeight = heightMap.GetHeight(x, y);
                            
                            // فقط إذا كانت التلال أعلى من الارتفاع الحالي
                            if (height + noise > currentHeight)
                            {
                                heightMap.SetHeight(x, y, height + noise);
                            }
                        }
                    }
                }
            }

            heightMap.UpdateStats();
        }

        /// <summary>
        /// حساب الانحدار في كل نقطة
        /// يُستخدم لاحقاً في تحليل الطرق
        /// </summary>
        public void CalculateSlopes(
            HeightMapData heightMap,
            float pixelSizeMeters,
            HeightMapPixel[,] pixelData)
        {
            Debug.Log("[PlateauMountainHillGenerator] Calculating slopes");

            for (int y = 0; y < heightMap.height; y++)
            {
                for (int x = 0; x < heightMap.width; x++)
                {
                    float h = heightMap.GetHeight(x, y);
                    float hx = heightMap.GetHeight(x + 1, y);
                    float hz = heightMap.GetHeight(x, y + 1);

                    // حساب الانحدار في الاتجاهين
                    float slopeX = (hx - h) / pixelSizeMeters;
                    float slopeZ = (hz - h) / pixelSizeMeters;

                    // الانحدار الكلي بالدرجات
                    float slopeMagnitude = Mathf.Sqrt(slopeX * slopeX + slopeZ * slopeZ);
                    float slopeDegrees = Mathf.Atan(slopeMagnitude) * Mathf.Rad2Deg;

                    if (pixelData != null && x < pixelData.GetLength(0) && y < pixelData.GetLength(1))
                    {
                        pixelData[x, y].slope = slopeDegrees;
                    }
                }
            }
        }
    }
}
