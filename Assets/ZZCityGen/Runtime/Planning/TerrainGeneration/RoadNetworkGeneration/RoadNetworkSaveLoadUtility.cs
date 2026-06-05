using System;
using System.IO;
using UnityEngine;
using ZZCityGen.Data;

namespace ZZCityGen.Planning.TerrainGeneration.RoadNetworkGeneration
{
    public static class RoadNetworkSaveLoadUtility
    {
        private const string ROAD_NETWORK_FILENAME = "RoadNetwork.json";
        private const string TRANSPORTATION_ANALYSIS_FILENAME = "TransportationAnalysis.json";

        public static string GetRoadNetworkPath()
        {
            return Path.Combine(Application.persistentDataPath, ROAD_NETWORK_FILENAME);
        }

        public static string GetTransportationAnalysisPath()
        {
            return Path.Combine(Application.persistentDataPath, TRANSPORTATION_ANALYSIS_FILENAME);
        }

        public static bool SaveRoadNetwork(RoadNetworkPlan networkPlan, string directory)
        {
            try
            {
                if (!Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                string path = Path.Combine(directory, ROAD_NETWORK_FILENAME);
                string json = JsonUtility.ToJson(networkPlan, prettyPrint: true);
                File.WriteAllText(path, json);

                Debug.Log($"[RoadNetworkSaveLoad] Saved RoadNetwork to: {path}");
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[RoadNetworkSaveLoad] Failed to save road network: {ex.Message}");
                return false;
            }
        }

        public static bool SaveTransportationAnalysis(TransportationAnalysisData analysisData, string directory)
        {
            try
            {
                if (!Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                string path = Path.Combine(directory, TRANSPORTATION_ANALYSIS_FILENAME);
                string json = JsonUtility.ToJson(analysisData, prettyPrint: true);
                File.WriteAllText(path, json);

                Debug.Log($"[RoadNetworkSaveLoad] Saved TransportationAnalysis to: {path}");
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[RoadNetworkSaveLoad] Failed to save transportation analysis: {ex.Message}");
                return false;
            }
        }

        public static TransportationAnalysisData LoadTransportationAnalysis(string filePath)
        {
            try
            {
                if (string.IsNullOrEmpty(filePath))
                {
                    Debug.LogError("[RoadNetworkSaveLoad] Transportation analysis path was empty.");
                    return null;
                }

                if (!File.Exists(filePath))
                {
                    Debug.LogError($"[RoadNetworkSaveLoad] Transportation analysis file not found: {filePath}");
                    return null;
                }

                string json = File.ReadAllText(filePath);
                var analysis = JsonUtility.FromJson<TransportationAnalysisData>(json);

                Debug.Log($"[RoadNetworkSaveLoad] Loaded TransportationAnalysis from: {filePath}");
                return analysis;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[RoadNetworkSaveLoad] Failed to load transportation analysis: {ex.Message}");
                return null;
            }
        }

        public static RoadNetworkPlan LoadRoadNetwork(string filePath)
        {
            try
            {
                if (string.IsNullOrEmpty(filePath))
                {
                    Debug.LogError("[RoadNetworkSaveLoad] Road network path was empty.");
                    return null;
                }

                if (!File.Exists(filePath))
                {
                    Debug.LogError($"[RoadNetworkSaveLoad] Road network file not found: {filePath}");
                    return null;
                }

                string json = File.ReadAllText(filePath);
                var plan = JsonUtility.FromJson<RoadNetworkPlan>(json);
                Debug.Log($"[RoadNetworkSaveLoad] Loaded RoadNetwork from: {filePath}");
                return plan;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[RoadNetworkSaveLoad] Failed to load road network: {ex.Message}");
                return null;
            }
        }
    }
}
