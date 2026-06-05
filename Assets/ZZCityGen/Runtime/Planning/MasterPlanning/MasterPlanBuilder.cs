using System;
using System.Collections.Generic;
using UnityEngine;
using ZZCityGen.Planning.MasterPlanning.Generators;

namespace ZZCityGen.Planning.MasterPlanning
{
    /// <summary>
    /// محرك التخطيط العالمي الرئيسي
    /// The Master Plan Engine - Core orchestrator
    /// 
    /// هذا هو محرك "مهندس المدن الافتراضي" الذي:
    /// 1. يرسم العالم بالكامل على الورق قبل البناء
    /// 2. يتخذ جميع القرارات الكبرى مسبقاً
    /// 3. ينتج ملف واحد فقط: MasterPlan.json
    /// 
    /// الخطوات:
    /// 2.1 → حدود العالم
    /// 2.2 → تحليل التضاريس
    /// 2.3 → تحليل المناخ
    /// 2.4 → اختيار العاصمة
    /// 2.5 → توزيع المدن
    /// 2.6 → شبكة النقل
    /// 2.7 → المطارات
    /// 2.8 → الموانئ
    /// 2.9 → الهوية الاقتصادية
    /// 2.10 → حساب السكان
    /// 2.11 → توزيع الأراضي
    /// 2.12 → التحقق من التناقضات
    /// </summary>
    public class MasterPlanBuilder
    {
        private readonly int seed;
        private readonly string worldName;
        private readonly float worldSizeKm;

        // Generators
        private WorldBoundsGenerator worldBoundsGen;
        private TerrainAnalysisGenerator terrainGen;
        private ClimateAnalysisGenerator climateGen;
        private CapitalLocationGenerator capitalGen;
        private CityDistributionGenerator cityDistributionGen;
        private TransportNetworkGenerator transportGen;
        private InfrastructureGenerator infrastructureGen;
        private EconomyAndPopulationGenerator economyGen;
        private LandAllocationGenerator landGen;
        private MasterPlanValidator validator;

        // Output
        private MasterPlanData masterPlan;

        public MasterPlanBuilder(int seed, string worldName, float worldSizeKm)
        {
            this.seed = seed;
            this.worldName = worldName;
            this.worldSizeKm = worldSizeKm;

            InitializeGenerators();
        }

        /// <summary>
        /// تهيئة جميع المحركات
        /// </summary>
        private void InitializeGenerators()
        {
            worldBoundsGen = new WorldBoundsGenerator(seed);
            terrainGen = new TerrainAnalysisGenerator(seed);
            climateGen = new ClimateAnalysisGenerator(seed);
            capitalGen = new CapitalLocationGenerator(seed);
            cityDistributionGen = new CityDistributionGenerator(seed);
            transportGen = new TransportNetworkGenerator(seed);
            infrastructureGen = new InfrastructureGenerator(seed);
            economyGen = new EconomyAndPopulationGenerator(seed);
            landGen = new LandAllocationGenerator(seed);
            validator = new MasterPlanValidator();
        }

        /// <summary>
        /// بناء خطة التخطيط الرئيسية الكاملة
        /// BUILD THE MASTER PLAN - This is the main method!
        /// </summary>
        public MasterPlanData BuildMasterPlan()
        {
            Debug.Log("╔════════════════════════════════════════════════════════════╗");
            Debug.Log("║     STARTING MASTER PLAN GENERATION (Stage 2)             ║");
            Debug.Log("║     Virtual City Planner - Planning Phase                 ║");
            Debug.Log("╚════════════════════════════════════════════════════════════╝");

            masterPlan = new MasterPlanData
            {
                seed = seed,
                worldName = worldName
            };

            try
            {
                // ============================================
                // المرحلة 2.1: حدود العالم
                // ============================================
                Debug.Log("\n[STEP 2.1] Generating World Bounds...");
                masterPlan.worldBounds = worldBoundsGen.GenerateWorldBounds(worldSizeKm);
                masterPlan.worldAreaSquareKm = masterPlan.worldBounds.areaSquareKm;
                masterPlan.planningLayers.Add(new PlanningLayerData("World Layer"));
                masterPlan.SetLayerComplete("World Layer");

                // ============================================
                // المرحلة 2.2: تحليل التضاريس
                // ============================================
                Debug.Log("\n[STEP 2.2] Analyzing Terrain...");
                masterPlan.terrainAnalysis = terrainGen.GenerateTerrainAnalysis(masterPlan.worldBounds);
                masterPlan.planningLayers.Add(new PlanningLayerData("Terrain Layer"));
                masterPlan.SetLayerComplete("Terrain Layer");

                // ============================================
                // المرحلة 2.3: تحليل المناخ
                // ============================================
                Debug.Log("\n[STEP 2.3] Analyzing Climate...");
                masterPlan.climateAnalysis = climateGen.GenerateClimateAnalysis(
                    masterPlan.worldBounds,
                    masterPlan.terrainAnalysis
                );
                masterPlan.planningLayers.Add(new PlanningLayerData("Climate Layer"));
                masterPlan.SetLayerComplete("Climate Layer");

                // ============================================
                // المرحلة 2.4: اختيار العاصمة
                // ============================================
                Debug.Log("\n[STEP 2.4] Selecting Capital Location...");
                masterPlan.capital = capitalGen.SelectCapital(
                    masterPlan.worldBounds,
                    masterPlan.terrainAnalysis,
                    masterPlan.climateAnalysis
                );
                masterPlan.planningLayers.Add(new PlanningLayerData("City Layer"));

                // ============================================
                // المرحلة 2.5: توزيع المدن
                // ============================================
                Debug.Log("\n[STEP 2.5] Distributing Cities...");
                var nameGen = new SeededNameGenerator(seed);
                masterPlan.cities = cityDistributionGen.DistributeCities(
                    masterPlan.capital,
                    masterPlan.worldBounds,
                    masterPlan.terrainAnalysis,
                    masterPlan.climateAnalysis,
                    nameGen
                );
                masterPlan.totalCities = masterPlan.cities.Count + 1;  // +1 for capital
                masterPlan.SetLayerComplete("City Layer");

                // ============================================
                // المرحلة 2.6: شبكة النقل
                // ============================================
                Debug.Log("\n[STEP 2.6] Building Transport Network...");
                masterPlan.transportNetwork = transportGen.GenerateTransportNetwork(
                    masterPlan.capital,
                    masterPlan.cities
                );
                masterPlan.planningLayers.Add(new PlanningLayerData("Transport Layer"));
                masterPlan.SetLayerComplete("Transport Layer");

                // ============================================
                // المرحلة 2.7 و 2.8: المطارات والموانئ
                // ============================================
                Debug.Log("\n[STEP 2.7-2.8] Planning Airports and Ports...");
                masterPlan.infrastructure = infrastructureGen.GenerateInfrastructure(
                    masterPlan.capital,
                    masterPlan.cities,
                    masterPlan.worldBounds,
                    masterPlan.terrainAnalysis
                );
                masterPlan.planningLayers.Add(new PlanningLayerData("Infrastructure Layer"));
                masterPlan.SetLayerComplete("Infrastructure Layer");

                // ============================================
                // المرحلة 2.9 و 2.10: الاقتصاد والسكان
                // ============================================
                Debug.Log("\n[STEP 2.9-2.10] Planning Economy and Population...");
                masterPlan.economy = new GlobalEconomyPlanData();
                masterPlan.populationDistribution = new PopulationDistributionData();
                economyGen.GenerateEconomyAndPopulation(
                    masterPlan.cities,
                    masterPlan.economy,
                    masterPlan.populationDistribution
                );

                // ============================================
                // المرحلة 2.11: توزيع الأراضي
                // ============================================
                Debug.Log("\n[STEP 2.11] Allocating Land Use...");
                masterPlan.globalLandAllocation = landGen.GenerateLandAllocation(
                    masterPlan.worldAreaSquareKm,
                    masterPlan.terrainAnalysis,
                    masterPlan.climateAnalysis,
                    masterPlan.economy
                );
                masterPlan.planningLayers.Add(new PlanningLayerData("District Layer"));
                masterPlan.SetLayerComplete("District Layer");

                // ============================================
                // المرحلة 2.12: التحقق من التناقضات
                // ============================================
                Debug.Log("\n[STEP 2.12] Validating Master Plan...");
                masterPlan.validation = validator.ValidateMasterPlan(masterPlan);

                if (!masterPlan.validation.isValid)
                {
                    Debug.LogError("[MasterPlanBuilder] Validation failed! Plan contains errors.");
                    throw new System.InvalidOperationException("Master plan validation failed");
                }

                Debug.Log("\n╔════════════════════════════════════════════════════════════╗");
                Debug.Log("║            ✅ MASTER PLAN GENERATION COMPLETE             ║");
                Debug.Log("╚════════════════════════════════════════════════════════════╝");

                PrintMasterPlanSummary();

                return masterPlan;
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[MasterPlanBuilder] Error building master plan: {ex.Message}\n{ex.StackTrace}");
                return null;
            }
        }

        /// <summary>
        /// طباعة ملخص خطة التخطيط
        /// </summary>
        private void PrintMasterPlanSummary()
        {
            Debug.Log(masterPlan.GetSummary());
        }

        /// <summary>
        /// الحصول على الخطة المبنية
        /// </summary>
        public MasterPlanData GetMasterPlan()
        {
            return masterPlan;
        }

        /// <summary>
        /// هل تم بناء الخطة بنجاح؟
        /// </summary>
        public bool IsValid()
        {
            return masterPlan != null && masterPlan.validation != null && masterPlan.validation.isValid;
        }

        /// <summary>
        /// الحصول على عدد خطوات البناء الكلي
        /// </summary>
        public static int GetTotalSteps()
        {
            return 12;  // 2.1 إلى 2.12
        }

        /// <summary>
        /// الحصول على تقدم البناء
        /// </summary>
        public float GetProgress()
        {
            if (masterPlan == null) return 0;
            return (float)masterPlan.planningLayers.Count / 7f;  // 7 planning layers total
        }
    }
}
