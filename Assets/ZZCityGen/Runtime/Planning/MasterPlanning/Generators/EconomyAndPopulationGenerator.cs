using System;
using System.Collections.Generic;
using UnityEngine;

namespace ZZCityGen.Planning.MasterPlanning.Generators
{
    /// <summary>
    /// المرحلة 2.9 و 2.10: الاقتصاد والسكان
    /// Generates economy and population distribution
    /// 
    /// المرحلة 2.9: كل مدينة تحصل على هوية
    /// - Technology (تكنولوجيا)
    /// - Manufacturing (تصنيع)
    /// - Tourism (سياحة)
    /// - إلخ
    /// 
    /// المرحلة 2.10: حساب السكان (ليس عشوائي)
    /// - Capital: 800,000
    /// - Family City: 120,000
    /// - Rural Town: 8,000
    /// </summary>
    public class EconomyAndPopulationGenerator
    {
        private readonly System.Random random;

        public EconomyAndPopulationGenerator(int seed)
        {
            this.random = new System.Random(seed);
        }

        /// <summary>
        /// توليد الاقتصاد والسكان
        /// </summary>
        public void GenerateEconomyAndPopulation(
            List<CityMasterPlanData> cities,
            GlobalEconomyPlanData economy,
            PopulationDistributionData population)
        {
            // ============================================
            // المرحلة 2.9: تحديد الهوية الاقتصادية
            // ============================================
            AssignEconomicIdentities(cities);

            // ============================================
            // المرحلة 2.10: حساب السكان
            // ============================================
            CalculatePopulation(cities, population);

            // ============================================
            // تحديث إحصائيات الاقتصاد
            // ============================================
            UpdateEconomyStatistics(cities, economy, population);

            Debug.Log("[EconomyAndPopulationGenerator] Economy and population generated:");
            Debug.Log($"  Total Population: {population.totalPopulation:N0}");
            Debug.Log($"  Capital Population: {population.capitalPopulation:N0}");
            Debug.Log($"  Average Population per City: {population.totalPopulation / Mathf.Max(1, cities.Count):N0}");
        }

        /// <summary>
        /// المرحلة 2.9: تحديد الهوية الاقتصادية لكل مدينة
        /// </summary>
        private void AssignEconomicIdentities(List<CityMasterPlanData> cities)
        {
            int technologyHubs = 0;
            int manufacturingCenters = 0;
            int tourismCenters = 0;

            foreach (var city in cities)
            {
                // القاعدة: تم تحديد الهوية بالفعل في CityDistributionGenerator
                // لكن قد نحتاج لتحديثها بناءً على موقع المدينة

                switch (city.archetype)
                {
                    case CityArchetype.Capital:
                        city.economicIdentity = EconomicIdentity.Finance;
                        break;

                    case CityArchetype.IndustrialCity:
                        city.economicIdentity = EconomicIdentity.Manufacturing;
                        manufacturingCenters++;
                        break;

                    case CityArchetype.UniversityCity:
                        city.economicIdentity = EconomicIdentity.Technology;
                        technologyHubs++;
                        break;

                    case CityArchetype.CoastalCity:
                    case CityArchetype.TourismDestination:
                        city.economicIdentity = EconomicIdentity.Tourism;
                        tourismCenters++;
                        break;

                    case CityArchetype.RuralTown:
                        city.economicIdentity = EconomicIdentity.Agriculture;
                        break;

                    case CityArchetype.FamilyCity:
                        city.economicIdentity = EconomicIdentity.Services;
                        break;
                }

                Debug.Log($"[Economy] {city.name}: {city.economicIdentity}");
            }
        }

        /// <summary>
        /// المرحلة 2.10: حساب السكان لكل مدينة
        /// </summary>
        private void CalculatePopulation(
            List<CityMasterPlanData> cities,
            PopulationDistributionData population)
        {
            foreach (var city in cities)
            {
                // تم تحديد targetPopulation بالفعل في CityDistributionGenerator
                // لكن قد نضيف تعديلات بناءً على العوامل

                // تطبيق معامل التأثير:
                // المدن الأقرب من العاصمة قد يكون لديها سكان أكثر
                float proximityBonus = 1.0f;
                if (!city.isCapital && city.distanceFromCapitalKm > 0)
                {
                    // المدن البعيدة قد يكون لديها سكان أقل
                    proximityBonus = Mathf.Max(0.7f, 1.0f - (city.distanceFromCapitalKm / 100));
                }

                // تطبيق معامل البونص
                int finalPopulation = Mathf.RoundToInt(city.targetPopulation * proximityBonus);
                city.targetPopulation = finalPopulation;

                // تحديث توزيع السكان
                switch (city.archetype)
                {
                    case CityArchetype.Capital:
                        population.capitalPopulation += finalPopulation;
                        break;
                    case CityArchetype.FamilyCity:
                        population.familyCitiesPopulation += finalPopulation;
                        break;
                    case CityArchetype.IndustrialCity:
                        population.industrialCitiesPopulation += finalPopulation;
                        break;
                    case CityArchetype.RuralTown:
                        population.ruralPopulation += finalPopulation;
                        break;
                    case CityArchetype.CoastalCity:
                    case CityArchetype.TourismDestination:
                        population.coastalCitiesPopulation += finalPopulation;
                        break;
                }

                Debug.Log($"[Population] {city.name}: {finalPopulation:N0} inhabitants");
            }
        }

        /// <summary>
        /// تحديث إحصائيات الاقتصاد العالمي
        /// </summary>
        private void UpdateEconomyStatistics(
            List<CityMasterPlanData> cities,
            GlobalEconomyPlanData economy,
            PopulationDistributionData population)
        {
            // عد المناطق الاقتصادية المختلفة
            int technologyHubs = 0;
            int industrialZones = 0;
            int agriculturalZones = 0;
            int tourismCenters = 0;

            foreach (var city in cities)
            {
                switch (city.economicIdentity)
                {
                    case EconomicIdentity.Technology:
                        technologyHubs++;
                        break;
                    case EconomicIdentity.Manufacturing:
                        industrialZones++;
                        break;
                    case EconomicIdentity.Agriculture:
                        agriculturalZones++;
                        break;
                    case EconomicIdentity.Tourism:
                        tourismCenters++;
                        break;
                }
            }

            economy.technologyHubs = technologyHubs;
            economy.majorIndustrialZones = industrialZones;
            economy.agriculturalZones = agriculturalZones;
            economy.tourismCenters = tourismCenters;

            // حساب إجمالي السكان بالملايين
            economy.totalPlannedPopulationMillions = population.totalPopulation / 1_000_000;

            // حساب معدل التوظيف المتوقع (75% للمدن المتقدمة)
            economy.expectedEmploymentRate = 0.75f;
        }

        /// <summary>
        /// الحصول على نسبة التوزيع السكاني
        /// </summary>
        public Dictionary<string, float> GetPopulationDistribution(PopulationDistributionData population)
        {
            int total = population.totalPopulation;
            if (total == 0) total = 1;

            return new Dictionary<string, float>
            {
                { "Capital", (float)population.capitalPopulation / total },
                { "Family Cities", (float)population.familyCitiesPopulation / total },
                { "Industrial", (float)population.industrialCitiesPopulation / total },
                { "Rural", (float)population.ruralPopulation / total },
                { "Coastal", (float)population.coastalCitiesPopulation / total }
            };
        }

        /// <summary>
        /// الحصول على النمو الاقتصادي المتوقع
        /// </summary>
        public float CalculateExpectedGrowth(List<CityMasterPlanData> cities)
        {
            float totalGrowth = 0;
            foreach (var city in cities)
            {
                totalGrowth += city.CalculateGrowthRate();
            }

            return totalGrowth / Mathf.Max(1, cities.Count);
        }
    }
}
