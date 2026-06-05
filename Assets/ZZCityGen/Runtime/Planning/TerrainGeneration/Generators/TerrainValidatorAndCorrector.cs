using System;
using System.Collections.Generic;
using UnityEngine;

namespace ZZCityGen.Planning.TerrainGeneration.Generators
{
    /// <summary>
    /// المرحلة 3.12، 3.13: التحقق والتصحيح
    /// - 3.12: التحقق من صحة التضاريس
    /// - 3.13: تصحيح التناقضات تلقائياً
    /// </summary>
    public class TerrainValidatorAndCorrector
    {
        /// <summary>
        /// المرحلة 3.12: التحقق من صحة التضاريس
        /// </summary>
        public TerrainValidationData ValidateTerrainData(
            TerrainGenerationData terrainData,
            MasterPlanData masterPlan,
            Vector2 worldBoundsMin,
            float pixelSize)
        {
            Debug.Log("[TerrainValidator] Validating terrain data");

            var validation = new TerrainValidationData();

            // ============================================
            // 1. التحقق من وجود HeightMap
            // ============================================
            if (terrainData.heightMap == null || terrainData.heightMap.heightData.Count == 0)
            {
                validation.AddError("HeightMap is missing or empty");
                return validation;
            }

            // ============================================
            // 2. التحقق من المناطق المحمية
            // ============================================
            if (terrainData.protectedZones.Count != masterPlan.cities.Count + 1)
            {
                validation.AddWarning(
                    $"Protected zones count mismatch: expected {masterPlan.cities.Count + 1}, got {terrainData.protectedZones.Count}"
                );
            }

            // ============================================
            // 3. التحقق من أن المدن داخل مناطقها المحمية
            // ============================================
            foreach (var zone in terrainData.protectedZones)
            {
                float height = terrainData.heightMap.GetHeightInterpolated(
                    (zone.center.x - worldBoundsMin.x) / pixelSize,
                    (zone.center.y - worldBoundsMin.y) / pixelSize
                );

                // التحقق من أن الارتفاع معقول
                if (height < -100 || height > 3000)
                {
                    validation.AddError($"Protected zone at {zone.center} has unrealistic height: {height}m");
                }
            }

            // ============================================
            // 4. التحقق من الأنهار والبحيرات
            // ============================================
            foreach (var river in terrainData.rivers)
            {
                if (river.pathPoints.Count < 3)
                {
                    validation.AddWarning($"River '{river.name}' is too short ({river.pathPoints.Count} points)");
                }

                // التحقق من أن النهر متصل
                for (int i = 0; i < river.pathPoints.Count - 1; i++)
                {
                    float distance = Vector2.Distance(river.pathPoints[i], river.pathPoints[i + 1]);
                    if (distance > 1000)
                    {
                        validation.AddWarning($"River '{river.name}' has a large gap at point {i}");
                    }
                }
            }

            // ============================================
            // 5. التحقق من البحيرات
            // ============================================
            foreach (var lake in terrainData.lakes)
            {
                if (lake.radiusMeters <= 0)
                {
                    validation.AddError($"Lake '{lake.name}' has invalid radius");
                }
            }

            // ============================================
            // 6. التحقق من الإحصائيات
            // ============================================
            if (terrainData.avgBuildability < 0 || terrainData.avgBuildability > 1)
            {
                validation.AddError($"Invalid buildability average: {terrainData.avgBuildability}");
            }

            if (terrainData.waterCoverage < 0 || terrainData.waterCoverage > 1)
            {
                validation.AddError($"Invalid water coverage: {terrainData.waterCoverage}");
            }

            Debug.Log($"[TerrainValidator] Validation complete: {validation.errors.Count} errors, {validation.warnings.Count} warnings");

            return validation;
        }

        /// <summary>
        /// المرحلة 3.13: تصحيح التناقضات تلقائياً
        /// </summary>
        public bool CorrectTerrainIssues(
            TerrainGenerationData terrainData,
            HeightMapData heightMap,
            Vector2 worldBoundsMin,
            float pixelSize)
        {
            Debug.Log("[TerrainCorrector] Attempting to fix terrain issues");

            bool hasCorrectedIssues = false;

            // ============================================
            // 1. إعادة توجيه الأنهار القصيرة جداً
            // ============================================
            for (int i = terrainData.rivers.Count - 1; i >= 0; i--)
            {
                var river = terrainData.rivers[i];

                if (river.pathPoints.Count < 3)
                {
                    Debug.Log($"[TerrainCorrector] Removing too short river: {river.name}");
                    terrainData.rivers.RemoveAt(i);
                    hasCorrectedIssues = true;
                }
            }

            // ============================================
            // 2. إصلاح الفجوات في الأنهار
            // ============================================
            foreach (var river in terrainData.rivers)
            {
                for (int i = 0; i < river.pathPoints.Count - 1; i++)
                {
                    float distance = Vector2.Distance(river.pathPoints[i], river.pathPoints[i + 1]);

                    if (distance > 500)  // 500 متر = فجوة كبيرة
                    {
                        // إدراج نقاط وسيطة
                        int pointsToAdd = Mathf.CeilToInt(distance / 250);
                        for (int j = 1; j < pointsToAdd; j++)
                        {
                            float t = (float)j / pointsToAdd;
                            Vector2 intermediatePoint = Vector2.Lerp(
                                river.pathPoints[i],
                                river.pathPoints[i + 1],
                                t
                            );
                            river.pathPoints.Insert(i + j, intermediatePoint);
                        }

                        i += pointsToAdd - 1;
                        hasCorrectedIssues = true;
                    }
                }
            }

            // ============================================
            // 3. إزالة البحيرات المتداخلة
            // ============================================
            for (int i = 0; i < terrainData.lakes.Count - 1; i++)
            {
                for (int j = i + 1; j < terrainData.lakes.Count; j++)
                {
                    float distance = Vector2.Distance(
                        terrainData.lakes[i].center,
                        terrainData.lakes[j].center
                    );

                    if (distance < terrainData.lakes[i].radiusMeters + terrainData.lakes[j].radiusMeters)
                    {
                        // دمج البحيرتين أو حذف الأصغر
                        if (terrainData.lakes[i].radiusMeters > terrainData.lakes[j].radiusMeters)
                        {
                            Debug.Log($"[TerrainCorrector] Removing overlapping lake: {terrainData.lakes[j].name}");
                            terrainData.lakes.RemoveAt(j);
                            j--;
                        }
                        else
                        {
                            Debug.Log($"[TerrainCorrector] Removing overlapping lake: {terrainData.lakes[i].name}");
                            terrainData.lakes.RemoveAt(i);
                            i--;
                            break;
                        }

                        hasCorrectedIssues = true;
                    }
                }
            }

            // ============================================
            // 4. إعادة حساب الإحصائيات
            // ============================================
            if (hasCorrectedIssues)
            {
                RecalculateStatistics(terrainData, heightMap);
                Debug.Log("[TerrainCorrector] Statistics recalculated");
            }

            Debug.Log("[TerrainCorrector] Correction complete" + (hasCorrectedIssues ? " - issues fixed" : " - no issues found"));

            return hasCorrectedIssues;
        }

        /// <summary>
        /// إعادة حساب الإحصائيات
        /// </summary>
        private void RecalculateStatistics(
            TerrainGenerationData terrainData,
            HeightMapData heightMap)
        {
            int totalPixels = heightMap.heightData.Count;
            int waterPixels = 0;
            int forestPixels = 0;
            int mountainPixels = 0;
            float buildabilitySum = 0;

            float seaLevel = -50f;

            for (int y = 0; y < heightMap.height; y++)
            {
                for (int x = 0; x < heightMap.width; x++)
                {
                    float height = heightMap.GetHeight(x, y);

                    if (height <= seaLevel)
                        waterPixels++;
                    else if (height > seaLevel + 1000)
                        mountainPixels++;

                    buildabilitySum += (height > seaLevel) ? 0.5f : 0f;
                }
            }

            terrainData.waterCoverage = (float)waterPixels / totalPixels;
            terrainData.mountainCoverage = (float)mountainPixels / totalPixels;
            terrainData.forestCoverage = terrainData.forests.Count * 2000f / (heightMap.width * heightMap.height);
            terrainData.avgBuildability = buildabilitySum / totalPixels;
            terrainData.avgRoadFriendliness = 0.5f;  // تقدير تقريبي
        }
    }
}
