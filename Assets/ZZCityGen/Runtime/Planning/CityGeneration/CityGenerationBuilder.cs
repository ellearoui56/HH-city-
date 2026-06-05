using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using ZZCityGen.Data;
using ZZCityGen.Planning.MasterPlanning;
using ZZCityGen.Planning.CityGeneration;

namespace ZZCityGen.Planning.CityGeneration
{
    public sealed class CityGenerationBuilder
    {
        private readonly MasterPlanData masterPlan;
        private readonly RoadNetworkPlan roadNetwork;
        private readonly TransportationAnalysisData transportAnalysis;
        private readonly System.Random random;

        public CityGenerationBuilder(MasterPlanData masterPlan, RoadNetworkPlan roadNetwork = null, TransportationAnalysisData transportAnalysis = null)
        {
            this.masterPlan = masterPlan ?? throw new ArgumentNullException(nameof(masterPlan));
            this.roadNetwork = roadNetwork;
            this.transportAnalysis = transportAnalysis;
            random = new System.Random(masterPlan.seed + 97123);
        }

        public CityGenerationPackage BuildCityLayouts(out UrbanAnalysisData urbanAnalysis)
        {
            var package = new CityGenerationPackage
            {
                worldName = masterPlan.worldName,
                seed = masterPlan.seed,
                generatedTimestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds()
            };

            if (masterPlan.cities == null || masterPlan.cities.Count == 0)
            {
                urbanAnalysis = new UrbanAnalysisData
                {
                    worldName = masterPlan.worldName,
                    seed = masterPlan.seed,
                    generatedTimestamp = package.generatedTimestamp
                };
                return package;
            }

            var capital = masterPlan.cities.FirstOrDefault(c => c.isCapital);
            for (var cityIndex = 0; cityIndex < masterPlan.cities.Count; cityIndex++)
            {
                var city = masterPlan.cities[cityIndex];
                if (capital != null)
                {
                    city.distanceFromCapitalKm = Vector2.Distance(city.position, capital.position) / 1000f;
                }

                var layout = BuildCityLayout(city, cityIndex);
                package.cities.Add(layout);
            }

            urbanAnalysis = CreateUrbanAnalysis(package);
            return package;
        }

        private CityLayoutData BuildCityLayout(CityMasterPlanData city, int cityIndex)
        {
            var layout = new CityLayoutData
            {
                name = city.name,
                archetype = city.archetype,
                economicIdentity = DetermineEconomicIdentity(city),
                identityDescription = DetermineCityIdentity(city),
                position = city.position,
                currentRadiusMeters = city.radiusMeters,
                futureExpansionRadiusMeters = city.radiusMeters * 1.32f,
                centerType = DetermineCityCenterType(city)
            };

            layout.cityScore = CalculateCityScore(city, layout);
            layout.center = DetermineCityCenter(city, layout);
            layout.boundaryPoints = GenerateCityBoundary(city, layout);
            layout.growthZones = GenerateGrowthZones(city, layout);
            layout.districts = BuildDistrictLayouts(city, layout);
            layout.mainStreets = GenerateMainStreets(city, layout);
            layout.ringRoads = GenerateRingRoads(city, layout);
            layout.serviceSites = GenerateServiceSites(city, layout);
            layout.parks = GenerateParks(city, layout);
            layout.landValueMap = GenerateLandValueMap(city, layout);
            layout.cityQualityScore = CalculateCityQuality(layout);
            layout.regenerationRecommendation = layout.cityQualityScore < 54f ? "Consider regenerating layout for better walkability and traffic efficiency." : "Layout quality is acceptable for planning stage.";
            return layout;
        }

        private float CalculateCityScore(CityMasterPlanData city, CityLayoutData layout)
        {
            float populationScore = Mathf.Clamp01(city.targetPopulation / 350000f);
            float economyScore = city.economicIdentity switch
            {
                EconomicIdentity.Technology => 0.96f,
                EconomicIdentity.Finance => 0.94f,
                EconomicIdentity.Manufacturing => 0.88f,
                EconomicIdentity.Services => 0.82f,
                EconomicIdentity.Tourism => 0.78f,
                EconomicIdentity.Agriculture => 0.72f,
                _ => 0.80f
            };

            float locationScore = 0.5f;
            if (masterPlan.terrainAnalysis?.zones != null)
            {
                var bestZone = masterPlan.terrainAnalysis.GetMostBuildableZone();
                if (bestZone != null)
                {
                    locationScore = Mathf.Clamp01(0.55f + 0.35f * bestZone.percentageOfWorld);
                }
            }

            float transportScore = 0.45f;
            if (transportAnalysis != null)
            {
                transportScore = Mathf.Clamp01(transportAnalysis.worldConnectivityScore / 100f * 0.8f + 0.2f);
            }
            else if (masterPlan.transportNetwork?.nodes != null)
            {
                transportScore = Mathf.Clamp01(Mathf.Min(1f, masterPlan.transportNetwork.highwayCount / 3f + masterPlan.transportNetwork.secondaryRoadCount / 6f));
            }

            float tourismScore = city.archetype == CityArchetype.TourismDestination || city.archetype == CityArchetype.CoastalCity ? 0.9f : 0.45f;
            float industryScore = city.archetype == CityArchetype.IndustrialCity ? 0.9f : 0.48f;
            float identityScore = city.economicIdentity switch
            {
                EconomicIdentity.Technology => 0.92f,
                EconomicIdentity.Finance => 0.90f,
                EconomicIdentity.Manufacturing => 0.85f,
                EconomicIdentity.Services => 0.80f,
                EconomicIdentity.Tourism => 0.76f,
                EconomicIdentity.Agriculture => 0.70f,
                _ => 0.78f
            };

            float raw = populationScore * 0.24f + economyScore * 0.20f + locationScore * 0.16f + transportScore * 0.18f + tourismScore * 0.12f + industryScore * 0.10f;
            return Mathf.Clamp01(raw) * 100f;
        }

        private CityArchetype GetArchetype(CityMasterPlanData city)
        {
            return city?.archetype ?? CityArchetype.FamilyCity;
        }

        private EconomicIdentity DetermineEconomicIdentity(CityMasterPlanData city)
        {
            if (city.economicIdentity != EconomicIdentity.Mixed)
            {
                return city.economicIdentity;
            }

            return city.archetype switch
            {
                CityArchetype.Capital => EconomicIdentity.Finance,
                CityArchetype.FamilyCity => EconomicIdentity.Services,
                CityArchetype.IndustrialCity => EconomicIdentity.Manufacturing,
                CityArchetype.RuralTown => EconomicIdentity.Agriculture,
                CityArchetype.CoastalCity => EconomicIdentity.Tourism,
                CityArchetype.UniversityCity => EconomicIdentity.Technology,
                CityArchetype.TourismDestination => EconomicIdentity.Tourism,
                _ => EconomicIdentity.Mixed
            };
        }

        private string DetermineCityIdentity(CityMasterPlanData city)
        {
            var identity = DetermineEconomicIdentity(city);
            return identity switch
            {
                EconomicIdentity.Technology => "Technology City",
                EconomicIdentity.Finance => "Financial Center",
                EconomicIdentity.Manufacturing => "Industrial City",
                EconomicIdentity.Services => "Service Economy City",
                EconomicIdentity.Tourism => "Tourism Destination",
                EconomicIdentity.Agriculture => "Agricultural Hub",
                _ => "Mixed-Use City"
            };
        }

        private string DetermineCityCenterType(CityMasterPlanData city)
        {
            return city.archetype switch
            {
                CityArchetype.Capital => "Government & Business Center",
                CityArchetype.FamilyCity => "Town Center",
                CityArchetype.IndustrialCity => "Central Industrial Hub",
                CityArchetype.RuralTown => "Market Town Center",
                CityArchetype.CoastalCity => "Harbor District",
                CityArchetype.UniversityCity => "Academic Core",
                CityArchetype.TourismDestination => "Resort Center",
                _ => "City Center"
            };
        }

        private Vector2 DetermineCityCenter(CityMasterPlanData city, CityLayoutData layout)
        {
            if (roadNetwork != null && roadNetwork.Highways != null)
            {
                var closestHighway = roadNetwork.Highways.OrderBy(h => Vector2.Distance(city.position, h.from)).ThenBy(h => Vector2.Distance(city.position, h.to)).FirstOrDefault();
                if (closestHighway != null)
                {
                    var closestPoint = FindClosestPointOnLine(closestHighway.from, closestHighway.to, city.position);
                    return Vector2.Lerp(city.position, closestPoint, 0.18f);
                }
            }

            return city.position;
        }

        private List<Vector2> GenerateCityBoundary(CityMasterPlanData city, CityLayoutData layout)
        {
            var boundary = new List<Vector2>();
            var segments = 12;
            var waterZone = GetNearestTerrainZone(city.position, TerrainZoneType.Water);
            var mountainZone = GetNearestTerrainZone(city.position, TerrainZoneType.Mountain);

            for (var i = 0; i < segments; i++)
            {
                var angle = 2f * Mathf.PI * i / segments;
                var direction = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
                var radius = city.radiusMeters * Mathf.Lerp(0.88f, 1.06f, (float)random.NextDouble());
                if (waterZone != null)
                {
                    var toWater = (waterZone.center - city.position).normalized;
                    var dot = Vector2.Dot(direction, toWater);
                    if (dot > 0.45f)
                    {
                        radius *= city.archetype == CityArchetype.CoastalCity ? 1.10f : 0.85f;
                    }
                }

                if (mountainZone != null)
                {
                    var toMountain = (mountainZone.center - city.position).normalized;
                    var dot = Vector2.Dot(direction, toMountain);
                    if (dot > 0.3f)
                    {
                        radius *= 0.74f;
                    }
                }

                var point = city.position + direction * radius;
                boundary.Add(point);
            }

            return boundary;
        }

        private List<GrowthZoneData> GenerateGrowthZones(CityMasterPlanData city, CityLayoutData layout)
        {
            var zones = new List<GrowthZoneData>();
            zones.Add(new GrowthZoneData
            {
                name = "Current Urban Footprint",
                zoneType = "CurrentCity",
                boundaryPoints = GenerateCircularBand(city.position, city.radiusMeters * 0.92f, 10, 0.08f),
                score = 0.88f
            });

            zones.Add(new GrowthZoneData
            {
                name = "Future Expansion Area",
                zoneType = "Expansion",
                boundaryPoints = GenerateCircularBand(city.position, city.radiusMeters * 1.35f, 12, 0.12f),
                score = 0.68f
            });

            return zones;
        }

        private List<Vector2> GenerateCircularBand(Vector2 center, float radius, int count, float noiseFactor)
        {
            var points = new List<Vector2>();
            for (var i = 0; i < count; i++)
            {
                var angle = 2f * Mathf.PI * i / count;
                var radiusAdjustment = radius * (1f + ((float)random.NextDouble() - 0.5f) * noiseFactor);
                points.Add(center + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radiusAdjustment);
            }
            return points;
        }

        private List<DistrictLayoutData> BuildDistrictLayouts(CityMasterPlanData city, CityLayoutData layout)
        {
            var districts = new List<DistrictLayoutData>();
            var recipe = GetDistrictRecipe(city.archetype);
            for (var i = 0; i < recipe.Count; i++)
            {
                var districtType = recipe[i];
                var angle = 2f * Mathf.PI * i / recipe.Count + ((float)random.NextDouble() - 0.5f) * 0.25f;
                var distance = city.radiusMeters * Mathf.Lerp(0.22f, 0.46f, (float)random.NextDouble());
                var center = city.position + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * distance;
                var size = city.radiusMeters * Mathf.Lerp(0.32f, 0.58f, (float)random.NextDouble());
                var area = size * size;
                var density = DetermineDistrictDensity(districtType);
                var population = (int)Mathf.Max(0, city.targetPopulation * density * 0.28f);
                var jobs = (int)Mathf.Max(0, city.targetPopulation * Mathf.Lerp(0.08f, 0.28f, density));

                districts.Add(new DistrictLayoutData
                {
                    name = GetDistrictName(city, districtType, i),
                    districtType = districtType,
                    bounds = new Rect(center.x - size * 0.5f, center.y - size * 0.5f, size, size),
                    densityScore = density,
                    populationTarget = population,
                    jobsTarget = jobs,
                    areaSquareMeters = area,
                    attractionScore = Mathf.Clamp01(0.3f + density * 0.6f)
                });
            }
            return districts;
        }

        private List<DistrictType> GetDistrictRecipe(CityArchetype archetype)
        {
            return archetype switch
            {
                CityArchetype.Capital => new List<DistrictType> { DistrictType.Downtown, DistrictType.Government, DistrictType.Business, DistrictType.University, DistrictType.Industrial, DistrictType.Park, DistrictType.Residential },
                CityArchetype.FamilyCity => new List<DistrictType> { DistrictType.Downtown, DistrictType.Residential, DistrictType.Commercial, DistrictType.Education, DistrictType.Park },
                CityArchetype.IndustrialCity => new List<DistrictType> { DistrictType.Industrial, DistrictType.FreightTerminal, DistrictType.Commercial, DistrictType.Residential, DistrictType.Park },
                CityArchetype.RuralTown => new List<DistrictType> { DistrictType.Tourism, DistrictType.Residential, DistrictType.Agriculture, DistrictType.Commercial, DistrictType.Park },
                CityArchetype.CoastalCity => new List<DistrictType> { DistrictType.Port, DistrictType.Tourism, DistrictType.Residential, DistrictType.Commercial, DistrictType.Park },
                CityArchetype.UniversityCity => new List<DistrictType> { DistrictType.University, DistrictType.Residential, DistrictType.Commercial, DistrictType.Education, DistrictType.Park },
                CityArchetype.TourismDestination => new List<DistrictType> { DistrictType.Tourism, DistrictType.Residential, DistrictType.Commercial, DistrictType.Park },
                _ => new List<DistrictType> { DistrictType.Downtown, DistrictType.Residential, DistrictType.Commercial, DistrictType.Park }
            };
        }

        private string GetDistrictName(CityMasterPlanData city, DistrictType type, int index)
        {
            var baseName = type switch
            {
                DistrictType.Downtown => "Downtown",
                DistrictType.Government => "Government Quarter",
                DistrictType.Business => "Business District",
                DistrictType.University => "University Campus",
                DistrictType.Industrial => "Industrial Zone",
                DistrictType.Park => "Urban Park",
                DistrictType.Residential => "Residential Quarter",
                DistrictType.Commercial => "Commercial Strip",
                DistrictType.Education => "Education District",
                DistrictType.Tourism => "Tourism District",
                DistrictType.Port => "Harbor District",
                DistrictType.FreightTerminal => "Logistics Hub",
                DistrictType.Agriculture => "Agri-Belt",
                _ => type.ToString()
            };
            return city.name + " " + baseName;
        }

        private float DetermineDistrictDensity(DistrictType type)
        {
            return type switch
            {
                DistrictType.Downtown => 0.98f,
                DistrictType.Business => 0.85f,
                DistrictType.University => 0.72f,
                DistrictType.Industrial => 0.48f,
                DistrictType.Residential => 0.56f,
                DistrictType.Commercial => 0.60f,
                DistrictType.Education => 0.42f,
                DistrictType.Tourism => 0.52f,
                DistrictType.Park => 0.12f,
                DistrictType.Port => 0.44f,
                DistrictType.FreightTerminal => 0.24f,
                DistrictType.Agriculture => 0.18f,
                _ => 0.50f
            };
        }

        private List<StreetSegmentPlan> GenerateMainStreets(CityMasterPlanData city, CityLayoutData layout)
        {
            var streets = new List<StreetSegmentPlan>();
            var streetBases = new List<Vector2>();
            foreach (var district in layout.districts)
            {
                streetBases.Add(district.bounds.center);
            }

            for (var i = 0; i < streetBases.Count; i++)
            {
                var target = streetBases[i];
                var street = CreateStreetSegment($"Main Avenue {city.name} {i + 1}", layout.center, target, 16f, "Avenue");
                streets.Add(street);
            }

            for (var i = 0; i < streetBases.Count; i++)
            {
                var a = streetBases[i];
                var b = streetBases[(i + 1) % streetBases.Count];
                streets.Add(CreateStreetSegment($"Boulevard {city.name} {i + 1}", a, b, 14f, "Boulevard"));
            }

            if (roadNetwork != null && roadNetwork.Highways != null && roadNetwork.Highways.Count > 0)
            {
                var highway = roadNetwork.Highways.OrderBy(h => Vector2.Distance(city.position, h.from)).First();
                streets.Add(CreateStreetSegment($"City Connector {city.name}", layout.center, FindClosestPointOnLine(highway.from, highway.to, layout.center), 18f, "Connector"));
            }

            return streets;
        }

        private List<StreetSegmentPlan> GenerateRingRoads(CityMasterPlanData city, CityLayoutData layout)
        {
            var rings = new List<StreetSegmentPlan>();
            var count = Mathf.Max(4, layout.districts.Count);
            var innerRadius = city.radiusMeters * 0.78f;
            for (var i = 0; i < count; i++)
            {
                var angleA = 2f * Mathf.PI * i / count;
                var angleB = 2f * Mathf.PI * ((i + 1) % count) / count;
                var a = layout.center + new Vector2(Mathf.Cos(angleA), Mathf.Sin(angleA)) * innerRadius;
                var b = layout.center + new Vector2(Mathf.Cos(angleB), Mathf.Sin(angleB)) * innerRadius;
                rings.Add(CreateStreetSegment($"Ring Road {city.name} {i + 1}", a, b, 18f, "RingRoad"));
            }
            return rings;
        }

        private List<CityServiceSiteData> GenerateServiceSites(CityMasterPlanData city, CityLayoutData layout)
        {
            var services = new List<CityServiceSiteData>();
            var centralOffset = layout.currentRadiusMeters * 0.13f;
            services.Add(new CityServiceSiteData
            {
                name = $"City Hall {city.name}",
                serviceType = "Government",
                position = layout.center + new Vector2(centralOffset, 0),
                serviceRadiusMeters = layout.currentRadiusMeters * 0.16f,
                importanceScore = 0.94f
            });
            services.Add(new CityServiceSiteData
            {
                name = $"Hospital {city.name}",
                serviceType = "Healthcare",
                position = layout.center + new Vector2(-centralOffset * 0.9f, centralOffset * 0.4f),
                serviceRadiusMeters = layout.currentRadiusMeters * 0.14f,
                importanceScore = 0.90f
            });
            services.Add(new CityServiceSiteData
            {
                name = $"School District {city.name}",
                serviceType = "Education",
                position = layout.center + new Vector2(centralOffset * 0.45f, -centralOffset * 0.7f),
                serviceRadiusMeters = layout.currentRadiusMeters * 0.12f,
                importanceScore = 0.82f
            });
            services.Add(new CityServiceSiteData
            {
                name = $"Fire Station {city.name}",
                serviceType = "Emergency",
                position = layout.center + new Vector2(-centralOffset * 0.5f, -centralOffset * 0.8f),
                serviceRadiusMeters = layout.currentRadiusMeters * 0.10f,
                importanceScore = 0.76f
            });

            if (city.archetype == CityArchetype.TourismDestination || city.archetype == CityArchetype.CoastalCity)
            {
                services.Add(new CityServiceSiteData
                {
                    name = $"Tourism Office {city.name}",
                    serviceType = "Tourism",
                    position = layout.center + new Vector2(centralOffset * 0.8f, -centralOffset * 0.2f),
                    serviceRadiusMeters = layout.currentRadiusMeters * 0.10f,
                    importanceScore = 0.70f
                });
            }

            return services;
        }

        private List<ParkReserveData> GenerateParks(CityMasterPlanData city, CityLayoutData layout)
        {
            var parks = new List<ParkReserveData>();
            var parkCount = Mathf.Clamp((int)Mathf.Round(layout.districts.Count * 0.35f), 2, 4);
            var increment = 2f * Mathf.PI / parkCount;
            var radius = city.radiusMeters * 0.62f;
            for (var i = 0; i < parkCount; i++)
            {
                var angle = i * increment + ((float)random.NextDouble() - 0.5f) * 0.28f;
                parks.Add(new ParkReserveData
                {
                    name = $"Park {city.name} {i + 1}",
                    center = layout.center + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius,
                    radiusMeters = city.radiusMeters * Mathf.Lerp(0.08f, 0.14f, (float)random.NextDouble()),
                    parkType = i == 0 ? "City Park" : "Urban Forest",
                    serenityScore = Mathf.Clamp01(0.6f + (float)random.NextDouble() * 0.35f)
                });
            }
            return parks;
        }

        private List<LandValueSample> GenerateLandValueMap(CityMasterPlanData city, CityLayoutData layout)
        {
            var landValues = new List<LandValueSample>();
            var samplePoints = new List<(string label, Vector2 point, DistrictType type)>
            {
                ("Downtown Core", layout.center, DistrictType.Downtown),
                ("Western Edge", layout.center + new Vector2(-layout.currentRadiusMeters * 0.75f, 0), DistrictType.Residential),
                ("Eastern Edge", layout.center + new Vector2(layout.currentRadiusMeters * 0.75f, 0), DistrictType.Tourism),
                ("Northern Gateway", layout.center + new Vector2(0, layout.currentRadiusMeters * 0.75f), DistrictType.Commercial),
                ("Southern Expansion", layout.center + new Vector2(0, -layout.currentRadiusMeters * 0.75f), DistrictType.Residential)
            };

            foreach (var district in layout.districts)
            {
                samplePoints.Add((district.name, district.bounds.center, district.districtType));
            }

            foreach (var service in layout.serviceSites)
            {
                samplePoints.Add(($"Near {service.serviceType}", service.position, DistrictType.Commercial));
            }

            var uniquePoints = new List<(string label, Vector2 point, DistrictType type)>();
            foreach (var entry in samplePoints)
            {
                if (!uniquePoints.Any(existing => Vector2.Distance(existing.point, entry.point) < 0.1f))
                {
                    uniquePoints.Add(entry);
                }
                if (uniquePoints.Count >= 12)
                {
                    break;
                }
            }

            foreach (var entry in uniquePoints)
            {
                var distance = Vector2.Distance(entry.point, layout.center);
                var baseValue = Mathf.Lerp(95f, 20f, Mathf.Clamp01(distance / layout.futureExpansionRadiusMeters));
                var parkBonus = layout.parks.Any(p => Vector2.Distance(p.center, entry.point) <= p.radiusMeters * 1.2f) ? 8f : 0f;
                var serviceBonus = layout.serviceSites.Any(s => Vector2.Distance(s.position, entry.point) <= s.serviceRadiusMeters * 1.1f) ? 5f : 0f;
                var premium = Mathf.Clamp(baseValue + parkBonus + serviceBonus, 18f, 100f);

                landValues.Add(new LandValueSample
                {
                    label = entry.label,
                    position = entry.point,
                    valueScore = premium,
                    distanceFromCenter = distance,
                    preferredDistrict = entry.type,
                    reason = premium > 80f ? "Prime mixed-use edge near amenities" : "Balanced location for city growth"
                });
            }

            return landValues.OrderByDescending(v => v.valueScore).ToList();
        }

        private float CalculateCityQuality(CityLayoutData layout)
        {
            var walkability = Mathf.Clamp01(0.55f + (layout.mainStreets.Count / 10f) * 0.18f + (layout.parks.Count / 5f) * 0.08f);
            var accessibility = Mathf.Clamp01(0.50f + (layout.ringRoads.Count / 8f) * 0.18f + (layout.serviceSites.Count / 8f) * 0.12f);
            var growthPotential = Mathf.Clamp01(0.45f + (layout.futureExpansionRadiusMeters / layout.currentRadiusMeters - 0.9f) * 0.4f);
            var trafficEfficiency = Mathf.Clamp01(0.50f + (layout.mainStreets.Count / 12f) * 0.16f + (layout.ringRoads.Count / 8f) * 0.14f);
            return Mathf.Clamp01((walkability + accessibility + growthPotential + trafficEfficiency) / 4f) * 100f;
        }

        private UrbanAnalysisData CreateUrbanAnalysis(CityGenerationPackage package)
        {
            var analysis = new UrbanAnalysisData
            {
                worldName = package.worldName,
                seed = package.seed,
                generatedTimestamp = package.generatedTimestamp
            };

            foreach (var city in package.cities)
            {
                var premiumSamples = city.landValueMap.OrderByDescending(s => s.valueScore).Take(3);
                foreach (var sample in premiumSamples)
                {
                    analysis.premiumLandZones.Add(new CityOpportunityAreaData
                    {
                        name = $"{city.name} Premium Zone: {sample.label}",
                        position = sample.position,
                        recommendedUse = sample.preferredDistrict == DistrictType.Downtown ? "Tower / Landmark" : "High-value Mixed-Use",
                        priorityScore = sample.valueScore,
                        rationale = sample.reason,
                        radiusMeters = city.currentRadiusMeters * 0.12f
                    });
                }

                var housing = city.districts.Where(d => d.districtType == DistrictType.Residential || d.districtType == DistrictType.LuxuryResidential || d.districtType == DistrictType.PopularResidential).OrderByDescending(d => d.attractionScore).Take(2);
                foreach (var district in housing)
                {
                    analysis.bestHousingAreas.Add(new CityOpportunityAreaData
                    {
                        name = $"{city.name} Housing Area: {district.name}",
                        position = district.bounds.center,
                        recommendedUse = "Residential / Community",
                        priorityScore = district.attractionScore * 100f,
                        rationale = "Good balance of density, green access, and services.",
                        radiusMeters = Mathf.Max(100f, district.bounds.width * 0.4f)
                    });
                }

                var industrial = city.districts.Where(d => d.districtType == DistrictType.Industrial || d.districtType == DistrictType.FreightTerminal).OrderByDescending(d => d.attractionScore).Take(2);
                foreach (var district in industrial)
                {
                    analysis.bestIndustrialAreas.Add(new CityOpportunityAreaData
                    {
                        name = $"{city.name} Industrial Area: {district.name}",
                        position = district.bounds.center,
                        recommendedUse = "Logistics / Industrial Park",
                        priorityScore = district.attractionScore * 100f,
                        rationale = "Edge-oriented zoning with freight connectivity.",
                        radiusMeters = Mathf.Max(140f, district.bounds.width * 0.36f)
                    });
                }

                var expansionCount = 3;
                for (var i = 0; i < expansionCount; i++)
                {
                    var angle = 2f * Mathf.PI * i / expansionCount + 0.22f;
                    var position = city.center + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * city.futureExpansionRadiusMeters;
                    analysis.futureExpansionAreas.Add(new CityOpportunityAreaData
                    {
                        name = $"{city.name} Future Expansion {i + 1}",
                        position = position,
                        recommendedUse = "Growth Zone",
                        priorityScore = 72f - i * 6f,
                        rationale = "Reserved expansion corridor outside current urban edge.",
                        radiusMeters = city.futureExpansionRadiusMeters * 0.18f
                    });
                }

                var towerAreas = city.landValueMap.Where(s => s.valueScore > 70f).Take(3);
                foreach (var sample in towerAreas)
                {
                    analysis.bestTowerAreas.Add(new CityOpportunityAreaData
                    {
                        name = $"{city.name} Tower Area: {sample.label}",
                        position = sample.position,
                        recommendedUse = "Mid/High-Rise Development",
                        priorityScore = sample.valueScore,
                        rationale = "High land value close to core amenities.",
                        radiusMeters = city.currentRadiusMeters * 0.14f
                    });
                }
            }

            return analysis;
        }

        private TerrainZoneData GetNearestTerrainZone(Vector2 position, TerrainZoneType zoneType)
        {
            if (masterPlan.terrainAnalysis?.zones == null)
                return null;
            TerrainZoneData closest = null;
            var bestDistance = float.MaxValue;
            foreach (var zone in masterPlan.terrainAnalysis.zones)
            {
                if (zone.type != zoneType) continue;
                var distance = Vector2.Distance(position, zone.center);
                if (distance < bestDistance)
                {
                    bestDistance = distance;
                    closest = zone;
                }
            }
            return closest;
        }

        private StreetSegmentPlan CreateStreetSegment(string name, Vector2 from, Vector2 to, float width, string roadClass)
        {
            return new StreetSegmentPlan
            {
                name = name,
                from = from,
                to = to,
                lengthMeters = Vector2.Distance(from, to),
                widthMeters = width,
                roadClass = roadClass
            };
        }

        private Vector2 FindClosestPointOnLine(Vector2 a, Vector2 b, Vector2 point)
        {
            var ab = b - a;
            var t = Vector2.Dot(point - a, ab) / Mathf.Max(ab.sqrMagnitude, 0.0001f);
            t = Mathf.Clamp01(t);
            return a + ab * t;
        }
    }
}
