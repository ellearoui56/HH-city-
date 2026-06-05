using UnityEditor;
using UnityEngine;

namespace ZZCityGen.Editor
{
    [CustomEditor(typeof(Real3DAssetImportReport))]
    public sealed class Real3DAssetImportReportEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            var report = (Real3DAssetImportReport)target;
            EditorGUILayout.LabelField("Real 3D Asset Import Report", EditorStyles.boldLabel);
            EditorGUILayout.LabelField("Imported At", report.importedAtUtc);
            EditorGUILayout.LabelField("Source Folder", report.sourceFolder);
            EditorGUILayout.LabelField("Models", report.totalModelCount.ToString());
            EditorGUILayout.LabelField("Prefabs", report.totalPrefabCount.ToString());

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Packs", EditorStyles.boldLabel);
            foreach (var pack in report.packs)
            {
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                EditorGUILayout.LabelField(pack.displayName, EditorStyles.boldLabel);
                EditorGUILayout.LabelField("Status", pack.status);
                EditorGUILayout.LabelField("Archive", string.IsNullOrEmpty(pack.archivePath) ? "Not resolved" : pack.archivePath);
                EditorGUILayout.LabelField("Models / Prefabs", $"{pack.modelCount} / {pack.prefabCount}");
                if (!string.IsNullOrEmpty(pack.warning))
                {
                    EditorGUILayout.HelpBox(pack.warning, MessageType.Warning);
                }
                EditorGUILayout.EndVertical();
            }

            EditorGUILayout.Space();
            if (GUILayout.Button("Open Real 3D Asset Dashboard"))
            {
                Real3DAssetDashboardWindow.Open();
            }

            if (GUILayout.Button("Export CSV"))
            {
                Real3DAssetPackImporter.ExportImportReportCsv();
            }

            if (GUILayout.Button("Reveal Real Asset Folder"))
            {
                Real3DAssetPackImporter.RevealRealAssetFolder();
            }
        }
    }
}
