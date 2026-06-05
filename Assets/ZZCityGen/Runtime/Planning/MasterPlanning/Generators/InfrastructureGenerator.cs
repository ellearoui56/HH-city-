using System;
using System.Collections.Generic;
using UnityEngine;

namespace ZZCityGen.Planning.MasterPlanning.Generators
{
    /// <summary>
    /// المرحلة 2.7 و 2.8: توليد المطارات والموانئ
    /// Generates airports and ports
    /// </summary>
    public class InfrastructureGenerator
    {
        private readonly System.Random random;

        public InfrastructureGenerator(int seed)
        {
            this.random = new System.Random(seed);
        }

        /// <summary>
        /// توليد كامل البنية التحتية
        /// </summary>
        public InfrastructureMasterPlanData GenerateInfrastructure(
            CapitalLocationData capital,
            List<CityMasterPlanData> cities,
            WorldBoundsData bounds,
            TerrainAnalysisData terrain)
        {
            var infrastructure = new InfrastructureMasterPlanData();

            // ============================================
            // 1. توليد المطارات
            // ============================================
            GenerateAirports(capital, cities, infrastructure);

            // ============================================
            // 2. توليد الموانئ
            // ============================================
            GeneratePorts(cities, infrastructure, terrain);

            // ============================================
            // 3. توليد محطات الكهرباء
            // ============================================
            infrastructure.powerPlantCount = Mathf.CeilToInt(cities.Count / 3);

            // ============================================
            // 4. توليد محطات المياه
            // ============================================
            infrastructure.waterTreatmentPlantCount = Mathf.CeilToInt(cities.Count / 2);

            // ============================================
            // 5. توليد محطات الصرف الصحي
            // ============================================
            infrastructure.sewagePlantCount = Mathf.CeilToInt(cities.Count / 2);

            Debug.Log("[InfrastructureGenerator] Infrastructure generated:");
            Debug.Log($"  Airports: {infrastructure.airports.Count}");
            Debug.Log($"  Ports: {infrastructure.ports.Count}");
            Debug.Log($"  Power Plants: {infrastructure.powerPlantCount}");
            Debug.Log($"  Water Treatment: {infrastructure.waterTreatmentPlantCount}");
            Debug.Log($"  Sewage Plants: {infrastructure.sewagePlantCount}");

            return infrastructure;
        }

        /// <summary>
        /// المرحلة 2.7: توليد المطارات
        /// 
        /// مثال:
        /// - International Airport (العاصمة)
        /// - Regional Airports (المدن الكبرى)
        /// - Local Airports (المدن الصغرى)
        /// </summary>
        private void GenerateAirports(CapitalLocationData capital, 
                                     List<CityMasterPlanData> cities,
                                     InfrastructureMasterPlanData infrastructure)
        {
            // ============================================
            // 1. مطار دولي في العاصمة
            // ============================================
            var capitalAirport = new AirportPlanData(
                $"International Airport",
                capital.position,
                AirportType.International,
                cityIndex: 0
            );
            infrastructure.AddAirport(capitalAirport);

            // ============================================
            // 2. مطارات إقليمية في المدن الكبيرة
            // ============================================
            for (int i = 1; i < cities.Count; i++)
            {
                var city = cities[i];

                // المدن الكبرى والصناعية تحصل على مطارات إقليمية
                if (city.archetype == CityArchetype.FamilyCity ||
                    city.archetype == CityArchetype.IndustrialCity)
                {
                    var airport = new AirportPlanData(
                        $"{city.name} Regional Airport",
                        GetAirportLocation(city.position),
                        AirportType.Regional,
                        cityIndex: i
                    );
                    infrastructure.AddAirport(airport);
                }
                // المدن الساحلية والسياحية تحصل على مطارات محلية
                else if (city.archetype == CityArchetype.CoastalCity ||
                         city.archetype == CityArchetype.TourismDestination)
                {
                    var airport = new AirportPlanData(
                        $"{city.name} Local Airport",
                        GetAirportLocation(city.position),
                        AirportType.Local,
                        cityIndex: i
                    );
                    infrastructure.AddAirport(airport);
                }
            }

            Debug.Log($"[InfrastructureGenerator] Generated {infrastructure.airports.Count} airports");
        }

        /// <summary>
        /// المرحلة 2.8: توليد الموانئ
        /// إذا كانت المدينة ساحلية، ننشئ موانئ
        /// </summary>
        private void GeneratePorts(List<CityMasterPlanData> cities,
                                  InfrastructureMasterPlanData infrastructure,
                                  TerrainAnalysisData terrain)
        {
            // اعثر على المناطق المائية
            TerrainZoneData waterZone = null;
            foreach (var zone in terrain.zones)
            {
                if (zone.type == TerrainZoneType.Water)
                {
                    waterZone = zone;
                    break;
                }
            }

            if (waterZone == null)
            {
                Debug.LogWarning("[InfrastructureGenerator] No water zone found, skipping ports");
                return;
            }

            // ============================================
            // تحديد المدن الساحلية
            // ============================================
            for (int i = 0; i < cities.Count; i++)
            {
                var city = cities[i];

                // تحقق من أن المدينة قريبة من الماء
                float distanceToWater = Vector2.Distance(city.position, waterZone.center);
                if (distanceToWater < waterZone.radiusMeters + 20_000)  // 20km من حافة الماء
                {
                    city.isCoastal = true;

                    // ============================================
                    // مدينة ساحلية = ميناء تجاري
                    // ============================================
                    if (city.archetype == CityArchetype.CoastalCity ||
                        city.economicIdentity == EconomicIdentity.Tourism)
                    {
                        var port = new PortPlanData(
                            $"{city.name} Commercial Port",
                            GetPortLocation(city.position, waterZone.center),
                            PortType.Commercial,
                            cityIndex: i
                        );
                        infrastructure.AddPort(port);

                        // ============================================
                        // المدن الساحلية قد تحصل على موانئ سياحية إضافية
                        // ============================================
                        if (city.economicIdentity == EconomicIdentity.Tourism)
                        {
                            var tourismPort = new PortPlanData(
                                $"{city.name} Tourism Port",
                                GetPortLocation(city.position, waterZone.center, offset: 2000),
                                PortType.Tourism,
                                cityIndex: i
                            );
                            infrastructure.AddPort(tourismPort);
                        }
                    }

                    // ============================================
                    // المدن الريفية الساحلية قد تحصل على موانئ صيد
                    // ============================================
                    if (city.archetype == CityArchetype.RuralTown ||
                        city.economicIdentity == EconomicIdentity.Agriculture)
                    {
                        var fishingPort = new PortPlanData(
                            $"{city.name} Fishing Port",
                            GetPortLocation(city.position, waterZone.center, offset: -2000),
                            PortType.Fishing,
                            cityIndex: i
                        );
                        infrastructure.AddPort(fishingPort);
                    }
                }
            }

            Debug.Log($"[InfrastructureGenerator] Generated {infrastructure.ports.Count} ports");
        }

        /// <summary>
        /// حساب موقع المطار (خارج المدينة)
        /// </summary>
        private Vector2 GetAirportLocation(Vector2 cityPosition, float offset = 5000)
        {
            // ضع المطار على بعد 5 كم من المدينة
            Vector2 direction = Random.insideUnitCircle.normalized;
            return cityPosition + direction * offset;
        }

        /// <summary>
        /// حساب موقع الميناء (على الساحل)
        /// </summary>
        private Vector2 GetPortLocation(Vector2 cityPosition, Vector2 waterCenter, float offset = 0)
        {
            // حرك الميناء باتجاه الماء
            Vector2 direction = (waterCenter - cityPosition).normalized;
            return cityPosition + direction * (4000 + offset);
        }

        /// <summary>
        /// الحصول على إجمالي السعة المطاراتية المخطط لها
        /// </summary>
        public long GetTotalAirportCapacity(InfrastructureMasterPlanData infrastructure)
        {
            long total = 0;
            foreach (var airport in infrastructure.airports)
            {
                total += airport.plannedCapacity;
            }
            return total;
        }

        /// <summary>
        /// الحصول على إجمالي سعة الموانئ المخطط لها
        /// </summary>
        public long GetTotalPortCapacity(InfrastructureMasterPlanData infrastructure)
        {
            long total = 0;
            foreach (var port in infrastructure.ports)
            {
                total += port.containerCapacity;
            }
            return total;
        }

        /// <summary>
        /// الحصول على المطارات حسب النوع
        /// </summary>
        public List<AirportPlanData> GetAirportsByType(InfrastructureMasterPlanData infrastructure, AirportType type)
        {
            var filtered = new List<AirportPlanData>();
            foreach (var airport in infrastructure.airports)
            {
                if (airport.type == type)
                    filtered.Add(airport);
            }
            return filtered;
        }

        /// <summary>
        /// الحصول على الموانئ حسب النوع
        /// </summary>
        public List<PortPlanData> GetPortsByType(InfrastructureMasterPlanData infrastructure, PortType type)
        {
            var filtered = new List<PortPlanData>();
            foreach (var port in infrastructure.ports)
            {
                if (port.type == type)
                    filtered.Add(port);
            }
            return filtered;
        }
    }
}
