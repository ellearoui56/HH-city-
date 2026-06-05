using System;
using UnityEngine;
using ZZCityGen.Planning.MasterPlanning;

namespace ZZCityGen.Planning.TerrainGeneration.Generators
{
    /// <summary>
    /// المرحلة 3.9: إنشاء مناطق الغابات
    /// الغابات توضع في المناطق المناسبة
    /// (ليست جبال شديدة الانحدار، ليست قريبة من المدن كثيراً)
    /// </summary>
    public class ForestGenerator
    {
        private readonly System.Random random;

        public ForestGenerator(int seed)
        {
            this.random = new System.Random(seed);
        }

        /// <summary>
        /// توليد مناطق الغابات
        /// </summary>
        public System.Collections.Generic.List<ForestRegionData> GenerateForests(
            MasterPlanData masterPlan,
            Vector2 worldBoundsMin,
            Vector2 worldBoundsMax)
        {
            Debug.Log("[ForestGenerator] Generating forest regions");

            var forests = new System.Collections.Generic.List<ForestRegionData>();

            // الغابات توضع بعيداً عن المدن، لكن قريبة من الأنهار والمياه
            int forestCount = Mathf.RoundToInt(masterPlan.worldBounds.areaSquareKm / (50 * 50));  // غابة كل 50x50 كم

            for (int i = 0; i < forestCount; i++)
            {
                // البحث عن موقع مناسب
                Vector2 forestCenter = FindSuitableForestLocation(masterPlan, worldBoundsMin, worldBoundsMax, forests);

                if (forestCenter != Vector2.zero)
                {
                    float forestRadius = 1000 + (float)random.NextDouble() * 5000;  // 1-6 كم
                    float density = 0.6f + (float)random.NextDouble() * 0.4f;  // 60-100%

                    var forest = new ForestRegionData(
                        $"Forest {i}",
                        forestCenter,
                        forestRadius,
                        density
                    );

                    forests.Add(forest);
                }
            }

            Debug.Log($"[ForestGenerator] Generated {forests.Count} forest regions");

            return forests;
        }

        /// <summary>
        /// البحث عن موقع مناسب لغابة جديدة
        /// </summary>
        private Vector2 FindSuitableForestLocation(
            MasterPlanData masterPlan,
            Vector2 worldBoundsMin,
            Vector2 worldBoundsMax,
            System.Collections.Generic.List<ForestRegionData> existingForests)
        {
            int maxAttempts = 20;

            for (int attempt = 0; attempt < maxAttempts; attempt++)
            {
                // اختيار موقع عشوائي
                float randomX = worldBoundsMin.x + (float)random.NextDouble() * (worldBoundsMax.x - worldBoundsMin.x);
                float randomZ = worldBoundsMin.y + (float)random.NextDouble() * (worldBoundsMax.y - worldBoundsMin.y);
                var candidateLocation = new Vector2(randomX, randomZ);

                // التحقق من أنه بعيد عن المدن كفاية
                bool tooCloseToCity = false;
                
                if (masterPlan.capital != null)
                {
                    if (Vector2.Distance(candidateLocation, masterPlan.capital.position) < 5000)
                        tooCloseToCity = true;
                }

                foreach (var city in masterPlan.cities)
                {
                    if (Vector2.Distance(candidateLocation, city.position) < 5000)
                    {
                        tooCloseToCity = true;
                        break;
                    }
                }

                // التحقق من أنه بعيد عن الغابات الأخرى
                bool tooCloseToForest = false;
                foreach (var forest in existingForests)
                {
                    if (Vector2.Distance(candidateLocation, forest.center) < 3000)
                    {
                        tooCloseToForest = true;
                        break;
                    }
                }

                if (!tooCloseToCity && !tooCloseToForest)
                {
                    return candidateLocation;
                }
            }

            return Vector2.zero;
        }
    }
}
