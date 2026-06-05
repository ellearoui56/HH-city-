using System;
using System.IO;
using UnityEngine;

namespace ZZCityGen.Planning.MasterPlanning
{
    public static class MasterPlanSaveLoadUtility
    {
        private const string MASTER_PLAN_FILE_NAME = "MasterPlanData.json";

        public static string GetMasterPlanDataPath()
        {
            return Path.Combine(Application.persistentDataPath, MASTER_PLAN_FILE_NAME);
        }

        public static bool SaveMasterPlanData(MasterPlanData masterPlan, string directory)
        {
            try
            {
                if (!Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                string path = Path.Combine(directory, MASTER_PLAN_FILE_NAME);
                string json = JsonUtility.ToJson(masterPlan, prettyPrint: true);
                File.WriteAllText(path, json);

                Debug.Log($"[MasterPlanSaveLoad] Saved MasterPlanData to: {path}");
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[MasterPlanSaveLoad] Failed to save MasterPlanData: {ex.Message}");
                return false;
            }
        }

        public static MasterPlanData LoadMasterPlanData(string filePath)
        {
            try
            {
                if (string.IsNullOrEmpty(filePath))
                {
                    Debug.LogError("[MasterPlanSaveLoad] Master plan path was empty.");
                    return null;
                }

                if (!File.Exists(filePath))
                {
                    Debug.LogError($"[MasterPlanSaveLoad] Master plan file not found: {filePath}");
                    return null;
                }

                string json = File.ReadAllText(filePath);
                var masterPlan = JsonUtility.FromJson<MasterPlanData>(json);
                Debug.Log($"[MasterPlanSaveLoad] Loaded MasterPlanData from: {filePath}");
                return masterPlan;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[MasterPlanSaveLoad] Failed to load MasterPlanData: {ex.Message}");
                return null;
            }
        }
    }
}
