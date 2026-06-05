using System;
using System.IO;
using UnityEngine;

namespace ZZCityGen.Planning.CityGeneration
{
    public static class CityGenerationSaveLoadUtility
    {
        private const string CITY_DATA_FILENAME = "CityData.json";
        private const string URBAN_ANALYSIS_FILENAME = "UrbanAnalysis.json";

        public static string GetCityDataPath()
        {
            return Path.Combine(Application.persistentDataPath, CITY_DATA_FILENAME);
        }

        public static string GetUrbanAnalysisPath()
        {
            return Path.Combine(Application.persistentDataPath, URBAN_ANALYSIS_FILENAME);
        }

        public static bool SaveCityData(CityGenerationPackage cityData, string directory)
        {
            try
            {
                if (!Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                var path = Path.Combine(directory, CITY_DATA_FILENAME);
                var json = JsonUtility.ToJson(cityData, prettyPrint: true);
                File.WriteAllText(path, json);
                Debug.Log($"[CityGenerationSaveLoad] Saved CityData to: {path}");
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[CityGenerationSaveLoad] Failed to save CityData: {ex.Message}");
                return false;
            }
        }

        public static bool SaveUrbanAnalysis(UrbanAnalysisData analysis, string directory)
        {
            try
            {
                if (!Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                var path = Path.Combine(directory, URBAN_ANALYSIS_FILENAME);
                var json = JsonUtility.ToJson(analysis, prettyPrint: true);
                File.WriteAllText(path, json);
                Debug.Log($"[CityGenerationSaveLoad] Saved UrbanAnalysis to: {path}");
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[CityGenerationSaveLoad] Failed to save UrbanAnalysis: {ex.Message}");
                return false;
            }
        }
    }
}
