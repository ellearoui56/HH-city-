using System;
using System.Collections.Generic;
using UnityEngine;

namespace ZZCityGen.Planning.MasterPlanning.Generators
{
    /// <summary>
    /// المرحلة 2.12: فحص التناقضات
    /// Validates the master plan for errors and inconsistencies
    /// 
    /// النظام يفحص:
    /// ✅ مدينة داخل بحيرة؟
    /// ✅ مدينة فوق جبل حاد؟
    /// ✅ مطار فوق نهر؟
    /// ✅ ميناء بعيد عن الساحل؟
    /// 
    /// إذا وجد خطأ → يعيد الحساب
    /// </summary>
    public class MasterPlanValidator
    {
        /// <summary>
        /// التحقق الكامل من خطة التخطيط
        /// </summary>
        public MasterPlanValidationData ValidateMasterPlan(MasterPlanData masterPlan)
        {
            var validation = new MasterPlanValidationData();

            // ============================================
            // 1. فحص الحدود العالمية
            // ============================================
            ValidateWorldBounds(masterPlan, validation);

            // ============================================
            // 2. فحص المدن
            // ============================================
            ValidateCities(masterPlan, validation);

            // ============================================
            // 3. فحص المطارات
            // ============================================
            ValidateAirports(masterPlan, validation);

            // ============================================
            // 4. فحص الموانئ
            // ============================================
            ValidatePorts(masterPlan, validation);

            // ============================================
            // 5. فحص شبكة النقل
            // ============================================
            ValidateTransportNetwork(masterPlan, validation);

            // ============================================
            // 6. فحص توزيع السكان
            // ============================================
            ValidatePopulation(masterPlan, validation);

            // ============================================
            // 7. فحص توزيع الأراضي
            // ============================================
            ValidateLandAllocation(masterPlan, validation);

            // ============================================
            // تسجيل النتائج
            // ============================================
            if (validation.isValid)
            {
                Debug.Log("[MasterPlanValidator] ✅ Master plan validation PASSED");
            }
            else
            {
                Debug.LogError("[MasterPlanValidator] ❌ Master plan validation FAILED");
                foreach (var error in validation.errors)
                {
                    Debug.LogError($"  ERROR: {error}");
                }
            }

            foreach (var warning in validation.warnings)
            {
                Debug.LogWarning($"  WARNING: {warning}");
            }

            return validation;
        }

        /// <summary>
        /// فحص الحدود العالمية
        /// </summary>
        private void ValidateWorldBounds(MasterPlanData masterPlan, MasterPlanValidationData validation)
        {
            if (masterPlan.worldBounds == null)
            {
                validation.AddError("World bounds not defined");
                return;
            }

            if (masterPlan.worldBounds.areaSquareKm < 100)
            {
                validation.AddWarning("World is very small (< 100 km²)");
            }

            if (masterPlan.worldBounds.areaSquareKm > 1_000_000)
            {
                validation.AddWarning("World is very large (> 1,000,000 km²)");
            }
        }

        /// <summary>
        /// فحص المدن
        /// </summary>
        private void ValidateCities(MasterPlanData masterPlan, MasterPlanValidationData validation)
        {
            if (masterPlan.cities.Count == 0)
            {
                validation.AddWarning("No cities defined");
                return;
            }

            // ============================================
            // 1. تحقق من أن العاصمة موجودة
            // ============================================
            if (masterPlan.capital == null)
            {
                validation.AddError("Capital not defined");
                return;
            }

            // ============================================
            // 2. تحقق من أن المدن داخل الحدود
            // ============================================
            foreach (var city in masterPlan.cities)
            {
                if (!masterPlan.worldBounds.Contains(city.position))
                {
                    validation.AddError($"City '{city.name}' is outside world bounds at {city.position}");
                }

                // ============================================
                // 3. تحقق من أن المدينة ليست فوق جبل حاد
                // ============================================
                var terrain = GetTerrainZoneAt(city.position, masterPlan.terrainAnalysis);
                if (terrain != null && !terrain.isBuildable)
                {
                    validation.AddError($"City '{city.name}' is in unbuildable terrain ({terrain.type})");
                }

                // ============================================
                // 4. تحقق من المسافة بين المدن
                // ============================================
                foreach (var otherCity in masterPlan.cities)
                {
                    if (city.name == otherCity.name) continue;

                    float distance = Vector2.Distance(city.position, otherCity.position);
                    if (distance < city.minimumDistanceToOtherCitiesKm * 1000)
                    {
                        validation.AddWarning($"Cities '{city.name}' and '{otherCity.name}' are too close ({distance / 1000}km)");
                    }
                }

                // ============================================
                // 5. تحقق من صحة السكان
                // ============================================
                if (city.targetPopulation <= 0)
                {
                    validation.AddError($"City '{city.name}' has invalid population");
                }
            }
        }

        /// <summary>
        /// فحص المطارات
        /// </summary>
        private void ValidateAirports(MasterPlanData masterPlan, MasterPlanValidationData validation)
        {
            if (masterPlan.infrastructure?.airports.Count == 0)
            {
                validation.AddWarning("No airports defined");
                return;
            }

            foreach (var airport in masterPlan.infrastructure.airports)
            {
                // ============================================
                // 1. تحقق من أن المطار داخل الحدود
                // ============================================
                if (!masterPlan.worldBounds.Contains(airport.position))
                {
                    validation.AddError($"Airport '{airport.name}' is outside world bounds");
                }

                // ============================================
                // 2. تحقق من أن المطار ليس فوق ماء
                // ============================================
                var terrain = GetTerrainZoneAt(airport.position, masterPlan.terrainAnalysis);
                if (terrain != null && terrain.type == TerrainZoneType.Water)
                {
                    validation.AddError($"Airport '{airport.name}' is over water");
                }

                // ============================================
                // 3. تحقق من صحة السعة
                // ============================================
                if (airport.plannedCapacity <= 0)
                {
                    validation.AddWarning($"Airport '{airport.name}' has no capacity");
                }
            }
        }

        /// <summary>
        /// فحص الموانئ
        /// </summary>
        private void ValidatePorts(MasterPlanData masterPlan, MasterPlanValidationData validation)
        {
            if (masterPlan.infrastructure?.ports.Count == 0)
            {
                validation.AddWarning("No ports defined");
                return;
            }

            var waterZone = GetWaterZone(masterPlan.terrainAnalysis);

            foreach (var port in masterPlan.infrastructure.ports)
            {
                // ============================================
                // 1. تحقق من أن الميناء قريب من الماء
                // ============================================
                if (waterZone != null)
                {
                    float distanceToWater = Vector2.Distance(port.position, waterZone.center);
                    if (distanceToWater > waterZone.radiusMeters + 20_000)  // 20km tolerance
                    {
                        validation.AddError($"Port '{port.name}' is too far from water ({distanceToWater / 1000}km)");
                    }
                }

                // ============================================
                // 2. تحقق من صحة السعة
                // ============================================
                if (port.containerCapacity <= 0)
                {
                    validation.AddWarning($"Port '{port.name}' has no capacity");
                }
            }
        }

        /// <summary>
        /// فحص شبكة النقل
        /// </summary>
        private void ValidateTransportNetwork(MasterPlanData masterPlan, MasterPlanValidationData validation)
        {
            if (masterPlan.transportNetwork == null || masterPlan.transportNetwork.edges.Count == 0)
            {
                validation.AddWarning("Transport network is empty");
                return;
            }

            // ============================================
            // تحقق من أن جميع المدن متصلة بالشبكة
            // ============================================
            var connectedCities = new HashSet<int>();
            foreach (var edge in masterPlan.transportNetwork.edges)
            {
                connectedCities.Add(edge.fromNodeIndex);
                connectedCities.Add(edge.toNodeIndex);
            }

            for (int i = 0; i < masterPlan.cities.Count; i++)
            {
                if (!connectedCities.Contains(i))
                {
                    validation.AddWarning($"City at index {i} is not connected to transport network");
                }
            }

            // ============================================
            // تحقق من صحة طول الطرق
            // ============================================
            if (masterPlan.transportNetwork.totalPlannedRoadsKm <= 0)
            {
                validation.AddWarning("No roads planned in transport network");
            }
        }

        /// <summary>
        /// فحص توزيع السكان
        /// </summary>
        private void ValidatePopulation(MasterPlanData masterPlan, MasterPlanValidationData validation)
        {
            if (masterPlan.populationDistribution == null)
            {
                validation.AddError("Population distribution not defined");
                return;
            }

            int totalPop = masterPlan.populationDistribution.totalPopulation;
            if (totalPop <= 0)
            {
                validation.AddWarning("Total population is 0");
                return;
            }

            // ============================================
            // تحقق من أن العاصمة لديها أكبر سكان
            // ============================================
            if (masterPlan.populationDistribution.capitalPopulation < 100_000)
            {
                validation.AddWarning("Capital population is too small");
            }

            // ============================================
            // تحقق من التوازن السكاني
            // ============================================
            float capitalRatio = (float)masterPlan.populationDistribution.capitalPopulation / totalPop;
            if (capitalRatio > 0.6f)
            {
                validation.AddWarning("Capital has too much of the population (> 60%)");
            }
        }

        /// <summary>
        /// فحص توزيع الأراضي
        /// </summary>
        private void ValidateLandAllocation(MasterPlanData masterPlan, MasterPlanValidationData validation)
        {
            if (masterPlan.globalLandAllocation.Count == 0)
            {
                validation.AddWarning("Land allocation not defined");
                return;
            }

            // ============================================
            // تحقق من أن المجموع تقريباً 100%
            // ============================================
            float totalPercentage = 0;
            foreach (var land in masterPlan.globalLandAllocation)
            {
                totalPercentage += land.percentageOfWorld;
            }

            if (Mathf.Abs(totalPercentage - 1.0f) > 0.01f)
            {
                validation.AddWarning($"Land allocation doesn't sum to 100% ({totalPercentage * 100:F1}%)");
            }
        }

        /// <summary>
        /// الحصول على منطقة التضاريس في موقع معين
        /// </summary>
        private TerrainZoneData GetTerrainZoneAt(Vector2 position, TerrainAnalysisData terrain)
        {
            TerrainZoneData closest = null;
            float minDistance = float.MaxValue;

            foreach (var zone in terrain.zones)
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
        /// الحصول على منطقة المياه
        /// </summary>
        private TerrainZoneData GetWaterZone(TerrainAnalysisData terrain)
        {
            foreach (var zone in terrain.zones)
            {
                if (zone.type == TerrainZoneType.Water)
                    return zone;
            }
            return null;
        }
    }
}
