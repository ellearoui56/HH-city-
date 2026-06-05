using System;
using System.Collections.Generic;
using UnityEngine;

namespace ZZCityGen.Planning.MasterPlanning
{
    /// <summary>
    /// خطة التخطيط الرئيسية الشاملة
    /// THE comprehensive master planning data structure
    /// 
    /// This is the single output file: MasterPlan.json
    /// يحتوي على جميع القرارات التي ستوجه التطوير اللاحق
    /// </summary>
    [Serializable]
    public class MasterPlanData
    {
        // ============================================
        // الطبقة 1: حدود العالم (World Layer)
        // ============================================
        public WorldBoundsData worldBounds;
        public float worldAreaSquareKm;
        
        // ============================================
        // الطبقة 2: التضاريس (Terrain Layer)
        // ============================================
        public TerrainAnalysisData terrainAnalysis;
        
        // ============================================
        // الطبقة 3: المناخ (Climate Layer)
        // ============================================
        public ClimateAnalysisData climateAnalysis;
        
        // ============================================
        // الطبقة 4: النقل (Transport Layer)
        // ============================================
        public TransportNetworkData transportNetwork;
        
        // ============================================
        // الطبقة 5: المدن (City Layer)
        // ============================================
        public CapitalLocationData capital;
        public List<CityMasterPlanData> cities = new List<CityMasterPlanData>();
        
        // ============================================
        // الطبقة 6: الأحياء (District Layer)
        // ============================================
        public List<LandAllocationData> globalLandAllocation = new List<LandAllocationData>();
        
        // ============================================
        // الطبقة 7: البنية التحتية (Infrastructure Layer)
        // ============================================
        public InfrastructureMasterPlanData infrastructure;
        
        // ============================================
        // بيانات اقتصادية
        // ============================================
        public GlobalEconomyPlanData economy;
        public PopulationDistributionData populationDistribution;
        
        // ============================================
        // ملخص الخطة
        // ============================================
        
        /// <summary>
        /// إجمالي عدد المدن (بما فيها العاصمة)
        /// </summary>
        public int totalCities;
        
        /// <summary>
        /// عدد المطارات المخطط لها
        /// </summary>
        public int totalAirports => infrastructure?.airports?.Count ?? 0;
        
        /// <summary>
        /// عدد الموانئ المخطط لها
        /// </summary>
        public int totalPorts => infrastructure?.ports?.Count ?? 0;
        
        /// <summary>
        /// إجمالي الطرق المخطط لها (كم)
        /// </summary>
        public float totalPlannedRoadsKm => transportNetwork?.totalPlannedRoadsKm ?? 0;
        
        /// <summary>
        /// إجمالي السكان المخطط
        /// </summary>
        public int totalPlannedPopulation => populationDistribution?.totalPopulation ?? 0;
        
        // ============================================
        // البيانات التشغيلية
        // ============================================
        
        /// <summary>
        /// البذرة العشوائية المستخدمة
        /// </summary>
        public int seed;
        
        /// <summary>
        /// اسم العالم
        /// </summary>
        public string worldName;
        
        /// <summary>
        /// وقت التوليد (Unix timestamp)
        /// </summary>
        public long generationTimestamp;
        
        /// <summary>
        /// رقم الإصدار (للتحديثات المستقبلية)
        /// </summary>
        public int schemaVersion = 1;
        
        /// <summary>
        /// نتائج التحقق من التناقضات
        /// </summary>
        public MasterPlanValidationData validation;
        
        /// <summary>
        /// حالات طبقات التخطيط
        /// </summary>
        public List<PlanningLayerData> planningLayers = new List<PlanningLayerData>();

        public MasterPlanData()
        {
            worldBounds = new WorldBoundsData();
            terrainAnalysis = new TerrainAnalysisData();
            climateAnalysis = new ClimateAnalysisData();
            transportNetwork = new TransportNetworkData();
            cities = new List<CityMasterPlanData>();
            globalLandAllocation = new List<LandAllocationData>();
            infrastructure = new InfrastructureMasterPlanData();
            economy = new GlobalEconomyPlanData();
            populationDistribution = new PopulationDistributionData();
            validation = new MasterPlanValidationData();
            
            generationTimestamp = System.DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            schemaVersion = 1;
        }

        /// <summary>
        /// إضافة مدينة إلى الخطة
        /// </summary>
        public void AddCity(CityMasterPlanData city)
        {
            cities.Add(city);
            totalCities = cities.Count + (capital != null ? 1 : 0);
        }

        /// <summary>
        /// حساب الكثافة السكانية العالمية
        /// </summary>
        public float GetGlobalPopulationDensity()
        {
            if (worldAreaSquareKm <= 0) return 0;
            return populationDistribution?.CalculatePopulationDensity(worldAreaSquareKm) ?? 0;
        }

        /// <summary>
        /// الحصول على ملخص إحصائي للخطة
        /// </summary>
        public string GetSummary()
        {
            return $@"
=== MASTER PLAN SUMMARY ===
World: {worldName}
Seed: {seed}

GEOGRAPHY:
  Area: {worldAreaSquareKm:F2} km²
  Bounds: X[{worldBounds?.minX}-{worldBounds?.maxX}], Z[{worldBounds?.minZ}-{worldBounds?.maxZ}]

CLIMATE:
  Dominant Climate: {climateAnalysis?.dominantClimate}
  Agricultural Potential: {climateAnalysis?.agriculturePotentialPercentage}%
  Average Temperature: {climateAnalysis?.globalAverageTemperature}°C

CITIES:
  Total Cities: {totalCities}
  Capital: {capital?.name} @ {capital?.position}
  Capital Population: {capital?.targetPopulation}

POPULATION:
  Total Population: {totalPlannedPopulation:N0}
  Density: {GetGlobalPopulationDensity():F2} people/km²

TRANSPORT:
  Planned Roads: {totalPlannedRoadsKm:F2} km
  Highways: {transportNetwork?.highwayCount}
  Secondary Roads: {transportNetwork?.secondaryRoadCount}

INFRASTRUCTURE:
  Airports: {totalAirports}
  Ports: {totalPorts}

VALIDATION:
  Valid: {validation?.isValid}
  Errors: {validation?.errors?.Count ?? 0}
  Warnings: {validation?.warnings?.Count ?? 0}
";
        }

        /// <summary>
        /// تحديث حالة الطبقة
        /// </summary>
        public void SetLayerComplete(string layerName)
        {
            var layer = planningLayers.Find(l => l.layerName == layerName);
            if (layer != null)
            {
                layer.MarkComplete();
            }
        }

        /// <summary>
        /// هل جميع الطبقات مكتملة؟
        /// </summary>
        public bool AreAllLayersComplete()
        {
            foreach (var layer in planningLayers)
            {
                if (!layer.isComplete)
                    return false;
            }
            return true;
        }
    }
}
