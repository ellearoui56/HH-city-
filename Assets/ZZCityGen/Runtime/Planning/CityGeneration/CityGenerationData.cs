using System;
using System.Collections.Generic;
using UnityEngine;
using ZZCityGen.Data;
using ZZCityGen.Planning.MasterPlanning;

namespace ZZCityGen.Planning.CityGeneration
{
    [Serializable]
    public sealed class CityGenerationPackage
    {
        public string worldName;
        public int seed;
        public long generatedTimestamp;
        public List<CityLayoutData> cities = new List<CityLayoutData>();

        public CityGenerationPackage()
        {
            cities = new List<CityLayoutData>();
            generatedTimestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        }
    }

    [Serializable]
    public sealed class CityLayoutData
    {
        public string name;
        public CityArchetype archetype;
        public EconomicIdentity economicIdentity;
        public string identityDescription;
        public Vector2 position;
        public Vector2 center;
        public string centerType;
        public float cityScore;
        public float cityQualityScore;
        public float currentRadiusMeters;
        public float futureExpansionRadiusMeters;
        public List<Vector2> boundaryPoints = new List<Vector2>();
        public List<DistrictLayoutData> districts = new List<DistrictLayoutData>();
        public List<StreetSegmentPlan> mainStreets = new List<StreetSegmentPlan>();
        public List<StreetSegmentPlan> ringRoads = new List<StreetSegmentPlan>();
        public List<CityServiceSiteData> serviceSites = new List<CityServiceSiteData>();
        public List<ParkReserveData> parks = new List<ParkReserveData>();
        public List<LandValueSample> landValueMap = new List<LandValueSample>();
        public List<GrowthZoneData> growthZones = new List<GrowthZoneData>();
        public string regenerationRecommendation;

        public CityLayoutData()
        {
            boundaryPoints = new List<Vector2>();
            districts = new List<DistrictLayoutData>();
            mainStreets = new List<StreetSegmentPlan>();
            ringRoads = new List<StreetSegmentPlan>();
            serviceSites = new List<CityServiceSiteData>();
            parks = new List<ParkReserveData>();
            landValueMap = new List<LandValueSample>();
            growthZones = new List<GrowthZoneData>();
        }
    }

    [Serializable]
    public sealed class DistrictLayoutData
    {
        public string name;
        public DistrictType districtType;
        public Rect bounds;
        public float densityScore;
        public int populationTarget;
        public int jobsTarget;
        public float areaSquareMeters;
        public float attractionScore;
    }

    [Serializable]
    public sealed class GrowthZoneData
    {
        public string name;
        public string zoneType;
        public List<Vector2> boundaryPoints = new List<Vector2>();
        public float score;
    }

    [Serializable]
    public sealed class CityServiceSiteData
    {
        public string name;
        public string serviceType;
        public Vector2 position;
        public float serviceRadiusMeters;
        public float importanceScore;
    }

    [Serializable]
    public sealed class ParkReserveData
    {
        public string name;
        public Vector2 center;
        public float radiusMeters;
        public string parkType;
        public float serenityScore;
    }

    [Serializable]
    public sealed class LandValueSample
    {
        public string label;
        public Vector2 position;
        public float valueScore;
        public float distanceFromCenter;
        public DistrictType preferredDistrict;
        public string reason;
    }

    [Serializable]
    public sealed class CityQualityMetrics
    {
        public float walkabilityScore;
        public float accessibilityScore;
        public float growthPotentialScore;
        public float trafficEfficiencyScore;
        public float overallQualityScore;
    }

    [Serializable]
    public sealed class CityOpportunityAreaData
    {
        public string name;
        public Vector2 position;
        public string recommendedUse;
        public float priorityScore;
        public string rationale;
        public float radiusMeters;
    }

    [Serializable]
    public sealed class UrbanAnalysisData
    {
        public string worldName;
        public int seed;
        public long generatedTimestamp;
        public List<CityOpportunityAreaData> bestTowerAreas = new List<CityOpportunityAreaData>();
        public List<CityOpportunityAreaData> bestHousingAreas = new List<CityOpportunityAreaData>();
        public List<CityOpportunityAreaData> bestIndustrialAreas = new List<CityOpportunityAreaData>();
        public List<CityOpportunityAreaData> futureExpansionAreas = new List<CityOpportunityAreaData>();
        public List<CityOpportunityAreaData> premiumLandZones = new List<CityOpportunityAreaData>();

        public UrbanAnalysisData()
        {
            bestTowerAreas = new List<CityOpportunityAreaData>();
            bestHousingAreas = new List<CityOpportunityAreaData>();
            bestIndustrialAreas = new List<CityOpportunityAreaData>();
            futureExpansionAreas = new List<CityOpportunityAreaData>();
            premiumLandZones = new List<CityOpportunityAreaData>();
            generatedTimestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        }
    }
}
