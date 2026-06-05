using System;
using System.Collections.Generic;
using UnityEngine;
using System.IO;

namespace ZZCityGen.Planning.TerrainGeneration
{
    /// <summary>
    /// نظام حفظ وتحميل بيانات التضاريس من JSON
    /// </summary>
    public static class TerrainSaveLoadUtility
    {
        private const string TERRAIN_DATA_FILENAME = "TerrainData.json";
        private const string TERRAIN_ANALYSIS_FILENAME = "TerrainAnalysis.json";

        /// <summary>
        /// حفظ بيانات التضاريس إلى JSON
        /// </summary>
        public static bool SaveTerrainData(TerrainGenerationData terrainData, string directory)
        {
            try
            {
                if (!Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                string path = Path.Combine(directory, TERRAIN_DATA_FILENAME);
                string json = JsonUtility.ToJson(terrainData, prettyPrint: true);
                File.WriteAllText(path, json);

                Debug.Log($"[TerrainSaveLoad] Terrain data saved to: {path}");
                Debug.Log($"[TerrainSaveLoad] File size: {new FileInfo(path).Length} bytes");

                return true;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[TerrainSaveLoad] Failed to save terrain data: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// تحميل بيانات التضاريس من JSON
        /// </summary>
        public static TerrainGenerationData LoadTerrainData(string filePath)
        {
            try
            {
                if (!File.Exists(filePath))
                {
                    Debug.LogError($"[TerrainSaveLoad] File not found: {filePath}");
                    return null;
                }

                string json = File.ReadAllText(filePath);
                var terrainData = JsonUtility.FromJson<TerrainGenerationData>(json);

                Debug.Log($"[TerrainSaveLoad] Terrain data loaded from: {filePath}");
                Debug.LogFormat("[TerrainSaveLoad] Loaded data with {0} rivers, {1} lakes, {2} forests",
                    terrainData.rivers.Count,
                    terrainData.lakes.Count,
                    terrainData.forests.Count
                );

                return terrainData;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[TerrainSaveLoad] Failed to load terrain data: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// حفظ تحليل التضاريس الإضافي
        /// </summary>
        public static bool SaveTerrainAnalysis(TerrainAnalysisData analysisData, string directory)
        {
            try
            {
                if (!Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                string path = Path.Combine(directory, TERRAIN_ANALYSIS_FILENAME);
                string json = JsonUtility.ToJson(analysisData, prettyPrint: true);
                File.WriteAllText(path, json);

                Debug.Log($"[TerrainSaveLoad] Terrain analysis saved to: {path}");

                return true;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[TerrainSaveLoad] Failed to save terrain analysis: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// تحميل تحليل التضاريس
        /// </summary>
        public static TerrainAnalysisData LoadTerrainAnalysis(string filePath)
        {
            try
            {
                if (!File.Exists(filePath))
                {
                    Debug.LogError($"[TerrainSaveLoad] File not found: {filePath}");
                    return null;
                }

                string json = File.ReadAllText(filePath);
                var analysisData = JsonUtility.FromJson<TerrainAnalysisData>(json);

                Debug.Log($"[TerrainSaveLoad] Terrain analysis loaded from: {filePath}");

                return analysisData;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[TerrainSaveLoad] Failed to load terrain analysis: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// الحصول على مسار ملف TerrainData في المشروع
        /// </summary>
        public static string GetTerrainDataPath()
        {
            return Path.Combine(Application.persistentDataPath, TERRAIN_DATA_FILENAME);
        }

        /// <summary>
        /// الحصول على مسار ملف TerrainAnalysis في المشروع
        /// </summary>
        public static string GetTerrainAnalysisPath()
        {
            return Path.Combine(Application.persistentDataPath, TERRAIN_ANALYSIS_FILENAME);
        }
    }
}
