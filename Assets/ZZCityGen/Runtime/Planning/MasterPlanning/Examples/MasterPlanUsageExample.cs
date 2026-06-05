using UnityEngine;
using ZZCityGen.Planning.MasterPlanning;
using ZZCityGen.Planning.MasterPlanning.Generators;

namespace ZZCityGen.Planning.MasterPlanning.Examples
{
    /// <summary>
    /// مثال على الاستخدام الأساسي لمحرك التخطيط العالمي
    /// Example usage of Master Plan Engine
    /// 
    /// يوضح هذا المثال كيفية:
    /// 1. إنشاء مثيل من المحرك
    /// 2. بناء خطة التخطيط
    /// 3. التحقق من الصحة
    /// 4. الوصول إلى النتائج
    /// </summary>
    public class MasterPlanUsageExample
    {
        /// <summary>
        /// مثال 1: الاستخدام الأساسي
        /// Generate a complete master plan from scratch
        /// </summary>
        public static void Example1_BasicUsage()
        {
            Debug.Log("=== EXAMPLE 1: Basic Master Plan Generation ===");

            // ============================================
            // الخطوة 1: إنشاء المحرك
            // ============================================
            var builder = new MasterPlanBuilder(
                seed: 12345,
                worldName: "Terravian",
                worldSizeKm: 200f
            );

            // ============================================
            // الخطوة 2: بناء الخطة
            // ============================================
            MasterPlanData masterPlan = builder.BuildMasterPlan();

            // ============================================
            // الخطوة 3: التحقق من الصحة
            // ============================================
            if (builder.IsValid())
            {
                Debug.Log("✅ Master plan generated successfully!");

                // ============================================
                // الخطوة 4: الوصول إلى البيانات
                // ============================================
                PrintPlanSummary(masterPlan);
            }
            else
            {
                Debug.LogError("❌ Master plan generation failed!");
            }
        }

        /// <summary>
        /// مثال 2: الوصول إلى تفاصيل محددة
        /// Access specific data from the master plan
        /// </summary>
        public static void Example2_AccessingData(MasterPlanData masterPlan)
        {
            Debug.Log("=== EXAMPLE 2: Accessing Specific Data ===");

            // ============================================
            // بيانات الحدود
            // ============================================
            Debug.Log($"World Size: {masterPlan.worldAreaSquareKm:F2} km²");
            Debug.Log($"World Bounds: X[{masterPlan.worldBounds.minX}-{masterPlan.worldBounds.maxX}]");

            // ============================================
            // بيانات التضاريس
            // ============================================
            Debug.Log($"\nTerrain Analysis:");
            Debug.Log($"  Buildable Area: {masterPlan.terrainAnalysis.buildableAreaPercentage}%");
            Debug.Log($"  Max Elevation: {masterPlan.terrainAnalysis.maxElevation}m");
            Debug.Log($"  Terrain Zones: {masterPlan.terrainAnalysis.zones.Count}");

            // ============================================
            // بيانات المناخ
            // ============================================
            Debug.Log($"\nClimate Analysis:");
            Debug.Log($"  Dominant Climate: {masterPlan.climateAnalysis.dominantClimate}");
            Debug.Log($"  Agriculture Potential: {masterPlan.climateAnalysis.agriculturePotentialPercentage}%");
            Debug.Log($"  Global Avg Temperature: {masterPlan.climateAnalysis.globalAverageTemperature}°C");

            // ============================================
            // بيانات العاصمة
            // ============================================
            Debug.Log($"\nCapital:");
            Debug.Log($"  Name: Central City");
            Debug.Log($"  Position: {masterPlan.capital.position}");
            Debug.Log($"  Population: {masterPlan.capital.targetPopulation:N0}");
            Debug.Log($"  Accessibility Score: {masterPlan.capital.accessibilityScore}");

            // ============================================
            // بيانات المدن
            // ============================================
            Debug.Log($"\nCities ({masterPlan.cities.Count}):");
            foreach (var city in masterPlan.cities)
            {
                Debug.Log($"  {city.name}:");
                Debug.Log($"    Type: {city.archetype}");
                Debug.Log($"    Population: {city.targetPopulation:N0}");
                Debug.Log($"    Economy: {city.economicIdentity}");
                Debug.Log($"    Distance from Capital: {city.distanceFromCapitalKm:F2} km");
            }

            // ============================================
            // بيانات النقل
            // ============================================
            Debug.Log($"\nTransport Network:");
            Debug.Log($"  Total Nodes: {masterPlan.transportNetwork.nodes.Count}");
            Debug.Log($"  Total Edges: {masterPlan.transportNetwork.edges.Count}");
            Debug.Log($"  Total Roads: {masterPlan.transportNetwork.totalPlannedRoadsKm:F2} km");
            Debug.Log($"  Highways: {masterPlan.transportNetwork.highwayCount}");

            // ============================================
            // بيانات البنية التحتية
            // ============================================
            Debug.Log($"\nInfrastructure:");
            Debug.Log($"  Airports: {masterPlan.infrastructure.airports.Count}");
            Debug.Log($"  Ports: {masterPlan.infrastructure.ports.Count}");
            Debug.Log($"  Power Plants: {masterPlan.infrastructure.powerPlantCount}");

            // ============================================
            // بيانات السكان
            // ============================================
            Debug.Log($"\nPopulation Distribution:");
            Debug.Log($"  Total: {masterPlan.populationDistribution.totalPopulation:N0}");
            Debug.Log($"  Capital: {masterPlan.populationDistribution.capitalPopulation:N0}");
            Debug.Log($"  Density: {masterPlan.GetGlobalPopulationDensity():F2} people/km²");

            // ============================================
            // بيانات الأراضي
            // ============================================
            Debug.Log($"\nLand Allocation:");
            foreach (var allocation in masterPlan.globalLandAllocation)
            {
                Debug.Log($"  {allocation.type}: {allocation.percentageOfWorld * 100:F1}%");
            }

            // ============================================
            // بيانات التحقق
            // ============================================
            Debug.Log($"\nValidation:");
            Debug.Log($"  Valid: {masterPlan.validation.isValid}");
            Debug.Log($"  Errors: {masterPlan.validation.errors.Count}");
            Debug.Log($"  Warnings: {masterPlan.validation.warnings.Count}");
        }

        /// <summary>
        /// مثال 3: تحليل المدن الفردية
        /// Analyze individual cities
        /// </summary>
        public static void Example3_CityAnalysis(MasterPlanData masterPlan)
        {
            Debug.Log("=== EXAMPLE 3: City Analysis ===");

            if (masterPlan.cities.Count == 0)
                return;

            var city = masterPlan.cities[0];

            Debug.Log($"City: {city.name}");
            Debug.Log($"  Archetype: {city.archetype}");
            Debug.Log($"  Position: {city.position}");
            Debug.Log($"  Radius: {city.radiusMeters}m");
            Debug.Log($"  Target Population: {city.targetPopulation:N0}");
            Debug.Log($"  Economic Identity: {city.economicIdentity}");
            Debug.Log($"  Is Capital: {city.isCapital}");
            Debug.Log($"  Is Coastal: {city.isCoastal}");
            Debug.Log($"  Distance from Capital: {city.distanceFromCapitalKm:F2}km");
            Debug.Log($"  Expected Growth Rate: {city.CalculateGrowthRate() * 100}% annually");
        }

        /// <summary>
        /// مثال 4: تحليل شبكة النقل
        /// Analyze transport network
        /// </summary>
        public static void Example4_TransportAnalysis(MasterPlanData masterPlan)
        {
            Debug.Log("=== EXAMPLE 4: Transport Network Analysis ===");

            var network = masterPlan.transportNetwork;

            Debug.Log($"Nodes: {network.nodes.Count}");
            Debug.Log($"Edges: {network.edges.Count}");
            Debug.Log($"Total Road Length: {network.totalPlannedRoadsKm:F2}km");

            // جد الطرق السريعة
            var highways = GetHighways(network);
            Debug.Log($"\nHighways ({highways.Count}):");
            foreach (var highway in highways)
            {
                Debug.Log($"  {highway.fromNodeIndex} → {highway.toNodeIndex}: {highway.lengthKm:F2}km");
            }
        }

        /// <summary>
        /// مثال 5: تحليل المطارات والموانئ
        /// Analyze airports and ports
        /// </summary>
        public static void Example5_InfrastructureAnalysis(MasterPlanData masterPlan)
        {
            Debug.Log("=== EXAMPLE 5: Infrastructure Analysis ===");

            var infrastructure = masterPlan.infrastructure;

            // ============================================
            // المطارات
            // ============================================
            Debug.Log($"Airports ({infrastructure.airports.Count}):");
            foreach (var airport in infrastructure.airports)
            {
                Debug.Log($"  {airport.name}:");
                Debug.Log($"    Type: {airport.type}");
                Debug.Log($"    Capacity: {airport.plannedCapacity:N0} passengers/year");
                Debug.Log($"    Position: {airport.position}");
            }

            // ============================================
            // الموانئ
            // ============================================
            Debug.Log($"\nPorts ({infrastructure.ports.Count}):");
            foreach (var port in infrastructure.ports)
            {
                Debug.Log($"  {port.name}:");
                Debug.Log($"    Type: {port.type}");
                Debug.Log($"    Capacity: {port.containerCapacity:N0} TEU/year");
                Debug.Log($"    Position: {port.position}");
            }
        }

        /// <summary>
        /// مثال 6: معالجة الأخطاء
        /// Error handling
        /// </summary>
        public static void Example6_ErrorHandling()
        {
            Debug.Log("=== EXAMPLE 6: Error Handling ===");

            var builder = new MasterPlanBuilder(
                seed: 999,
                worldName: "Test World",
                worldSizeKm: 50f  // عالم صغير جداً
            );

            MasterPlanData masterPlan = builder.BuildMasterPlan();

            if (masterPlan == null)
            {
                Debug.LogError("❌ Failed to build master plan");
                return;
            }

            if (!builder.IsValid())
            {
                Debug.LogWarning("⚠️ Plan is valid but has issues:");

                if (masterPlan.validation.errors.Count > 0)
                {
                    Debug.LogError("Errors:");
                    foreach (var error in masterPlan.validation.errors)
                    {
                        Debug.LogError($"  - {error}");
                    }
                }

                if (masterPlan.validation.warnings.Count > 0)
                {
                    Debug.LogWarning("Warnings:");
                    foreach (var warning in masterPlan.validation.warnings)
                    {
                        Debug.LogWarning($"  - {warning}");
                    }
                }
            }
        }

        /// <summary>
        /// مثال 7: حفظ وتحميل الخطة
        /// Save and load master plan
        /// </summary>
        public static void Example7_SaveAndLoad(MasterPlanData masterPlan)
        {
            Debug.Log("=== EXAMPLE 7: Save and Load ===");

            // حفظ
            string json = JsonUtility.ToJson(masterPlan, true);
            string filePath = "Assets/MasterPlan.json";
            System.IO.File.WriteAllText(filePath, json);
            Debug.Log($"✅ Saved master plan to {filePath}");

            // تحميل
            string loadedJson = System.IO.File.ReadAllText(filePath);
            MasterPlanData loadedPlan = JsonUtility.FromJson<MasterPlanData>(loadedJson);
            Debug.Log($"✅ Loaded master plan: {loadedPlan.worldName}");
        }

        /// <summary>
        /// طباعة ملخص الخطة
        /// </summary>
        private static void PrintPlanSummary(MasterPlanData masterPlan)
        {
            Debug.Log(masterPlan.GetSummary());
        }

        /// <summary>
        /// الحصول على جميع الطرق السريعة
        /// </summary>
        private static System.Collections.Generic.List<TransportEdgeData> GetHighways(TransportNetworkData network)
        {
            var highways = new System.Collections.Generic.List<TransportEdgeData>();
            foreach (var edge in network.edges)
            {
                if (edge.roadType == "Highway")
                    highways.Add(edge);
            }
            return highways;
        }
    }
}
