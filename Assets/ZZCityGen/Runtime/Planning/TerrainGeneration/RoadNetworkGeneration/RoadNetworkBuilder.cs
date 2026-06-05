using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using ZZCityGen.Data;
using ZZCityGen.Planning.MasterPlanning;
using CityArchetype = ZZCityGen.Planning.MasterPlanning.CityArchetype;

namespace ZZCityGen.Planning.TerrainGeneration.RoadNetworkGeneration
{
    public class RoadNetworkBuilder
    {
        private readonly MasterPlanData masterPlan;
        private readonly TerrainGenerationData terrainData;
        private readonly TerrainAnalysisData terrainAnalysis;
        private readonly Vector2 worldMin;
        private readonly Vector2 worldMax;
        private readonly float worldWidth;
        private readonly float worldHeight;
        private readonly float seaLevel;
        private readonly float futureYears = 10f;

        public RoadNetworkBuilder(MasterPlanData masterPlan, TerrainGenerationData terrainData, TerrainAnalysisData terrainAnalysis = null)
        {
            this.masterPlan = masterPlan ?? throw new ArgumentNullException(nameof(masterPlan));
            this.terrainData = terrainData ?? throw new ArgumentNullException(nameof(terrainData));
            this.terrainAnalysis = terrainAnalysis;
            this.worldMin = new Vector2(masterPlan.worldBounds.minX, masterPlan.worldBounds.minZ);
            this.worldMax = new Vector2(masterPlan.worldBounds.maxX, masterPlan.worldBounds.maxZ);
            this.worldWidth = Mathf.Max(1f, this.worldMax.x - this.worldMin.x);
            this.worldHeight = Mathf.Max(1f, this.worldMax.y - this.worldMin.y);
            this.seaLevel = Mathf.Min(terrainData.heightMap.minElevation + 1.5f, terrainData.heightMap.minElevation + 5f);
        }

        public RoadNetworkPlan BuildRoadNetwork(out TransportationAnalysisData transportationAnalysis, TerrainAnalysisData terrainAnalysisOverride = null)
        {
            transportationAnalysis = new TransportationAnalysisData();
            var corridorSource = terrainAnalysisOverride ?? terrainAnalysis;
            var roadNetwork = new RoadNetworkPlan();

            if (masterPlan.cities == null || masterPlan.cities.Count == 0)
            {
                Debug.LogWarning("[RoadNetworkBuilder] No cities available in the master plan.");
                return roadNetwork;
            }

            var cityNodes = CreateCityNodes();
            var demandMatrix = EvaluateTrafficDemand(cityNodes);

            BuildPrimaryHighways(roadNetwork, transportationAnalysis, cityNodes, demandMatrix, corridorSource);
            BuildRegionalRoads(roadNetwork, transportationAnalysis, cityNodes, demandMatrix, corridorSource);
            BuildAirportAccess(roadNetwork, transportationAnalysis, cityNodes);
            BuildPortAccess(roadNetwork, transportationAnalysis, cityNodes);
            BuildRailNetwork(roadNetwork, transportationAnalysis, cityNodes, corridorSource);
            BuildFreightCorridors(roadNetwork, transportationAnalysis, cityNodes, demandMatrix, corridorSource);
            BuildBackupRailNetwork(roadNetwork, transportationAnalysis, cityNodes, demandMatrix, corridorSource);
            BuildIntersections(roadNetwork);
            AnalyzeNetworkPerformance(roadNetwork, transportationAnalysis, demandMatrix);
            PopulateLogisticsAnalysis(transportationAnalysis, corridorSource, cityNodes);
            UpdateMasterPlanTransportNetwork(roadNetwork, cityNodes);

            Debug.Log($"[RoadNetworkBuilder] Generated {roadNetwork.Highways.Count} highways, {roadNetwork.Railways.Count} railways, {roadNetwork.Bridges.Count} bridges, {roadNetwork.Tunnels.Count} tunnels.");
            return roadNetwork;
        }

        public RoadNetworkPlan BuildRoadNetwork()
        {
            TransportationAnalysisData unused;
            return BuildRoadNetwork(out unused);
        }

        private List<CityNode> CreateCityNodes()
        {
            var cityNodes = new List<CityNode>();

            for (int index = 0; index < masterPlan.cities.Count; index++)
            {
                var city = masterPlan.cities[index];
                cityNodes.Add(new CityNode
                {
                    index = index,
                    city = city,
                    position = city.position,
                    isCapital = city.isCapital
                });
            }

            return cityNodes;
        }

        private List<TransportDemandData> EvaluateTrafficDemand(List<CityNode> cityNodes)
        {
            var demands = new List<TransportDemandData>();

            for (int a = 0; a < cityNodes.Count; a++)
            {
                for (int b = a + 1; b < cityNodes.Count; b++)
                {
                    var demandScore = EstimateDemandScore(cityNodes[a], cityNodes[b]);
                    var dailyTraffic = EstimateDailyTraffic(cityNodes[a], cityNodes[b], demandScore);
                    var growthFactor = EstimateGrowthFactor(cityNodes[a], cityNodes[b], demandScore);
                    var futureTraffic = dailyTraffic * growthFactor;
                    var category = GetDemandCategory(demandScore);

                    demands.Add(new TransportDemandData
                    {
                        fromIndex = a,
                        toIndex = b,
                        demandScore = demandScore,
                        dailyTraffic = dailyTraffic,
                        futureTraffic = futureTraffic,
                        category = category
                    });
                }
            }

            return demands.OrderByDescending(d => d.demandScore).ToList();
        }

        private void BuildPrimaryHighways(
            RoadNetworkPlan network,
            TransportationAnalysisData analysis,
            List<CityNode> cityNodes,
            List<TransportDemandData> demandMatrix,
            TerrainAnalysisData corridorSource)
        {
            var capitalIndex = cityNodes.FindIndex(n => n.isCapital);
            if (capitalIndex < 0)
            {
                capitalIndex = 0;
            }

            foreach (var demand in demandMatrix)
            {
                if (demand.demandScore < 0.18f)
                    continue;

                int fromIndex = demand.fromIndex;
                int toIndex = demand.toIndex;
                var fromCity = cityNodes[fromIndex];
                var toCity = cityNodes[toIndex];
                var connectionName = $"{fromCity.city.name} ↔ {toCity.city.name}";
                var path = GetRoutePath(fromCity.position, toCity.position, fromIndex, toIndex, corridorSource);
                var routeInfo = AnalyzeRoute(path);
                float routeLength = CalculateRouteLength(path);
                int laneCount = AssignLaneCount(demand.futureTraffic);
                var roadClass = laneCount >= 6 ? "Express Highway" : "Highway";

                var highway = new HighwayPlan
                {
                    name = connectionName,
                    from = fromCity.position,
                    to = toCity.position,
                    lengthMeters = routeLength,
                    requiresBridge = routeInfo.requiresBridge,
                    requiresTunnel = routeInfo.requiresTunnel,
                    laneCount = laneCount,
                    roadClass = roadClass,
                    projectedDailyTraffic = demand.futureTraffic
                };

                network.Highways.Add(highway);

                if (routeInfo.requiresBridge)
                {
                    network.Bridges.Add(CreateBridgePlan(highway));
                }

                if (routeInfo.requiresTunnel)
                {
                    network.Tunnels.Add(CreateTunnelPlan(highway, routeInfo.maximumSlope));
                }

                AddHighwayCorridorAnalysis(analysis, fromIndex, toIndex, path, routeInfo);
                analysis.demandSummaries.Add(new TransportDemandSummary(connectionName, demand.dailyTraffic, demand.futureTraffic, demand.category));
            }

            if (network.Highways.Count == 0 && cityNodes.Count >= 2)
            {
                var first = cityNodes[0];
                var second = cityNodes[1];
                var path = GetRoutePath(first.position, second.position, first.index, second.index, corridorSource);
                var routeInfo = AnalyzeRoute(path);
                var routeLength = CalculateRouteLength(path);
                var highway = new HighwayPlan
                {
                    name = $"{first.city.name} ↔ {second.city.name}",
                    from = first.position,
                    to = second.position,
                    lengthMeters = routeLength,
                    requiresBridge = routeInfo.requiresBridge,
                    requiresTunnel = routeInfo.requiresTunnel,
                    laneCount = 4,
                    roadClass = "Highway",
                    projectedDailyTraffic = 16000f
                };

                network.Highways.Add(highway);
                if (routeInfo.requiresBridge) network.Bridges.Add(CreateBridgePlan(highway));
                if (routeInfo.requiresTunnel) network.Tunnels.Add(CreateTunnelPlan(highway, routeInfo.maximumSlope));
                AddHighwayCorridorAnalysis(analysis, first.index, second.index, path, routeInfo);
            }
        }

        private void BuildRegionalRoads(
            RoadNetworkPlan network,
            TransportationAnalysisData analysis,
            List<CityNode> cityNodes,
            List<TransportDemandData> demandMatrix,
            TerrainAnalysisData corridorSource)
        {
            var directConnections = new HashSet<string>();
            foreach (var highway in network.Highways)
            {
                directConnections.Add(RoadKey(highway.from, highway.to));
            }

            foreach (var demand in demandMatrix)
            {
                if (demand.demandScore < 0.08f || demand.demandScore >= 0.18f)
                    continue;

                var fromCity = cityNodes[demand.fromIndex];
                var toCity = cityNodes[demand.toIndex];
                var key = RoadKey(fromCity.position, toCity.position);

                if (directConnections.Contains(key))
                    continue;

                if (Vector2.Distance(fromCity.position, toCity.position) > 18000f)
                    continue;

                var path = GetRoutePath(fromCity.position, toCity.position, fromCity.index, toCity.index, corridorSource);
                var routeInfo = AnalyzeRoute(path);
                float routeLength = CalculateRouteLength(path);

                var street = new StreetSegmentPlan
                {
                    name = $"Regional {fromCity.city.name} - {toCity.city.name}",
                    from = fromCity.position,
                    to = toCity.position,
                    lengthMeters = routeLength,
                    widthMeters = 12f,
                    roadClass = "RegionalRoad"
                };

                network.MainStreets.Add(street);
                analysis.expansionCorridors.Add(new ExpansionCorridorData(street.name, path, 0.55f, "Regional"));
            }

            var coastalCities = cityNodes.Where(c => c.city.isCoastal).ToList();
            for (int i = 0; i < coastalCities.Count - 1; i++)
            {
                var fromCity = coastalCities[i];
                var toCity = coastalCities[i + 1];
                var path = GetRoutePath(fromCity.position, toCity.position, fromCity.index, toCity.index, corridorSource);
                var routeInfo = AnalyzeRoute(path);
                float routeLength = CalculateRouteLength(path);

                var coastalRoad = new StreetSegmentPlan
                {
                    name = $"Coastal Connector {fromCity.city.name} - {toCity.city.name}",
                    from = fromCity.position,
                    to = toCity.position,
                    lengthMeters = routeLength,
                    widthMeters = 10f,
                    roadClass = "CoastalRoad"
                };

                network.SecondaryStreets.Add(coastalRoad);
                analysis.expansionCorridors.Add(new ExpansionCorridorData(coastalRoad.name, path, 0.45f, "Coastal"));
            }
        }

        private void BuildAirportAccess(RoadNetworkPlan network, TransportationAnalysisData analysis, List<CityNode> cityNodes)
        {
            if (masterPlan.infrastructure?.airports == null)
                return;

            foreach (var airport in masterPlan.infrastructure.airports)
            {
                var airportCity = airport.cityIndex >= 0 && airport.cityIndex < cityNodes.Count ? cityNodes[airport.cityIndex] : cityNodes.First();
                var target = airport.position;
                var accessTarget = FindClosestHighwayPoint(network, airport.position, airportCity.position);
                var path = GetRoutePath(target, accessTarget, airport.cityIndex, airport.cityIndex, terrainAnalysis);
                float routeLength = CalculateRouteLength(path);

                network.SecondaryStreets.Add(new StreetSegmentPlan
                {
                    name = $"Airport Access {airport.name}",
                    from = target,
                    to = accessTarget,
                    lengthMeters = routeLength,
                    widthMeters = 14f,
                    roadClass = "AirportAccess"
                });

                analysis.bestLogisticsAreas.Add(new LogisticsAreaData(target, 3500f, 0.84f, airport.name, "Airport access corridor"));
            }
        }

        private void BuildPortAccess(RoadNetworkPlan network, TransportationAnalysisData analysis, List<CityNode> cityNodes)
        {
            if (masterPlan.infrastructure?.ports == null)
                return;

            foreach (var port in masterPlan.infrastructure.ports)
            {
                var portCity = port.cityIndex >= 0 && port.cityIndex < cityNodes.Count ? cityNodes[port.cityIndex] : cityNodes.First();
                var target = port.position;
                var accessTarget = FindClosestHighwayPoint(network, port.position, portCity.position);
                var path = GetRoutePath(target, accessTarget, port.cityIndex, port.cityIndex, terrainAnalysis);
                float routeLength = CalculateRouteLength(path);

                network.SecondaryStreets.Add(new StreetSegmentPlan
                {
                    name = $"Port Access {port.name}",
                    from = target,
                    to = accessTarget,
                    lengthMeters = routeLength,
                    widthMeters = 16f,
                    roadClass = "PortAccess"
                });

                analysis.bestLogisticsAreas.Add(new LogisticsAreaData(target, 4200f, 0.92f, port.name, "Port cargo access"));
            }
        }

        private void BuildRailNetwork(RoadNetworkPlan network, TransportationAnalysisData analysis, List<CityNode> cityNodes, TerrainAnalysisData corridorSource)
        {
            var capital = cityNodes.FirstOrDefault(n => n.isCapital) ?? cityNodes.First();
            foreach (var candidate in cityNodes)
            {
                if (candidate.index == capital.index)
                    continue;

                bool isIndustrialRoute = candidate.city.archetype == CityArchetype.IndustrialCity;
                bool isPortRoute = masterPlan.infrastructure?.ports?.Any(p => p.cityIndex == candidate.index) ?? false;
                bool isAirportRoute = masterPlan.infrastructure?.airports?.Any(a => a.cityIndex == candidate.index) ?? false;

                if (!isIndustrialRoute && !isPortRoute && !isAirportRoute && candidate.city.archetype != CityArchetype.CoastalCity)
                    continue;

                var path = GetRoutePath(capital.position, candidate.position, capital.index, candidate.index, corridorSource);
                var routeInfo = AnalyzeRoute(path);
                float routeLength = CalculateRouteLength(path);

                network.Railways.Add(new RailwayPlan
                {
                    name = $"Rail {capital.city.name} - {candidate.city.name}",
                    from = capital.position,
                    to = candidate.position,
                    lengthMeters = routeLength,
                    isHighPriority = isPortRoute || isAirportRoute || isIndustrialRoute,
                    estimatedTravelTimeMinutes = Mathf.Max(20f, routeLength / 80f * 60f),
                    projectedFreightCapacity = isPortRoute ? 24000f : 12000f
                });

                if (routeInfo.requiresTunnel)
                {
                    transportationAnalysis.futureCongestionZones.Add(new CongestionZoneData(
                        GetSegmentMidPoint(capital.position, candidate.position),
                        4000f,
                        0.65f,
                        "Rail tunnel candidate"
                    ));
                }
            }
        }

        private void BuildBackupRailNetwork(RoadNetworkPlan network, TransportationAnalysisData analysis, List<CityNode> cityNodes, List<TransportDemandData> demandMatrix, TerrainAnalysisData corridorSource)
        {
            var industrialCities = cityNodes.Where(c => c.city.archetype == CityArchetype.IndustrialCity || HasPort(c.index) || HasAirport(c.index)).ToList();
            if (industrialCities.Count < 2)
                return;

            for (int i = 0; i < industrialCities.Count; i++)
            {
                for (int j = i + 1; j < industrialCities.Count; j++)
                {
                    var fromCity = industrialCities[i];
                    var toCity = industrialCities[j];
                    if (demandMatrix.All(d => !(d.fromIndex == fromCity.index && d.toIndex == toCity.index) && !(d.fromIndex == toCity.index && d.toIndex == fromCity.index)))
                        continue;

                    var path = GetRoutePath(fromCity.position, toCity.position, fromCity.index, toCity.index, corridorSource);
                    var routeInfo = AnalyzeRoute(path);
                    float routeLength = CalculateRouteLength(path);
                    float redundancyScore = 0.4f + (fromCity.city.archetype == CityArchetype.IndustrialCity || toCity.city.archetype == CityArchetype.IndustrialCity ? 0.3f : 0f);

                    var backupRail = new RailwayPlan
                    {
                        name = $"Backup Rail {fromCity.city.name} - {toCity.city.name}",
                        from = fromCity.position,
                        to = toCity.position,
                        lengthMeters = routeLength,
                        isHighPriority = true,
                        isBackupRoute = true,
                        estimatedTravelTimeMinutes = Mathf.Max(25f, routeLength / 70f * 60f),
                        projectedFreightCapacity = 10000f
                    };

                    network.Railways.Add(backupRail);
                    analysis.backupRailRoutes.Add(new BackupRailRouteData(backupRail.name, backupRail.from, backupRail.to, backupRail.lengthMeters, redundancyScore, "Redundant industrial/port freight route"));
                }
            }
        }

        private void BuildIntersections(RoadNetworkPlan network)
        {
            foreach (var city in masterPlan.cities)
            {
                network.Intersections.Add(new IntersectionPlan
                {
                    name = $"{city.name} Hub",
                    position = city.position,
                    intersectionType = "City Hub"
                });
            }

            for (int i = 0; i < network.Highways.Count; i++)
            {
                for (int j = i + 1; j < network.Highways.Count; j++)
                {
                    if (TryFindLineIntersection(network.Highways[i].from, network.Highways[i].to,
                                                 network.Highways[j].from, network.Highways[j].to,
                                                 out var intersection))
                    {
                        network.Intersections.Add(new IntersectionPlan
                        {
                            name = $"Interchange {network.Highways[i].name} × {network.Highways[j].name}",
                            position = intersection,
                            intersectionType = "Interchange",
                            connectedSegments = new List<string> { network.Highways[i].name, network.Highways[j].name }
                        });
                    }
                }
            }
        }

        private void AnalyzeNetworkPerformance(
            RoadNetworkPlan network,
            TransportationAnalysisData analysis,
            List<TransportDemandData> demandMatrix)
        {
            var connectedPairs = network.Highways.Count + network.Railways.Count;
            var totalPairs = Mathf.Max(1, masterPlan.cities.Count * (masterPlan.cities.Count - 1) / 2);
            analysis.worldConnectivityScore = Mathf.Clamp01((float)connectedPairs / totalPairs) * 100f;

            float totalTravelTime = 0f;
            int sampleCount = 0;

            foreach (var highway in network.Highways)
            {
                float speedKmh = highway.laneCount >= 6 ? 100f : highway.laneCount >= 4 ? 90f : 75f;
                float travelTime = highway.lengthMeters / 1000f / speedKmh * 60f;
                totalTravelTime += travelTime;
                sampleCount++;
            }

            foreach (var rail in network.Railways)
            {
                totalTravelTime += rail.estimatedTravelTimeMinutes;
                sampleCount++;
            }

            analysis.averageTravelTimeMinutes = sampleCount > 0 ? totalTravelTime / sampleCount : 0f;
            analysis.futureTrafficGrowthFactor = demandMatrix.Count > 0 ? demandMatrix.Average(d => d.futureTraffic / Mathf.Max(1f, d.dailyTraffic)) : 1f;

            foreach (var highway in network.Highways)
            {
                if (highway.projectedDailyTraffic > 55000f)
                {
                    analysis.futureCongestionZones.Add(new CongestionZoneData(
                        GetSegmentMidPoint(highway.from, highway.to),
                        5000f,
                        Mathf.Clamp01((highway.projectedDailyTraffic - 55000f) / 45000f),
                        "Highway congestion risk"
                    ));
                }
            }

            PopulateFutureTrafficProjections(analysis, demandMatrix);
        }

        private void PopulateFutureTrafficProjections(TransportationAnalysisData analysis, List<TransportDemandData> demandMatrix)
        {
            float projectedDaily = demandMatrix.Sum(d => d.dailyTraffic);
            float projectedFreight = demandMatrix.Sum(d => d.dailyTraffic * 0.18f);
            float growthFactor = analysis.futureTrafficGrowthFactor;

            foreach (int year in new[] { 5, 10, 20 })
            {
                float multiplier = Mathf.Pow(growthFactor, year / 10f);
                analysis.futureTrafficProjections.Add(new FutureTrafficProjectionData(
                    year,
                    projectedDaily * multiplier,
                    projectedFreight * multiplier,
                    Mathf.Clamp01((multiplier - 1f) * 0.18f)
                ));
            }
        }

        private void BuildFreightCorridors(RoadNetworkPlan network, TransportationAnalysisData analysis, List<CityNode> cityNodes, List<TransportDemandData> demandMatrix, TerrainAnalysisData corridorSource)
        {
            var freightPairs = demandMatrix.Where(d => d.demandScore >= 0.25f && (HasPort(d.fromIndex) || HasAirport(d.fromIndex) || HasPort(d.toIndex) || HasAirport(d.toIndex))).ToList();
            foreach (var freight in freightPairs)
            {
                var fromCity = cityNodes[freight.fromIndex];
                var toCity = cityNodes[freight.toIndex];
                var path = GetRoutePath(fromCity.position, toCity.position, fromCity.index, toCity.index, corridorSource);
                float routeLength = CalculateRouteLength(path);
                int lanes = AssignLaneCount(freight.futureTraffic) + 2;
                float capacity = Mathf.Max(15000f, freight.futureTraffic * 0.18f);
                var corridor = new FreightCorridorPlan
                {
                    name = $"Freight Corridor {fromCity.city.name} - {toCity.city.name}",
                    from = fromCity.position,
                    to = toCity.position,
                    lengthMeters = routeLength,
                    laneCount = Mathf.Clamp(lanes, 4, 8),
                    capacityTonsPerDay = capacity,
                    priorityTier = freight.demandScore > 0.6f ? "Tier 1" : "Tier 2"
                };

                network.FreightCorridors.Add(corridor);
                analysis.freightCorridors.Add(new FreightCorridorData(corridor.name, corridor.from, corridor.to, corridor.capacityTonsPerDay, freight.futureTraffic, corridor.priorityTier));
                analysis.expansionCorridors.Add(new ExpansionCorridorData(corridor.name, path, 0.72f, "Freight"));
            }
        }

        private void PopulateLogisticsAnalysis(
            TransportationAnalysisData analysis,
            TerrainAnalysisData corridorSource,
            List<CityNode> cityNodes)
        {
            if (corridorSource != null)
            {
                foreach (var expansion in corridorSource.bestExpansionAreas)
                {
                    analysis.bestCommercialAreas.Add(new HighValueLandData(expansion.center, expansion.buildabilityScore, "Residential"));
                    analysis.bestResidentialAreas.Add(new HighValueLandData(expansion.center, expansion.buildabilityScore, "Residential"));
                }

                foreach (var futureAirport in corridorSource.futureAirportAreas)
                {
                    analysis.bestLogisticsAreas.Add(new LogisticsAreaData(futureAirport.suggestedLocation, 5000f, futureAirport.suitabilityScore, "Future Airport Zone", "Recommended airport expansion area"));
                }

                foreach (var corridor in corridorSource.highwayCorridors)
                {
                    analysis.expansionCorridors.Add(new ExpansionCorridorData($"Corridor {corridor.fromCityIndex}-{corridor.toCityIndex}", corridor.suggestedPath, corridor.pathQuality, "Highway"));
                }
            }

            foreach (var city in cityNodes.Where(c => c.city.archetype == CityArchetype.IndustrialCity || c.city.archetype == CityArchetype.CoastalCity || HasPort(c.index)))
            {
                analysis.bestLogisticsAreas.Add(new LogisticsAreaData(city.position, 6000f, 0.88f, city.city.name, "Industrial/Port logistics"));
            }

            foreach (var city in cityNodes.Where(c => c.city.economicIdentity == EconomicIdentity.Finance || c.city.economicIdentity == EconomicIdentity.Technology || c.city.economicIdentity == EconomicIdentity.Services || c.city.archetype == CityArchetype.Capital || c.city.archetype == CityArchetype.TourismDestination))
            {
                analysis.bestCommercialAreas.Add(new HighValueLandData(city.position, 0.81f, "Commercial"));
            }
        }

        private float EstimateDemandScore(CityNode a, CityNode b)
        {
            float popScore = Mathf.Log10((a.city.populationTarget + b.city.populationTarget) / 1000f + 1f) / 5f;
            float economyScore = (GetEconomyWeight(a.city.economicIdentity) + GetEconomyWeight(b.city.economicIdentity)) * 0.5f;
            float portAirportScore = (HasAirport(a.index) || HasPort(a.index) ? 0.12f : 0f) + (HasAirport(b.index) || HasPort(b.index) ? 0.12f : 0f);
            float championship = (a.isCapital || b.isCapital) ? 0.14f : 0f;
            return Mathf.Clamp01(popScore * 0.55f + economyScore * 0.3f + portAirportScore + championship);
        }

        private float EstimateDailyTraffic(CityNode a, CityNode b, float demandScore)
        {
            float baseTraffic = 1800f + demandScore * 16000f;
            baseTraffic += (a.city.populationTarget + b.city.populationTarget) * 0.02f;
            baseTraffic += (HasPort(a.index) || HasAirport(a.index) ? 1800f : 0f);
            baseTraffic += (HasPort(b.index) || HasAirport(b.index) ? 1800f : 0f);
            return Mathf.Max(1200f, Mathf.Min(90000f, baseTraffic));
        }

        private float EstimateGrowthFactor(CityNode a, CityNode b, float demandScore)
        {
            float economyGrowth = (GetEconomyWeight(a.city.economicIdentity) + GetEconomyWeight(b.city.economicIdentity)) * 0.25f;
            float populationGrowth = Mathf.Clamp01((a.city.populationTarget + b.city.populationTarget) / 1_500_000f);
            return Mathf.Pow(1f + 0.03f + economyGrowth * 0.12f + populationGrowth * 0.06f + demandScore * 0.1f, futureYears);
        }

        private int AssignLaneCount(float projectedDailyTraffic)
        {
            if (projectedDailyTraffic > 70000f) return 8;
            if (projectedDailyTraffic > 42000f) return 6;
            if (projectedDailyTraffic > 22000f) return 4;
            return 2;
        }

        private string GetDemandCategory(float score)
        {
            if (score >= 0.75f) return "Very High";
            if (score >= 0.45f) return "High";
            if (score >= 0.20f) return "Medium";
            return "Low";
        }

        private float GetEconomyWeight(EconomicIdentity identity)
        {
            return identity switch
            {
                EconomicIdentity.Technology => 0.92f,
                EconomicIdentity.Manufacturing => 0.88f,
                EconomicIdentity.Finance => 0.82f,
                EconomicIdentity.Services => 0.78f,
                EconomicIdentity.Tourism => 0.68f,
                EconomicIdentity.Agriculture => 0.46f,
                _ => 0.58f,
            };
        }

        private bool HasAirport(int cityIndex)
        {
            return masterPlan.infrastructure?.airports?.Any(a => a.cityIndex == cityIndex) ?? false;
        }

        private bool HasPort(int cityIndex)
        {
            return masterPlan.infrastructure?.ports?.Any(p => p.cityIndex == cityIndex) ?? false;
        }

        private List<Vector2> GetRoutePath(Vector2 start, Vector2 end, int fromCityIndex, int toCityIndex, TerrainAnalysisData corridorSource)
        {
            var corridor = FindExistingCorridor(fromCityIndex, toCityIndex, corridorSource);
            if (corridor != null && corridor.suggestedPath.Count >= 3 && corridor.pathQuality > 0.3f)
            {
                return corridor.suggestedPath;
            }

            var route = CreateStraightRoute(start, end, sampleCount: 24);
            return OptimizeRouteForTerrain(route);
        }

        private HighwayCorridorData FindExistingCorridor(int fromCityIndex, int toCityIndex, TerrainAnalysisData corridorSource)
        {
            if (corridorSource == null)
                return null;

            return corridorSource.highwayCorridors.FirstOrDefault(c =>
                (c.fromCityIndex == fromCityIndex && c.toCityIndex == toCityIndex) ||
                (c.fromCityIndex == toCityIndex && c.toCityIndex == fromCityIndex)
            );
        }

        private List<Vector2> OptimizeRouteForTerrain(List<Vector2> route)
        {
            var optimized = new List<Vector2>(route);

            for (int i = 0; i < optimized.Count; i++)
            {
                var point = optimized[i];
                float height = SampleTerrainHeight(point);
                float penalty = GetTerrainPenalty(point, height);

                if (penalty > 0.45f && i > 0 && i < optimized.Count - 1)
                {
                    var previous = optimized[i - 1];
                    var next = optimized[i + 1];
                    var offset = (next - previous).normalized;
                    optimized[i] += new Vector2(-offset.y, offset.x) * penalty * 300f;
                }
            }

            return optimized;
        }

        private float GetTerrainPenalty(Vector2 worldPosition, float height)
        {
            float penalty = 0f;
            if (height <= seaLevel) penalty += 0.45f;
            if (height > seaLevel + 100f) penalty += 0.10f;
            penalty += GetWaterPenalty(worldPosition) * 0.4f;
            penalty += GetSlopePenalty(worldPosition) * 0.25f;
            return Mathf.Clamp01(penalty);
        }

        private float GetWaterPenalty(Vector2 worldPosition)
        {
            float penalty = 0f;
            foreach (var lake in terrainData.lakes)
            {
                if (Vector2.Distance(worldPosition, lake.center) < lake.radiusMeters + 1800f)
                    penalty += 0.35f;
            }
            foreach (var river in terrainData.rivers)
            {
                foreach (var point in river.pathPoints)
                {
                    if (Vector2.Distance(worldPosition, point) < river.width * 1.5f + 1200f)
                        penalty += 0.18f;
                }
            }
            return Mathf.Clamp01(penalty);
        }

        private float GetSlopePenalty(Vector2 worldPosition)
        {
            float sampleHeight = SampleTerrainHeight(worldPosition);
            float sampleOffset = 25f;
            var other = new Vector2(worldPosition.x + sampleOffset, worldPosition.y + sampleOffset);
            float sampleHeight2 = SampleTerrainHeight(other);
            float distance = Vector2.Distance(worldPosition, other);
            if (distance <= 0f) return 0f;

            float slope = Mathf.Abs(sampleHeight2 - sampleHeight) / distance;
            return Mathf.Clamp01(slope * 3f);
        }

        private List<Vector2> CreateStraightRoute(Vector2 start, Vector2 end, int sampleCount)
        {
            var route = new List<Vector2>();
            sampleCount = Mathf.Clamp(sampleCount, 4, 64);

            for (int step = 0; step < sampleCount; step++)
            {
                float t = (float)step / (sampleCount - 1);
                route.Add(Vector2.Lerp(start, end, t));
            }

            return route;
        }

        private RouteInfo AnalyzeRoute(List<Vector2> route)
        {
            var info = new RouteInfo();
            float previousHeight = SampleTerrainHeight(route[0]);
            float slopeSum = 0f;
            int slopeCount = 0;

            for (int i = 1; i < route.Count; i++)
            {
                var sample = route[i];
                float height = SampleTerrainHeight(sample);
                float distance = Vector2.Distance(route[i - 1], sample);
                float slope = 0f;

                if (distance > 0f)
                {
                    slope = Mathf.Rad2Deg * Mathf.Atan(Mathf.Abs(height - previousHeight) / distance);
                }

                slopeSum += slope;
                slopeCount++;
                info.maximumSlope = Mathf.Max(info.maximumSlope, slope);

                if (height <= seaLevel || IsOnWaterFeature(sample))
                {
                    info.requiresBridge = true;
                }

                if (slope >= 9f || height > seaLevel + 180f && slope >= 6f)
                {
                    info.requiresTunnel = true;
                }

                previousHeight = height;
            }

            info.averageSlope = slopeCount > 0 ? slopeSum / slopeCount : 0f;
            return info;
        }

        private bool IsOnWaterFeature(Vector2 worldPosition)
        {
            if (terrainData == null) return false;
            if (terrainData.coastline?.coastlinePoints != null && terrainData.coastline.coastlinePoints.Any(p => Vector2.Distance(p, worldPosition) < 1200f))
                return true;
            if (terrainData.lakes.Any(l => Vector2.Distance(l.center, worldPosition) < l.radiusMeters + 600f))
                return true;
            if (terrainData.rivers.Any(r => r.pathPoints.Any(p => Vector2.Distance(p, worldPosition) < r.width * 1.2f + 300f)))
                return true;
            return false;
        }

        private float SampleTerrainHeight(Vector2 worldPosition)
        {
            float xNorm = Mathf.InverseLerp(worldMin.x, worldMax.x, worldPosition.x);
            float zNorm = Mathf.InverseLerp(worldMin.y, worldMax.y, worldPosition.y);
            float sampleX = Mathf.Clamp01(xNorm) * (terrainData.heightMap.width - 1);
            float sampleY = Mathf.Clamp01(zNorm) * (terrainData.heightMap.height - 1);
            return terrainData.heightMap.GetHeightInterpolated(sampleX, sampleY);
        }

        private BridgePlan CreateBridgePlan(HighwayPlan highway)
        {
            return new BridgePlan
            {
                name = highway.name + " Bridge",
                from = highway.from,
                to = highway.to,
                spanMeters = Mathf.Max(80f, highway.lengthMeters * 0.07f)
            };
        }

        private TunnelPlan CreateTunnelPlan(HighwayPlan highway, float maxSlope)
        {
            return new TunnelPlan
            {
                name = highway.name + " Tunnel",
                from = highway.from,
                to = highway.to,
                boreMeters = Mathf.Max(40f, highway.lengthMeters * 0.04f)
            };
        }

        private void AddHighwayCorridorAnalysis(TransportationAnalysisData analysis, int fromIndex, int toIndex, List<Vector2> path, RouteInfo routeInfo)
        {
            analysis.expansionCorridors.Add(new ExpansionCorridorData(
                $"Highway Corridor {fromIndex}-{toIndex}",
                path,
                Mathf.Clamp01(1f - (routeInfo.averageSlope / 24f + (routeInfo.requiresBridge ? 0.15f : 0f) + (routeInfo.requiresTunnel ? 0.15f : 0f))),
                "Highway"
            ));
        }

        private Vector2 FindClosestHighwayPoint(RoadNetworkPlan network, Vector2 target, Vector2 fallback)
        {
            var bestPoint = fallback;
            float bestDistance = float.MaxValue;

            foreach (var highway in network.Highways)
            {
                var point = ClosestPointOnSegment(highway.from, highway.to, target);
                float distance = Vector2.Distance(point, target);
                if (distance < bestDistance)
                {
                    bestDistance = distance;
                    bestPoint = point;
                }
            }

            return bestDistance < 18000f ? bestPoint : fallback;
        }

        private Vector2 ClosestPointOnSegment(Vector2 a, Vector2 b, Vector2 point)
        {
            var ab = b - a;
            float t = Vector2.Dot(point - a, ab) / Vector2.Dot(ab, ab);
            t = Mathf.Clamp01(t);
            return a + ab * t;
        }

        private Vector2 GetSegmentMidPoint(Vector2 start, Vector2 end)
        {
            return Vector2.Lerp(start, end, 0.5f);
        }

        private bool TryFindLineIntersection(Vector2 a1, Vector2 a2, Vector2 b1, Vector2 b2, out Vector2 intersection)
        {
            intersection = Vector2.zero;
            float d = (a2.x - a1.x) * (b2.y - b1.y) - (a2.y - a1.y) * (b2.x - b1.x);
            if (Mathf.Approximately(d, 0f))
                return false;

            float u = ((b1.x - a1.x) * (b2.y - b1.y) - (b1.y - a1.y) * (b2.x - b1.x)) / d;
            float v = ((b1.x - a1.x) * (a2.y - a1.y) - (b1.y - a1.y) * (a2.x - a1.x)) / d;

            if (u >= 0f && u <= 1f && v >= 0f && v <= 1f)
            {
                intersection = a1 + u * (a2 - a1);
                return true;
            }

            return false;
        }

        private static string RoadKey(Vector2 from, Vector2 to)
        {
            return $"{from.x:F2},{from.y:F2}->{to.x:F2},{to.y:F2}";
        }

        private void UpdateMasterPlanTransportNetwork(RoadNetworkPlan roadNetwork, List<CityNode> cityNodes)
        {
            var transportNetwork = masterPlan.transportNetwork ?? new TransportNetworkData();
            transportNetwork.nodes.Clear();
            transportNetwork.edges.Clear();

            foreach (var node in cityNodes)
            {
                transportNetwork.AddNode(new TransportNodeData(node.index, node.position, node.isCapital));
            }

            void AddEdge(int fromIndex, int toIndex, string roadType, float lengthKm, int priority)
            {
                if (fromIndex < 0 || toIndex < 0 || fromIndex == toIndex)
                    return;

                if (transportNetwork.edges.Any(e =>
                    (e.fromNodeIndex == fromIndex && e.toNodeIndex == toIndex) ||
                    (e.fromNodeIndex == toIndex && e.toNodeIndex == fromIndex)))
                {
                    return;
                }

                transportNetwork.AddEdge(new TransportEdgeData(fromIndex, toIndex, roadType, lengthKm, priority));
            }

            foreach (var highway in roadNetwork.Highways)
            {
                int fromCity = FindClosestCityIndex(highway.from, cityNodes);
                int toCity = FindClosestCityIndex(highway.to, cityNodes);
                AddEdge(fromCity, toCity, "Highway", highway.lengthMeters / 1000f, 10);
            }

            foreach (var street in roadNetwork.MainStreets)
            {
                int fromCity = FindClosestCityIndex(street.from, cityNodes);
                int toCity = FindClosestCityIndex(street.to, cityNodes);
                AddEdge(fromCity, toCity, "Secondary", street.lengthMeters / 1000f, 5);
            }

            foreach (var street in roadNetwork.SecondaryStreets)
            {
                int fromCity = FindClosestCityIndex(street.from, cityNodes);
                int toCity = FindClosestCityIndex(street.to, cityNodes);
                AddEdge(fromCity, toCity, "Local", street.lengthMeters / 1000f, 3);
            }

            masterPlan.transportNetwork = transportNetwork;
        }

        private int FindClosestCityIndex(Vector2 position, List<CityNode> cityNodes)
        {
            int bestIndex = -1;
            float bestDistance = float.MaxValue;

            foreach (var node in cityNodes)
            {
                float distance = Vector2.Distance(node.position, position);
                if (distance < bestDistance)
                {
                    bestDistance = distance;
                    bestIndex = node.index;
                }
            }

            return bestIndex;
        }

        private class CityNode
        {
            public int index;
            public CityMasterPlanData city;
            public Vector2 position;
            public bool isCapital;
        }

        private class TransportDemandData
        {
            public int fromIndex;
            public int toIndex;
            public float demandScore;
            public float dailyTraffic;
            public float futureTraffic;
            public string category;
        }

        private class RouteInfo
        {
            public bool requiresBridge;
            public bool requiresTunnel;
            public float averageSlope;
            public float maximumSlope;
        }
    }
}
