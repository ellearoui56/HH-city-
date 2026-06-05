using System.IO;
using UnityEngine;
using ZZCityGen.Planning.CityGeneration;
using ZZCityGen.Planning.MasterPlanning;
using ZZCityGen.Planning.TerrainGeneration.RoadNetworkGeneration;

namespace ZZCityGen.Planning.CityGeneration
{
    public class CityGenerationUsageExample : MonoBehaviour
    {
        public void GenerateFromSavedMasterPlan()
        {
            string masterPlanPath = Path.Combine(Application.persistentDataPath, "MasterPlanData.json");
            var masterPlan = MasterPlanSaveLoadUtility.LoadMasterPlanData(masterPlanPath);
            if (masterPlan == null)
            {
                Debug.LogError("[CityGenerationUsageExample] MasterPlanData.json not found or invalid.");
                return;
            }

            string roadNetworkPath = Path.Combine(Application.persistentDataPath, "RoadNetwork.json");
            var roadNetwork = RoadNetworkSaveLoadUtility.LoadRoadNetwork(roadNetworkPath);
            string transportationAnalysisPath = Path.Combine(Application.persistentDataPath, "TransportationAnalysis.json");
            var transportAnalysis = RoadNetworkSaveLoadUtility.LoadTransportationAnalysis(transportationAnalysisPath);

            var builder = new CityGenerationBuilder(masterPlan, roadNetwork, transportAnalysis);
            var package = builder.BuildCityLayouts(out var urbanAnalysis);

            if (CityGenerationSaveLoadUtility.SaveCityData(package, Application.persistentDataPath))
            {
                Debug.Log($"[CityGenerationUsageExample] CityData saved at {CityGenerationSaveLoadUtility.GetCityDataPath()}");
            }

            if (CityGenerationSaveLoadUtility.SaveUrbanAnalysis(urbanAnalysis, Application.persistentDataPath))
            {
                Debug.Log($"[CityGenerationUsageExample] UrbanAnalysis saved at {CityGenerationSaveLoadUtility.GetUrbanAnalysisPath()}");
            }

            Debug.Log($"[CityGenerationUsageExample] Generated {package.cities.Count} city layouts.");
        }
    }
}
