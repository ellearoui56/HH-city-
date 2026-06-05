using System;
using System.Collections.Generic;
using UnityEngine;

namespace ZZCityGen.Planning.TerrainGeneration
{
    /// <summary>
    /// منطقة مشهودة للتوسع
    /// </summary>
    [Serializable]
    public class ExpansionAreaData
    {
        public Vector2 center;
        public float radiusMeters;
        public float buildabilityScore;
        public int cityIndex;

        public ExpansionAreaData() { }

        public ExpansionAreaData(Vector2 center, float radius, float score, int cityIndex)
        {
            this.center = center;
            this.radiusMeters = radius;
            this.buildabilityScore = score;
            this.cityIndex = cityIndex;
        }
    }

    /// <summary>
    /// ممر طريق آمن
    /// </summary>
    [Serializable]
    public class HighwayCorridorData
    {
        public int fromCityIndex;
        public int toCityIndex;
        public List<Vector2> suggestedPath = new List<Vector2>();

        /// <summary>
        /// متوسط الانحدار على المسار
        /// </summary>
        public float avgSlope;

        /// <summary>
        /// هل يحتاج جسور؟
        /// </summary>
        public bool needsBridges;

        /// <summary>
        /// هل يحتاج أنفاق؟
        /// </summary>
        public bool needsTunnels;

        /// <summary>
        /// جودة المسار (0-1)
        /// </summary>
        public float pathQuality;

        public HighwayCorridorData() { }

        public HighwayCorridorData(int from, int to)
        {
            this.fromCityIndex = from;
            this.toCityIndex = to;
            this.suggestedPath = new List<Vector2>();
        }
    }

    /// <summary>
    /// منطقة خطر فيضان
    /// </summary>
    [Serializable]
    public class FloodRiskData
    {
        public Vector2 center;
        public float radiusMeters;

        /// <summary>
        /// درجة الخطر (0-1)
        /// </summary>
        public float riskLevel;

        /// <summary>
        /// نوع الفيضان
        /// </summary>
        public string floodType;  // River, Lake, Flash

        public FloodRiskData() { }

        public FloodRiskData(Vector2 center, float radius, float risk, string type)
        {
            this.center = center;
            this.radiusMeters = radius;
            this.riskLevel = risk;
            this.floodType = type;
        }
    }

    /// <summary>
    /// منطقة خطر انزلاق أرضي
    /// </summary>
    [Serializable]
    public class LandslideRiskData
    {
        public Vector2 center;
        public float radiusMeters;

        /// <summary>
        /// درجة الخطر (0-1)
        /// </summary>
        public float riskLevel;

        /// <summary>
        /// السبب الرئيسي
        /// </summary>
        public string cause;  // SteepSlope, WaterErosion, MountainBase

        public LandslideRiskData() { }

        public LandslideRiskData(Vector2 center, float radius, float risk, string cause)
        {
            this.center = center;
            this.radiusMeters = radius;
            this.riskLevel = risk;
            this.cause = cause;
        }
    }

    /// <summary>
    /// منطقة مشهودة للمطار المستقبلي
    /// </summary>
    [Serializable]
    public class FutureAirportAreaData
    {
        public Vector2 suggestedLocation;

        /// <summary>
        /// جودة الموقع (0-1)
        /// </summary>
        public float suitabilityScore;

        /// <summary>
        /// المدينة التي سيخدمها
        /// </summary>
        public int associatedCityIndex;

        /// <summary>
        /// الحد الأدنى للمساحة المطلوبة
        /// </summary>
        public float requiredAreaSquareKm;

        public FutureAirportAreaData() { }

        public FutureAirportAreaData(Vector2 location, float score, int cityIndex, float area)
        {
            this.suggestedLocation = location;
            this.suitabilityScore = score;
            this.associatedCityIndex = cityIndex;
            this.requiredAreaSquareKm = area;
        }
    }

    /// <summary>
    /// تحليل متقدم للتضاريس
    /// يُستخدم لاتخاذ القرارات الذكية في المراحل التالية
    /// </summary>
    [Serializable]
    public class TerrainAnalysisData
    {
        /// <summary>
        /// مناطق التوسع الممتازة
        /// </summary>
        public List<ExpansionAreaData> bestExpansionAreas = new List<ExpansionAreaData>();

        /// <summary>
        /// ممرات الطرق المقترحة
        /// </summary>
        public List<HighwayCorridorData> highwayCorridors = new List<HighwayCorridorData>();

        /// <summary>
        /// مناطق خطر الفيضان
        /// </summary>
        public List<FloodRiskData> floodRisks = new List<FloodRiskData>();

        /// <summary>
        /// مناطق خطر الانزلاق
        /// </summary>
        public List<LandslideRiskData> landslideRisks = new List<LandslideRiskData>();

        /// <summary>
        /// مناطق المطارات المستقبلية
        /// </summary>
        public List<FutureAirportAreaData> futureAirportAreas = new List<FutureAirportAreaData>();

        /// <summary>
        /// أفضل مناطق التطوير الصناعي
        /// </summary>
        public List<Vector2> bestIndustrialZones = new List<Vector2>();

        /// <summary>
        /// أفضل مناطق التطوير السكني
        /// </summary>
        public List<Vector2> bestResidentialZones = new List<Vector2>();

        /// <summary>
        /// أفضل مناطق التطوير التجاري
        /// </summary>
        public List<Vector2> bestCommercialZones = new List<Vector2>();

        /// <summary>
        /// مناطق الحفظ الطبيعي (بدون تطوير)
        /// </summary>
        public List<Vector2> conservationAreas = new List<Vector2>();

        public TerrainAnalysisData()
        {
            bestExpansionAreas = new List<ExpansionAreaData>();
            highwayCorridors = new List<HighwayCorridorData>();
            floodRisks = new List<FloodRiskData>();
            landslideRisks = new List<LandslideRiskData>();
            futureAirportAreas = new List<FutureAirportAreaData>();
            bestIndustrialZones = new List<Vector2>();
            bestResidentialZones = new List<Vector2>();
            bestCommercialZones = new List<Vector2>();
            conservationAreas = new List<Vector2>();
        }
    }

    /// <summary>
    /// نتائج فحص التناقضات
    /// </summary>
    [Serializable]
    public class TerrainValidationData
    {
        public List<string> errors = new List<string>();
        public List<string> warnings = new List<string>();

        /// <summary>
        /// هل التضاريس صحيحة؟
        /// </summary>
        public bool isValid => errors.Count == 0;

        public void AddError(string error) => errors.Add(error);
        public void AddWarning(string warning) => warnings.Add(warning);
        public void Clear()
        {
            errors.Clear();
            warnings.Clear();
        }
    }
}
