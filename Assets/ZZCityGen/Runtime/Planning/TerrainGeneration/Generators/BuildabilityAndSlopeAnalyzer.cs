using System;
using UnityEngine;

namespace ZZCityGen.Planning.TerrainGeneration.Generators
{
    /// <summary>
    /// المرحلة 3.10، 3.11: التحليل
    /// - 3.10: حساب قابلية البناء في كل نقطة
    /// - 3.11: حساب خريطة الانحدارات للطرق
    /// </summary>
    public class BuildabilityAndSlopeAnalyzer
    {
        /// <summary>
        /// المرحلة 3.10: حساب قابلية البناء
        /// </summary>
        public void CalculateBuildability(
            HeightMapData heightMap,
            System.Collections.Generic.List<ProtectedZoneData> protectedZones,
            System.Collections.Generic.List<ForestRegionData> forests,
            System.Collections.Generic.List<LakeData> lakes,
            System.Collections.Generic.List<RiverData> rivers,
            Vector2 worldBoundsMin,
            float pixelSize,
            HeightMapPixel[,] pixelData)
        {
            Debug.Log("[BuildabilityAnalyzer] Calculating buildability scores");

            float minElev = heightMap.minElevation;
            float maxElev = heightMap.maxElevation;
            float seaLevel = -50f;

            for (int y = 0; y < heightMap.height; y++)
            {
                for (int x = 0; x < heightMap.width; x++)
                {
                    float worldX = worldBoundsMin.x + (x + 0.5f) * pixelSize;
                    float worldZ = worldBoundsMin.y + (y + 0.5f) * pixelSize;
                    var worldPos = new Vector2(worldX, worldZ);

                    float buildability = 1.0f;

                    // ============================================
                    // 1. الماء = غير قابل للبناء
                    // ============================================
                    float height = heightMap.GetHeight(x, y);
                    if (height <= seaLevel)
                    {
                        buildability = 0f;
                    }
                    // ============================================
                    // 2. الارتفاع الشديد = صعب البناء
                    // ============================================
                    else if (height > seaLevel + 1000)
                    {
                        buildability *= 0.3f;  // تقليل إلى 30%
                    }
                    else if (height > seaLevel + 500)
                    {
                        buildability *= 0.6f;  // تقليل إلى 60%
                    }

                    // ============================================
                    // 3. الغابات = تقليل قابلية البناء
                    // ============================================
                    foreach (var forest in forests)
                    {
                        if (Vector2.Distance(worldPos, forest.center) < forest.radiusMeters)
                        {
                            buildability *= (1f - forest.density);  // كلما كثفت الغابة، قل البناء
                            break;
                        }
                    }

                    // ============================================
                    // 4. البحيرات والأنهار = غير قابلة للبناء
                    // ============================================
                    foreach (var lake in lakes)
                    {
                        float distToLake = Vector2.Distance(worldPos, lake.center);
                        if (distToLake < lake.radiusMeters)
                        {
                            buildability = 0f;
                            break;
                        }
                    }

                    foreach (var river in rivers)
                    {
                        foreach (var point in river.pathPoints)
                        {
                            if (Vector2.Distance(worldPos, point) < river.width)
                            {
                                buildability = 0f;
                                break;
                            }
                        }
                        if (buildability == 0) break;
                    }

                    // ============================================
                    // 5. المناطق المحمية حول المدن = عالي
                    // ============================================
                    foreach (var zone in protectedZones)
                    {
                        if (zone.Contains(worldPos))
                        {
                            buildability = 1f;  // قابل للبناء بكامل الحد الأقصى
                            break;
                        }
                    }

                    // ضمان أن القابلية بين 0 و 1
                    buildability = Mathf.Clamp01(buildability);

                    if (pixelData != null && x < pixelData.GetLength(0) && y < pixelData.GetLength(1))
                    {
                        pixelData[x, y].buildability = buildability;
                    }
                }
            }
        }

        /// <summary>
        /// المرحلة 3.11: حساب خريطة الانحدارات
        /// </summary>
        public void CalculateSlopes(
            HeightMapData heightMap,
            float pixelSizeMeters,
            HeightMapPixel[,] pixelData)
        {
            Debug.Log("[SlopeAnalyzer] Calculating slope map");

            for (int y = 0; y < heightMap.height - 1; y++)
            {
                for (int x = 0; x < heightMap.width - 1; x++)
                {
                    float h = heightMap.GetHeight(x, y);
                    float hx = heightMap.GetHeight(x + 1, y);
                    float hz = heightMap.GetHeight(x, y + 1);

                    // الفرق الارتفاعي
                    float deltaX = hx - h;
                    float deltaZ = hz - h;

                    // الانحدار كنسبة
                    float slopeX = deltaX / pixelSizeMeters;
                    float slopeZ = deltaZ / pixelSizeMeters;

                    // الانحدار الكلي (بالراديان ثم إلى درجات)
                    float slopeMagnitude = Mathf.Sqrt(slopeX * slopeX + slopeZ * slopeZ);
                    float slopeDegrees = Mathf.Atan(slopeMagnitude) * Mathf.Rad2Deg;

                    if (pixelData != null && x < pixelData.GetLength(0) && y < pixelData.GetLength(1))
                    {
                        pixelData[x, y].slope = slopeDegrees;

                        // ============================================
                        // حساب "Road Friendliness" بناءً على الانحدار
                        // ============================================
                        if (slopeDegrees < 3f)
                        {
                            pixelData[x, y].roadFriendliness = 1.0f;  // ممتاز للطرق
                        }
                        else if (slopeDegrees < 6f)
                        {
                            pixelData[x, y].roadFriendliness = 0.8f;  // جيد
                        }
                        else if (slopeDegrees < 10f)
                        {
                            pixelData[x, y].roadFriendliness = 0.5f;  // متوسط
                        }
                        else if (slopeDegrees < 15f)
                        {
                            pixelData[x, y].roadFriendliness = 0.2f;  // سيء
                        }
                        else
                        {
                            pixelData[x, y].roadFriendliness = 0.0f;  // مستحيل تقريباً
                        }
                    }
                }
            }
        }
    }
}
