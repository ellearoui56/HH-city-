using System;
using System.Collections.Generic;
using UnityEngine;

namespace ZZCityGen.Planning.MasterPlanning.Generators
{
    /// <summary>
    /// المرحلة 2.11: توزيع الأراضي
    /// Generates global land allocation plan
    /// 
    /// قبل إنشاء الأحياء، نحدد توزيع الأراضي عالميًا:
    /// - Residential (سكني)
    /// - Commercial (تجاري)
    /// - Industrial (صناعي)
    /// - Agricultural (زراعي)
    /// - Government (حكومي)
    /// - Parks (حدائق)
    /// </summary>
    public class LandAllocationGenerator
    {
        private readonly System.Random random;

        public LandAllocationGenerator(int seed)
        {
            this.random = new System.Random(seed);
        }

        /// <summary>
        /// توليد خطة توزيع الأراضي العالمية
        /// </summary>
        public List<LandAllocationData> GenerateLandAllocation(
            float worldAreaSquareKm,
            TerrainAnalysisData terrain,
            ClimateAnalysisData climate,
            GlobalEconomyPlanData economy)
        {
            var allocation = new List<LandAllocationData>();

            // ============================================
            // 1. المناطق الزراعية (30%)
            // ============================================
            // في المناطق الصالحة للزراعة
            var agricultureAllocation = new LandAllocationData(
                GlobalLandUseType.Agricultural,
                0.30f,
                worldAreaSquareKm * 0.30f,
                priority: 7
            );
            allocation.Add(agricultureAllocation);

            // ============================================
            // 2. المناطق السكنية (20%)
            // ============================================
            var residentialAllocation = new LandAllocationData(
                GlobalLandUseType.Residential,
                0.20f,
                worldAreaSquareKm * 0.20f,
                priority: 8
            );
            allocation.Add(residentialAllocation);

            // ============================================
            // 3. الحدائق والمساحات الخضراء (20%)
            // ============================================
            var parksAllocation = new LandAllocationData(
                GlobalLandUseType.Parks,
                0.20f,
                worldAreaSquareKm * 0.20f,
                priority: 5
            );
            allocation.Add(parksAllocation);

            // ============================================
            // 4. المناطق الصناعية (10%)
            // ============================================
            var industrialAllocation = new LandAllocationData(
                GlobalLandUseType.Industrial,
                0.10f,
                worldAreaSquareKm * 0.10f,
                priority: 9
            );
            allocation.Add(industrialAllocation);

            // ============================================
            // 5. المناطق التجارية (8%)
            // ============================================
            var commercialAllocation = new LandAllocationData(
                GlobalLandUseType.Commercial,
                0.08f,
                worldAreaSquareKm * 0.08f,
                priority: 8
            );
            allocation.Add(commercialAllocation);

            // ============================================
            // 6. المناطق الحكومية (2%)
            // ============================================
            var governmentAllocation = new LandAllocationData(
                GlobalLandUseType.Government,
                0.02f,
                worldAreaSquareKm * 0.02f,
                priority: 6
            );
            allocation.Add(governmentAllocation);

            // ============================================
            // 7. المياه والتضاريس غير صالحة (10%)
            // ============================================
            var waterAllocation = new LandAllocationData(
                GlobalLandUseType.Water,
                terrain.waterPercentage / 100,
                worldAreaSquareKm * (terrain.waterPercentage / 100),
                priority: 1
            );
            allocation.Add(waterAllocation);

            // ============================================
            // 8. المناطق المحفوظة للتطوير المستقبلي (remaining)
            // ============================================
            float totalAllocated = 0;
            foreach (var land in allocation)
            {
                if (land.type != GlobalLandUseType.Water)
                    totalAllocated += land.percentageOfWorld;
            }

            float reservedPercentage = 1.0f - totalAllocated - (terrain.waterPercentage / 100);
            if (reservedPercentage > 0)
            {
                var reservedAllocation = new LandAllocationData(
                    GlobalLandUseType.Reserved,
                    reservedPercentage,
                    worldAreaSquareKm * reservedPercentage,
                    priority: 2
                );
                allocation.Add(reservedAllocation);
            }

            Debug.Log("[LandAllocationGenerator] Land allocation generated:");
            foreach (var land in allocation)
            {
                Debug.Log($"  {land.type}: {land.percentageOfWorld * 100:F1}% ({land.estimatedAreaSquareKm:F2} km²)");
            }

            return allocation;
        }

        /// <summary>
        /// الحصول على إجمالي الأراضي المتاحة للتطوير
        /// </summary>
        public float GetDevelopableArea(List<LandAllocationData> allocation)
        {
            float developable = 0;
            foreach (var land in allocation)
            {
                if (land.type != GlobalLandUseType.Water &&
                    land.type != GlobalLandUseType.Parks &&
                    land.type != GlobalLandUseType.Reserved)
                {
                    developable += land.estimatedAreaSquareKm;
                }
            }
            return developable;
        }

        /// <summary>
        /// الحصول على الأراضي حسب النوع
        /// </summary>
        public LandAllocationData GetAllocationByType(List<LandAllocationData> allocation, GlobalLandUseType type)
        {
            foreach (var land in allocation)
            {
                if (land.type == type)
                    return land;
            }
            return null;
        }

        /// <summary>
        /// حساب نسبة استخدام الأراضي
        /// </summary>
        public float GetLandUtilizationRatio(List<LandAllocationData> allocation)
        {
            float utilized = 0;
            float total = 0;

            foreach (var land in allocation)
            {
                if (land.type != GlobalLandUseType.Water &&
                    land.type != GlobalLandUseType.Reserved)
                {
                    utilized += land.percentageOfWorld;
                }
                if (land.type != GlobalLandUseType.Water)
                {
                    total += land.percentageOfWorld;
                }
            }

            return total > 0 ? utilized / total : 0;
        }
    }
}
