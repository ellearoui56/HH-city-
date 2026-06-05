using System;
using System.Collections.Generic;
using UnityEngine;

namespace ZZCityGen.Planning.TerrainGeneration.RoadNetworkGeneration
{
    [Serializable]
    public class LogisticsAreaData
    {
        public Vector2 center;
        public float radiusMeters;
        public float score;
        public string label;
        public string reason;

        public LogisticsAreaData() { }

        public LogisticsAreaData(Vector2 center, float radius, float score, string label, string reason)
        {
            this.center = center;
            this.radiusMeters = radius;
            this.score = score;
            this.label = label;
            this.reason = reason;
        }
    }

    [Serializable]
    public class ExpansionCorridorData
    {
        public string name;
        public List<Vector2> pathPoints = new List<Vector2>();
        public float corridorScore;
        public string corridorType;

        public ExpansionCorridorData() { }

        public ExpansionCorridorData(string name, List<Vector2> pathPoints, float corridorScore, string corridorType)
        {
            this.name = name;
            this.pathPoints = pathPoints;
            this.corridorScore = corridorScore;
            this.corridorType = corridorType;
        }
    }

    [Serializable]
    public class CongestionZoneData
    {
        public Vector2 center;
        public float radiusMeters;
        public float riskLevel;
        public string cause;

        public CongestionZoneData() { }

        public CongestionZoneData(Vector2 center, float radius, float riskLevel, string cause)
        {
            this.center = center;
            this.radiusMeters = radius;
            this.riskLevel = riskLevel;
            this.cause = cause;
        }
    }

    [Serializable]
    public class HighValueLandData
    {
        public Vector2 center;
        public float score;
        public string landUseType;

        public HighValueLandData() { }

        public HighValueLandData(Vector2 center, float score, string landUseType)
        {
            this.center = center;
            this.score = score;
            this.landUseType = landUseType;
        }
    }

    [Serializable]
    public class FreightCorridorData
    {
        public string name;
        public Vector2 from;
        public Vector2 to;
        public float capacityTonsPerDay;
        public float projectedFreightDemand;
        public string priorityTier;

        public FreightCorridorData() { }

        public FreightCorridorData(string name, Vector2 from, Vector2 to, float capacity, float demand, string priority)
        {
            this.name = name;
            this.from = from;
            this.to = to;
            this.capacityTonsPerDay = capacity;
            this.projectedFreightDemand = demand;
            this.priorityTier = priority;
        }
    }

    [Serializable]
    public class BackupRailRouteData
    {
        public string name;
        public Vector2 from;
        public Vector2 to;
        public float routeLengthMeters;
        public float redundancyScore;
        public string reason;

        public BackupRailRouteData() { }

        public BackupRailRouteData(string name, Vector2 from, Vector2 to, float length, float score, string reason)
        {
            this.name = name;
            this.from = from;
            this.to = to;
            this.routeLengthMeters = length;
            this.redundancyScore = score;
            this.reason = reason;
        }
    }

    [Serializable]
    public class FutureTrafficProjectionData
    {
        public int year;
        public float projectedTotalTraffic;
        public float projectedFreightTraffic;
        public float congestionRisk;

        public FutureTrafficProjectionData() { }

        public FutureTrafficProjectionData(int year, float projectedTotalTraffic, float projectedFreightTraffic, float congestionRisk)
        {
            this.year = year;
            this.projectedTotalTraffic = projectedTotalTraffic;
            this.projectedFreightTraffic = projectedFreightTraffic;
            this.congestionRisk = congestionRisk;
        }
    }

    [Serializable]
    public class TransportDemandSummary
    {
        public string connectionName;
        public float dailyTrafficEstimate;
        public float futureTrafficEstimate;
        public string demandCategory;

        public TransportDemandSummary() { }

        public TransportDemandSummary(string connectionName, float dailyTrafficEstimate, float futureTrafficEstimate, string demandCategory)
        {
            this.connectionName = connectionName;
            this.dailyTrafficEstimate = dailyTrafficEstimate;
            this.futureTrafficEstimate = futureTrafficEstimate;
            this.demandCategory = demandCategory;
        }
    }

    [Serializable]
    public class TransportationAnalysisData
    {
        public float worldConnectivityScore;
        public float averageTravelTimeMinutes;
        public float futureTrafficGrowthFactor;
        public List<LogisticsAreaData> bestLogisticsAreas = new List<LogisticsAreaData>();
        public List<HighValueLandData> bestCommercialAreas = new List<HighValueLandData>();
        public List<HighValueLandData> bestResidentialAreas = new List<HighValueLandData>();
        public List<ExpansionCorridorData> expansionCorridors = new List<ExpansionCorridorData>();
        public List<CongestionZoneData> futureCongestionZones = new List<CongestionZoneData>();
        public List<FreightCorridorData> freightCorridors = new List<FreightCorridorData>();
        public List<BackupRailRouteData> backupRailRoutes = new List<BackupRailRouteData>();
        public List<FutureTrafficProjectionData> futureTrafficProjections = new List<FutureTrafficProjectionData>();
        public List<TransportDemandSummary> demandSummaries = new List<TransportDemandSummary>();

        public TransportationAnalysisData()
        {
            bestLogisticsAreas = new List<LogisticsAreaData>();
            bestCommercialAreas = new List<HighValueLandData>();
            bestResidentialAreas = new List<HighValueLandData>();
            expansionCorridors = new List<ExpansionCorridorData>();
            futureCongestionZones = new List<CongestionZoneData>();
            freightCorridors = new List<FreightCorridorData>();
            backupRailRoutes = new List<BackupRailRouteData>();
            futureTrafficProjections = new List<FutureTrafficProjectionData>();
            demandSummaries = new List<TransportDemandSummary>();
        }
    }
}
