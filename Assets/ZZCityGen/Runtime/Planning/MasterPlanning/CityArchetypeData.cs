using System;
using UnityEngine;

namespace ZZCityGen.Planning.MasterPlanning
{
    /// <summary>
    /// أنماط المدن المختلفة
    /// Different city archetypes
    /// </summary>
    public enum CityArchetype
    {
        Capital,           // العاصمة
        FamilyCity,        // مدينة عائلية
        IndustrialCity,    // مدينة صناعية
        RuralTown,         // مدينة ريفية
        CoastalCity,       // مدينة ساحلية
        UniversityCity,    // مدينة جامعية
        TourismDestination // مقصد سياحي
    }

    /// <summary>
    /// الهوية الاقتصادية للمدينة
    /// City's economic identity
    /// </summary>
    public enum EconomicIdentity
    {
        Technology,      // تكنولوجيا
        Manufacturing,   // تصنيع
        Services,        // خدمات
        Agriculture,     // زراعة
        Tourism,         // سياحة
        Finance,         // مالية
        Mixed            // متنوع
    }

    /// <summary>
    /// بيانات مرشح موقع المدينة
    /// City location candidate data
    /// </summary>
    [Serializable]
    public class CityCandidateData
    {
        public Vector2 position;
        public float accessibilityScore;  // 0-100
        public float expansibilityScore;  // 0-100
        public float resourceScore;       // 0-100
        public float strategicScore;      // 0-100

        /// <summary>
        /// النقاط الكلية للترشيح
        /// </summary>
        public float totalScore => (accessibilityScore + expansibilityScore + 
                                   resourceScore + strategicScore) / 4f;

        public CityCandidateData() { }

        public CityCandidateData(Vector2 position, float accessibility, 
                                 float expansibility, float resource, float strategic)
        {
            this.position = position;
            this.accessibilityScore = accessibility;
            this.expansibilityScore = expansibility;
            this.resourceScore = resource;
            this.strategicScore = strategic;
        }
    }

    /// <summary>
    /// بيانات العاصمة المختارة
    /// Selected capital data
    /// </summary>
    [Serializable]
    public class CapitalLocationData
    {
        public Vector2 position;
        public float accessibilityScore;
        public float targetPopulation;
        public float radiusMeters;

        /// <summary>
        /// المناطق الصناعية المخططة
        /// </summary>
        public int plannedIndustrialZones;
        
        /// <summary>
        /// مراكز الابتكار
        /// </summary>
        public int innovationHubs;

        public CapitalLocationData() { }

        public CapitalLocationData(Vector2 position, float score, 
                                   float population, float radius)
        {
            this.position = position;
            this.accessibilityScore = score;
            this.targetPopulation = population;
            this.radiusMeters = radius;
            this.plannedIndustrialZones = 3;
            this.innovationHubs = 2;
        }
    }

    /// <summary>
    /// بيانات المدينة الموحدة
    /// Unified city data
    /// </summary>
    [Serializable]
    public class CityMasterPlanData
    {
        public string name;
        public CityArchetype archetype;
        public Vector2 position;
        public float radiusMeters;
        
        /// <summary>
        /// السكان المستهدفون
        /// </summary>
        public int targetPopulation;
        
        /// <summary>
        /// الهوية الاقتصادية
        /// </summary>
        public EconomicIdentity economicIdentity;
        
        /// <summary>
        /// هل هذه عاصمة؟
        /// </summary>
        public bool isCapital;
        
        /// <summary>
        /// هل هذه ساحلية؟
        /// </summary>
        public bool isCoastal;
        
        /// <summary>
        /// المسافة من العاصمة (كم)
        /// </summary>
        public float distanceFromCapitalKm;
        
        /// <summary>
        /// الحد الأدنى للمسافة من مدن أخرى (كم)
        /// </summary>
        public float minimumDistanceToOtherCitiesKm = 10;

        public CityMasterPlanData() { }

        public CityMasterPlanData(string name, CityArchetype archetype, Vector2 position,
                                  float radius, int population, EconomicIdentity economy)
        {
            this.name = name;
            this.archetype = archetype;
            this.position = position;
            this.radiusMeters = radius;
            this.targetPopulation = population;
            this.economicIdentity = economy;
            this.isCapital = archetype == CityArchetype.Capital;
            this.isCoastal = false;
        }

        /// <summary>
        /// حساب درجة نمو المدينة المتوقعة
        /// </summary>
        public float CalculateGrowthRate()
        {
            return archetype switch
            {
                CityArchetype.Capital => 0.08f,              // 8% سنويًا
                CityArchetype.FamilyCity => 0.05f,           // 5% سنويًا
                CityArchetype.IndustrialCity => 0.06f,       // 6% سنويًا
                CityArchetype.RuralTown => 0.02f,            // 2% سنويًا
                CityArchetype.CoastalCity => 0.07f,          // 7% سنويًا
                CityArchetype.UniversityCity => 0.04f,       // 4% سنويًا
                CityArchetype.TourismDestination => 0.05f,   // 5% سنويًا
                _ => 0.03f
            };
        }
    }
}
