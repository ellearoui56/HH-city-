using System;
using System.Collections.Generic;
using UnityEngine;

namespace ZZCityGen.Planning.MasterPlanning
{
    /// <summary>
    /// أنواع استخدام الأراضي العالمية
    /// Global land use types
    /// </summary>
    public enum GlobalLandUseType
    {
        Residential,     // سكني
        Commercial,      // تجاري
        Industrial,      // صناعي
        Agricultural,    // زراعي
        Government,      // حكومي
        Parks,           // حدائق
        Water,           // مياه
        Reserved         // محفوظ للتطوير
    }

    /// <summary>
    /// خطة توزيع الأراضي العالمية
    /// Global land allocation plan
    /// </summary>
    [Serializable]
    public class LandAllocationData
    {
        public GlobalLandUseType type;
        public float percentageOfWorld;
        public float estimatedAreaSquareKm;
        
        /// <summary>
        /// الأولوية للتطوير (1-10)
        /// </summary>
        public int developmentPriority;

        public LandAllocationData() { }

        public LandAllocationData(GlobalLandUseType type, float percentage, float area, int priority)
        {
            this.type = type;
            this.percentageOfWorld = percentage;
            this.estimatedAreaSquareKm = area;
            this.developmentPriority = priority;
        }
    }

    /// <summary>
    /// خطة اقتصادية للعالم
    /// Global economy plan
    /// </summary>
    [Serializable]
    public class GlobalEconomyPlanData
    {
        /// <summary>
        /// عدد المناطق الصناعية الرئيسية
        /// </summary>
        public int majorIndustrialZones;
        
        /// <summary>
        /// عدد مراكز التكنولوجيا
        /// </summary>
        public int technologyHubs;
        
        /// <summary>
        /// عدد المناطق الزراعية
        /// </summary>
        public int agriculturalZones;
        
        /// <summary>
        /// عدد مراكز السياحة
        /// </summary>
        public int tourismCenters;
        
        /// <summary>
        /// إجمالي السكان المخطط (مليون)
        /// </summary>
        public int totalPlannedPopulationMillions;
        
        /// <summary>
        /// نسبة التوظيف المتوقعة (%)
        /// </summary>
        public float expectedEmploymentRate;

        public GlobalEconomyPlanData()
        {
            majorIndustrialZones = 3;
            technologyHubs = 2;
            agriculturalZones = 4;
            tourismCenters = 2;
            totalPlannedPopulationMillions = 10;
            expectedEmploymentRate = 0.75f;  // 75%
        }
    }

    /// <summary>
    /// ملخص السكان العالمي
    /// Global population summary
    /// </summary>
    [Serializable]
    public class PopulationDistributionData
    {
        public int capitalPopulation;
        public int familyCitiesPopulation;
        public int industrialCitiesPopulation;
        public int ruralPopulation;
        public int coastalCitiesPopulation;
        
        /// <summary>
        /// الإجمالي
        /// </summary>
        public int totalPopulation => capitalPopulation + familyCitiesPopulation + 
                                      industrialCitiesPopulation + ruralPopulation + 
                                      coastalCitiesPopulation;

        public PopulationDistributionData()
        {
            capitalPopulation = 0;
            familyCitiesPopulation = 0;
            industrialCitiesPopulation = 0;
            ruralPopulation = 0;
            coastalCitiesPopulation = 0;
        }

        /// <summary>
        /// حساب الكثافة السكانية (نسمة/كم²)
        /// </summary>
        public float CalculatePopulationDensity(float worldAreaSquareKm)
        {
            return totalPopulation / worldAreaSquareKm;
        }
    }

    /// <summary>
    /// نتائج التحقق من التناقضات
    /// Validation check results
    /// </summary>
    [Serializable]
    public class MasterPlanValidationData
    {
        public List<string> errors = new List<string>();
        public List<string> warnings = new List<string>();
        
        /// <summary>
        /// هل خطة التخطيط صحيحة؟
        /// </summary>
        public bool isValid => errors.Count == 0;

        public void AddError(string error)
        {
            errors.Add(error);
        }

        public void AddWarning(string warning)
        {
            warnings.Add(warning);
        }

        public void Clear()
        {
            errors.Clear();
            warnings.Clear();
        }
    }

    /// <summary>
    /// طبقة التخطيط الفردية
    /// Individual planning layer
    /// </summary>
    [Serializable]
    public class PlanningLayerData
    {
        public string layerName;
        public bool isComplete;
        public List<string> decisions = new List<string>();
        public long timestampGenerated;

        public PlanningLayerData() { }

        public PlanningLayerData(string name)
        {
            layerName = name;
            isComplete = false;
            timestampGenerated = System.DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        }

        public void AddDecision(string decision)
        {
            decisions.Add(decision);
        }

        public void MarkComplete()
        {
            isComplete = true;
        }
    }
}
