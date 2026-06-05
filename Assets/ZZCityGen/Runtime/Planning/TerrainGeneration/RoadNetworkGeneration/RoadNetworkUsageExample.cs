using System;
using System.IO;
using UnityEngine;
using ZZCityGen.Planning.MasterPlanning;
using ZZCityGen.Planning.TerrainGeneration;

namespace ZZCityGen.Planning.TerrainGeneration.RoadNetworkGeneration
{
    public static class RoadNetworkUsageExample
    {
        public static void Example1_GenerateTransportBlueprint()
        {
            Debug.Log("========== EXAMPLE 1: Transport Blueprint Generation ==========");

            string masterPlanPath = Path.Combine(Application.persistentDataPath, "MasterPlanData.json");
            var masterPlan = MasterPlanSaveLoadUtility.LoadMasterPlanData(masterPlanPath);

            if (masterPlan == null)
            {
                Debug.LogError("[RoadNetworkUsageExample] Cannot generate road network because MasterPlanData is missing.");
                return;
            }

            string terrainPath = TerrainSaveLoadUtility.GetTerrainDataPath();
            var terrainData = TerrainSaveLoadUtility.LoadTerrainData(terrainPath);

            if (terrainData == null)
            {
                Debug.LogError("[RoadNetworkUsageExample] Cannot generate road network because TerrainData is missing.");
                return;
            }

            string terrainAnalysisPath = TerrainSaveLoadUtility.GetTerrainAnalysisPath();
            var terrainAnalysis = TerrainSaveLoadUtility.LoadTerrainAnalysis(terrainAnalysisPath);

            if (terrainAnalysis == null)
            {
                Debug.LogWarning("[RoadNetworkUsageExample] Terrain analysis file not found. The route planner will use heuristic terrain penalties.");
            }

            var roadBuilder = new RoadNetworkBuilder(masterPlan, terrainData, terrainAnalysis);
            var transportAnalysis = new TransportationAnalysisData();
            var networkPlan = roadBuilder.BuildRoadNetwork(out transportAnalysis, terrainAnalysis);

            if (networkPlan == null)
            {
                Debug.LogError("[RoadNetworkUsageExample] Road network generation failed.");
                return;
            }

            if (RoadNetworkSaveLoadUtility.SaveRoadNetwork(networkPlan, Application.persistentDataPath))
            {
                Debug.Log($"[RoadNetworkUsageExample] Road network saved to {RoadNetworkSaveLoadUtility.GetRoadNetworkPath()}");
            }

            if (RoadNetworkSaveLoadUtility.SaveTransportationAnalysis(transportAnalysis, Application.persistentDataPath))
            {
                Debug.Log($"[RoadNetworkUsageExample] Transportation analysis saved to {RoadNetworkSaveLoadUtility.GetTransportationAnalysisPath()}");
            }

            if (MasterPlanSaveLoadUtility.SaveMasterPlanData(masterPlan, Application.persistentDataPath))
            {
                Debug.Log($"[RoadNetworkUsageExample] Master plan updated with transport network and saved to {MasterPlanSaveLoadUtility.GetMasterPlanDataPath()}");
            }

            Debug.LogFormat("[RoadNetworkUsageExample] ✓ Generated {0} highways, {1} railways, {2} freight corridors, {3} bridges, {4} tunnels.",
                networkPlan.Highways.Count,
                networkPlan.Railways.Count,
                networkPlan.FreightCorridors.Count,
                networkPlan.Bridges.Count,
                networkPlan.Tunnels.Count
            );
        }
    }
}
