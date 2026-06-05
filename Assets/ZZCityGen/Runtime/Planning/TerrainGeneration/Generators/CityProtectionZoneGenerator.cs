using System;
using System.Collections.Generic;
using UnityEngine;
using ZZCityGen.Planning.MasterPlanning;

namespace ZZCityGen.Planning.TerrainGeneration.Generators
{
    /// <summary>
    /// المرحلة 3.2: حماية المدن
    /// قبل إضافة الجبال والتفاصيل الأخرى
    /// نحتاج لحماية مناطق المدن من الانحدارات الحادة
    /// </summary>
    public class CityProtectionZoneGenerator
    {
        /// <summary>
        /// إنشاء مناطق محمية حول المدن
        /// </summary>
        public List<ProtectedZoneData> GenerateProtectedZones(
            MasterPlanData masterPlan)
        {
            Debug.Log("[CityProtectionZoneGenerator] Generating protected zones");

            var protectedZones = new List<ProtectedZoneData>();

            // ============================================
            // 1. المنطقة المحمية حول العاصمة
            // ============================================
            if (masterPlan.capital != null)
            {
                var capitalZone = new ProtectedZoneData(
                    cityIndex: -1,  // العاصمة
                    center: masterPlan.capital.position,
                    radiusMeters: masterPlan.capital.radiusMeters * 1.5f,  // نطاق أكبر
                    maxAllowedSlope: 5f  // انحدار حاد جداً ممنوع
                );
                protectedZones.Add(capitalZone);
                Debug.Log($"[CityProtectionZoneGenerator] Capital protection zone at {capitalZone.center}");
            }

            // ============================================
            // 2. المناطق المحمية حول المدن الأخرى
            // ============================================
            for (int i = 0; i < masterPlan.cities.Count; i++)
            {
                var city = masterPlan.cities[i];

                float zoneRadius = city.radiusMeters * 1.2f;
                float maxSlope = 8f;  // انحدار أقل قساوة من العاصمة

                var zone = new ProtectedZoneData(
                    cityIndex: i,
                    center: city.position,
                    radiusMeters: zoneRadius,
                    maxAllowedSlope: maxSlope
                );

                protectedZones.Add(zone);
            }

            Debug.Log($"[CityProtectionZoneGenerator] Generated {protectedZones.Count} protected zones");

            return protectedZones;
        }

        /// <summary>
        /// تطبيق حماية المدن على HeightMap
        /// </summary>
        public void ApplyProtectionToHeightMap(
            HeightMapData heightMap,
            List<ProtectedZoneData> protectedZones,
            Vector2 worldBoundsMin,
            float pixelSize)
        {
            Debug.Log("[CityProtectionZoneGenerator] Applying protection to height map");

            for (int y = 0; y < heightMap.height; y++)
            {
                for (int x = 0; x < heightMap.width; x++)
                {
                    // تحويل إحداثيات الخريطة إلى إحداثيات العالم
                    float worldX = worldBoundsMin.x + (x + 0.5f) * pixelSize;
                    float worldZ = worldBoundsMin.y + (y + 0.5f) * pixelSize;
                    var worldPos = new Vector2(worldX, worldZ);

                    // البحث عن أقرب منطقة محمية
                    ProtectedZoneData closestZone = null;
                    float minDistance = float.MaxValue;

                    foreach (var zone in protectedZones)
                    {
                        float distance = Vector2.Distance(worldPos, zone.center);
                        if (distance < zone.radiusMeters && distance < minDistance)
                        {
                            minDistance = distance;
                            closestZone = zone;
                        }
                    }

                    if (closestZone != null)
                    {
                        // تطبيق التسطيح التدريجي
                        float currentHeight = heightMap.GetHeight(x, y);
                        float centerHeight = heightMap.GetHeightInterpolated(
                            (closestZone.center.x - worldBoundsMin.x) / pixelSize,
                            (closestZone.center.y - worldBoundsMin.y) / pixelSize
                        );

                        // المنطقة الداخلية = مستقيمة تماماً
                        if (closestZone.ContainsInner(worldPos))
                        {
                            heightMap.SetHeight(x, y, centerHeight);
                        }
                        // المنطقة الخارجية = تنحدر تدريجياً
                        else
                        {
                            float distanceRatio = minDistance / closestZone.radiusMeters;
                            float targetHeight = Mathf.Lerp(centerHeight, currentHeight, distanceRatio);
                            heightMap.SetHeight(x, y, targetHeight);
                        }
                    }
                }
            }

            heightMap.UpdateStats();
        }
    }
}
