using System;
using System.Collections.Generic;
using UnityEngine;

namespace ZZCityGen.Planning.MasterPlanning.Generators
{
    /// <summary>
    /// المرحلة 2.5: توزيع المدن
    /// Distributes different city types across the world
    /// 
    /// مثال:
    /// - 1 Capital
    /// - 4 Family Cities
    /// - 3 Rural Cities
    /// - 2 Industrial Cities
    /// - 1 Port City
    /// 
    /// قاعدة مهمة: لا يسمح بوجود مدينتين متلاصقتين (Minimum Distance: 10 km)
    /// </summary>
    public class CityDistributionGenerator
    {
        private readonly System.Random random;

        public CityDistributionGenerator(int seed)
        {
            this.random = new System.Random(seed);
        }

        /// <summary>
        /// توزيع المدن على مستوى العالم
        /// </summary>
        public List<CityMasterPlanData> DistributeCities(
            CapitalLocationData capital,
            WorldBoundsData bounds,
            TerrainAnalysisData terrain,
            ClimateAnalysisData climate,
            SeededNameGenerator nameGenerator)
        {
            var cities = new List<CityMasterPlanData>();

            // ============================================
            // 1. إضافة العاصمة
            // ============================================
            var capitalCity = new CityMasterPlanData(
                "Capital City",
                CityArchetype.Capital,
                capital.position,
                capital.radiusMeters,
                capital.targetPopulation,
                EconomicIdentity.Finance
            );
            capitalCity.isCapital = true;
            cities.Add(capitalCity);

            Debug.Log("[CityDistributionGenerator] Added capital city");

            // ============================================
            // 2. توزيع المدن الأخرى
            // ============================================
            var cityConfigs = GetCityDistributionConfig();

            foreach (var config in cityConfigs)
            {
                for (int i = 0; i < config.count; i++)
                {
                    CityMasterPlanData city = null;
                    
                    // محاولة العثور على موقع صالح
                    int attempts = 0;
                    while (attempts < 50 && city == null)
                    {
                        var position = FindValidCityLocation(cities, bounds, terrain, config.minDistanceKm);
                        if (position.HasValue)
                        {
                            city = new CityMasterPlanData(
                                nameGenerator.GenerateRandomCityName(),
                                config.archetype,
                                position.Value,
                                config.radiusMeters,
                                config.population,
                                config.economy
                            );

                            // التحقق من أن الموقع ليس فوق جبل
                            var zone = GetTerrainZoneAt(position.Value, terrain);
                            if (zone == null || !zone.isBuildable)
                            {
                                city = null;
                                attempts++;
                                continue;
                            }

                            cities.Add(city);
                            Debug.Log($"[CityDistributionGenerator] Added {config.archetype} city: {city.name} at {position}");
                            break;
                        }
                        attempts++;
                    }

                    if (city == null)
                    {
                        Debug.LogWarning($"[CityDistributionGenerator] Failed to place {config.archetype} city after {attempts} attempts");
                    }
                }
            }

            // ============================================
            // 3. حساب المسافة من العاصمة
            // ============================================
            foreach (var city in cities)
            {
                if (!city.isCapital)
                {
                    city.distanceFromCapitalKm = Vector2.Distance(city.position, capitalCity.position) / 1000f;
                }
            }

            Debug.Log($"[CityDistributionGenerator] Total cities distributed: {cities.Count}");
            return cities;
        }

        /// <summary>
        /// الحصول على إعدادات توزيع المدن
        /// </summary>
        private List<CityConfig> GetCityDistributionConfig()
        {
            var configs = new List<CityConfig>
            {
                // العائلات
                new CityConfig
                {
                    archetype = CityArchetype.FamilyCity,
                    count = 4,
                    population = 120_000,
                    radiusMeters = 8_000,
                    economy = EconomicIdentity.Services,
                    minDistanceKm = 15
                },

                // مدن ريفية
                new CityConfig
                {
                    archetype = CityArchetype.RuralTown,
                    count = 3,
                    population = 8_000,
                    radiusMeters = 3_000,
                    economy = EconomicIdentity.Agriculture,
                    minDistanceKm = 10
                },

                // مدن صناعية
                new CityConfig
                {
                    archetype = CityArchetype.IndustrialCity,
                    count = 2,
                    population = 180_000,
                    radiusMeters = 12_000,
                    economy = EconomicIdentity.Manufacturing,
                    minDistanceKm = 25
                },

                // مدن جامعية
                new CityConfig
                {
                    archetype = CityArchetype.UniversityCity,
                    count = 1,
                    population = 50_000,
                    radiusMeters = 5_000,
                    economy = EconomicIdentity.Technology,
                    minDistanceKm = 20
                },

                // مدن سياحية ساحلية
                new CityConfig
                {
                    archetype = CityArchetype.CoastalCity,
                    count = 2,
                    population = 95_000,
                    radiusMeters = 6_000,
                    economy = EconomicIdentity.Tourism,
                    minDistanceKm = 30
                }
            };

            return configs;
        }

        /// <summary>
        /// البحث عن موقع صالح للمدينة الجديدة
        /// </summary>
        private Vector2? FindValidCityLocation(List<CityMasterPlanData> existingCities, 
                                               WorldBoundsData bounds, 
                                               TerrainAnalysisData terrain,
                                               float minDistanceKm)
        {
            float minDistanceMeters = minDistanceKm * 1000;

            // محاولة العثور على موقع عشوائي صحيح
            int attempts = 0;
            while (attempts < 20)
            {
                var position = bounds.GetRandomPositionInBounds(random);

                // ============================================
                // 1. التحقق من المسافة من المدن الأخرى
                // ============================================
                bool tooClose = false;
                foreach (var city in existingCities)
                {
                    float distance = Vector2.Distance(position, city.position);
                    if (distance < minDistanceMeters)
                    {
                        tooClose = true;
                        break;
                    }
                }

                if (tooClose)
                {
                    attempts++;
                    continue;
                }

                // ============================================
                // 2. التحقق من أن الموقع في منطقة صالحة للبناء
                // ============================================
                var zone = GetTerrainZoneAt(position, terrain);
                if (zone != null && zone.isBuildable)
                {
                    return position;
                }

                attempts++;
            }

            return null;  // فشل العثور على موقع صالح
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
                if (distance < zone.radiusMeters && distance < minDistance)
                {
                    minDistance = distance;
                    closest = zone;
                }
            }

            return closest;
        }

        /// <summary>
        /// فئة تخزين إعدادات نوع المدينة
        /// </summary>
        private class CityConfig
        {
            public CityArchetype archetype;
            public int count;
            public int population;
            public float radiusMeters;
            public EconomicIdentity economy;
            public float minDistanceKm;
        }

        /// <summary>
        /// التحقق من أن جميع المدن تحقق المتطلبات
        /// </summary>
        public bool ValidateCityDistribution(List<CityMasterPlanData> cities, float minDistanceKm)
        {
            float minDistanceMeters = minDistanceKm * 1000;

            for (int i = 0; i < cities.Count; i++)
            {
                for (int j = i + 1; j < cities.Count; j++)
                {
                    float distance = Vector2.Distance(cities[i].position, cities[j].position);
                    if (distance < minDistanceMeters)
                    {
                        Debug.LogWarning($"[CityDistributionGenerator] Cities too close: {cities[i].name} and {cities[j].name} ({distance / 1000}km)");
                        return false;
                    }
                }
            }

            return true;
        }
    }
}
