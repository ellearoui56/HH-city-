using System;
using System.Collections.Generic;
using UnityEngine;
using ZZCityGen.Planning.MasterPlanning;

namespace ZZCityGen.Planning.TerrainGeneration.Generators
{
    /// <summary>
    /// المراحل 3.6، 3.7، 3.8: إنشاء الميزات المائية
    /// - 3.6: السواحل (Coastlines)
    /// - 3.7: الأنهار (Rivers) - بذكاء تابع الانحدار
    /// - 3.8: البحيرات (Lakes)
    /// </summary>
    public class RiverLakeCoastGenerator
    {
        private readonly System.Random random;

        public RiverLakeCoastGenerator(int seed)
        {
            this.random = new System.Random(seed);
        }

        /// <summary>
        /// المرحلة 3.6: إنشاء السواحل
        /// </summary>
        public CoastlineData GenerateCoastline(
            HeightMapData heightMap,
            MasterPlanData masterPlan,
            Vector2 worldBoundsMin,
            float pixelSize)
        {
            Debug.Log("[RiverLakeCoastGenerator] Generating coastlines");

            var coastlineData = new CoastlineData();

            // البحث عن منطقة الماء
            var waterZone = masterPlan.terrainAnalysis.zones.Find(z => z.type == TerrainZoneType.Water);
            if (waterZone == null)
                return coastlineData;

            float seaLevel = waterZone.averageHeightMeters;

            // تحديد خط الساحل (حيث يلتقي البر بالبحر)
            for (int y = 0; y < heightMap.height - 1; y++)
            {
                for (int x = 0; x < heightMap.width - 1; x++)
                {
                    float h1 = heightMap.GetHeight(x, y);
                    float h2 = heightMap.GetHeight(x + 1, y);
                    float h3 = heightMap.GetHeight(x, y + 1);
                    float h4 = heightMap.GetHeight(x + 1, y + 1);

                    // إذا كانت هناك انتقالية من ماء إلى بر
                    bool isTransition = (h1 < seaLevel && h2 > seaLevel) ||
                                       (h1 > seaLevel && h2 < seaLevel) ||
                                       (h1 < seaLevel && h3 > seaLevel) ||
                                       (h1 > seaLevel && h3 < seaLevel);

                    if (isTransition)
                    {
                        float worldX = worldBoundsMin.x + (x + 0.5f) * pixelSize;
                        float worldZ = worldBoundsMin.y + (y + 0.5f) * pixelSize;
                        coastlineData.coastlinePoints.Add(new Vector2(worldX, worldZ));
                    }
                }
            }

            // تصنيف مناطق الساحل إلى شواطئ وجروف
            ClassifyCoastalZones(coastlineData, heightMap, worldBoundsMin, pixelSize, seaLevel);

            Debug.Log($"[RiverLakeCoastGenerator] Coastline generated with {coastlineData.coastlinePoints.Count} points");

            return coastlineData;
        }

        /// <summary>
        /// تصنيف مناطق الساحل
        /// </summary>
        private void ClassifyCoastalZones(
            CoastlineData coastlineData,
            HeightMapData heightMap,
            Vector2 worldBoundsMin,
            float pixelSize,
            float seaLevel)
        {
            // تجميع النقاط القريبة من بعضها
            var clusters = new List<List<Vector2>>();

            foreach (var point in coastlineData.coastlinePoints)
            {
                bool foundCluster = false;

                foreach (var cluster in clusters)
                {
                    if (Vector2.Distance(point, cluster[0]) < 1000)  // 1 كم
                    {
                        cluster.Add(point);
                        foundCluster = true;
                        break;
                    }
                }

                if (!foundCluster)
                {
                    clusters.Add(new List<Vector2> { point });
                }
            }

            // تصنيف كل مجموعة
            foreach (var cluster in clusters)
            {
                if (cluster.Count == 0) continue;

                Vector2 centerPoint = Vector2.zero;
                foreach (var p in cluster)
                    centerPoint += p;
                centerPoint /= cluster.Count;

                // حساب الانحدار
                float avgSlope = 0;
                foreach (var p in cluster)
                {
                    int pixX = (int)((p.x - worldBoundsMin.x) / pixelSize);
                    int pixY = (int)((p.y - worldBoundsMin.y) / pixelSize);

                    if (pixX >= 0 && pixX < heightMap.width && pixY >= 0 && pixY < heightMap.height)
                    {
                        float h = heightMap.GetHeight(pixX, pixY);
                        if (h > seaLevel + 50)  // مرتفع = جرف
                            avgSlope += 1;
                    }
                }
                avgSlope /= cluster.Count;

                // تصنيف
                if (avgSlope > 0.5f)
                {
                    coastlineData.cliffCenters.Add(centerPoint);  // جروف عالية
                }
                else
                {
                    coastlineData.beachCenters.Add(centerPoint);  // شواطئ مستقيمة
                    coastlineData.harborZones.Add(centerPoint);   // مناطق موانئ محتملة
                }
            }
        }

        /// <summary>
        /// المرحلة 3.7: إنشاء الأنهار
        /// الأنهار تتابع الانحدار الطبيعي من الأعلى إلى الأسفل
        /// </summary>
        public List<RiverData> GenerateRivers(
            HeightMapData heightMap,
            MasterPlanData masterPlan,
            Vector2 worldBoundsMin,
            float pixelSize)
        {
            Debug.Log("[RiverLakeCoastGenerator] Generating rivers");

            var rivers = new List<RiverData>();

            // كل منطقة جبلية قد تولد نهراً
            var mountainZones = masterPlan.terrainAnalysis.zones.FindAll(z => z.type == TerrainZoneType.Mountain);

            foreach (var mountain in mountainZones)
            {
                // بدء النهر من أعلى نقطة في الجبل
                int startPixX = (int)((mountain.center.x - worldBoundsMin.x) / pixelSize);
                int startPixY = (int)((mountain.center.y - worldBoundsMin.y) / pixelSize);

                if (startPixX >= 0 && startPixX < heightMap.width && startPixY >= 0 && startPixY < heightMap.height)
                {
                    var river = TraceRiverFromPoint(
                        heightMap,
                        startPixX,
                        startPixY,
                        worldBoundsMin,
                        pixelSize,
                        mountain
                    );

                    if (river.pathPoints.Count > 10)  // نهر يجب أن يكون بطول معين
                    {
                        rivers.Add(river);
                    }
                }
            }

            Debug.Log($"[RiverLakeCoastGenerator] Generated {rivers.Count} rivers");

            return rivers;
        }

        /// <summary>
        /// تتبع مسار النهر بتابع الانحدار
        /// </summary>
        private RiverData TraceRiverFromPoint(
            HeightMapData heightMap,
            int startX,
            int startY,
            Vector2 worldBoundsMin,
            float pixelSize,
            TerrainZoneData source)
        {
            var river = new RiverData($"{source.type} River", 50f, 5f);

            int currentX = startX;
            int currentY = startY;
            int maxSteps = 500;
            int steps = 0;

            while (steps < maxSteps)
            {
                // تحويل إلى إحداثيات العالم
                float worldX = worldBoundsMin.x + (currentX + 0.5f) * pixelSize;
                float worldZ = worldBoundsMin.y + (currentY + 0.5f) * pixelSize;
                river.pathPoints.Add(new Vector2(worldX, worldZ));

                // البحث عن أقل نقطة حول الموقع الحالي
                float minHeight = heightMap.GetHeight(currentX, currentY);
                int nextX = currentX;
                int nextY = currentY;

                for (int dy = -1; dy <= 1; dy++)
                {
                    for (int dx = -1; dx <= 1; dx++)
                    {
                        if (dx == 0 && dy == 0) continue;

                        int checkX = currentX + dx;
                        int checkY = currentY + dy;

                        if (checkX >= 0 && checkX < heightMap.width && checkY >= 0 && checkY < heightMap.height)
                        {
                            float h = heightMap.GetHeight(checkX, checkY);
                            if (h < minHeight)
                            {
                                minHeight = h;
                                nextX = checkX;
                                nextY = checkY;
                            }
                        }
                    }
                }

                // إذا لم نجد نقطة أقل، النهر وصل إلى البحر أو بحيرة
                if (nextX == currentX && nextY == currentY)
                    break;

                currentX = nextX;
                currentY = nextY;
                steps++;
            }

            return river;
        }

        /// <summary>
        /// المرحلة 3.8: إنشاء البحيرات
        /// </summary>
        public List<LakeData> GenerateLakes(
            HeightMapData heightMap,
            MasterPlanData masterPlan,
            Vector2 worldBoundsMin,
            float pixelSize)
        {
            Debug.Log("[RiverLakeCoastGenerator] Generating lakes");

            var lakes = new List<LakeData>();

            // أنواع البحيرات المختلفة
            var valleyZones = masterPlan.terrainAnalysis.zones.FindAll(z => z.type == TerrainZoneType.Valley);

            foreach (var valley in valleyZones)
            {
                // كل وادي قد يحتوي على بحيرة
                if (random.NextDouble() > 0.5)  // 50% احتمال
                {
                    var lake = new LakeData(
                        $"Lake at {valley.type}",
                        valley.center,
                        radiusMeters: 2000 + (float)random.NextDouble() * 3000,  // 2-5 كم
                        waterLevel: valley.averageHeightMeters - 10,
                        lakeType: "Forest"
                    );

                    lakes.Add(lake);
                }
            }

            Debug.Log($"[RiverLakeCoastGenerator] Generated {lakes.Count} lakes");

            return lakes;
        }
    }
}
