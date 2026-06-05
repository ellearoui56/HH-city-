using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;
using UnityEngine.Networking;
using ZZCityGen.Data;
using ZZCityGen.Generation;

namespace ZZCityGen.Editor
{
    public static class Real3DAssetPackImporter
    {
        private const string RootFolder = "Assets/ZZCityGen/Real3DAssetPacks";
        private const string ArchiveFolder = RootFolder + "/Archives";
        private const string ExtractedFolder = RootFolder + "/Extracted";
        private const string PrefabFolder = RootFolder + "/Prefabs";
        private const string DatabaseFolder = RootFolder + "/Databases";
        private const string LicenseFolder = RootFolder + "/Licenses";
        private const string ReportFolder = RootFolder + "/Reports";
        private const string PrefabDatabasePath = DatabaseFolder + "/ZZCityGenRealPrefabDatabase.asset";
        private const string AssetCatalogPath = DatabaseFolder + "/ZZCityGenRealAssetCatalog.asset";
        private const string ImportReportPath = ReportFolder + "/ZZCityGenRealAssetImportReport.asset";
        private const int MaxPrefabsPerPack = 160;

        public static string RealAssetRootFolder => RootFolder;
        public static string RealAssetArchiveFolder => ArchiveFolder;
        public static string RealAssetPrefabFolder => PrefabFolder;
        public static string RealAssetPrefabDatabasePath => PrefabDatabasePath;
        public static string RealAssetCatalogPath => AssetCatalogPath;
        public static string RealAssetImportReportPath => ImportReportPath;

        private static readonly RealAssetPack[] Packs =
        {
            new RealAssetPack("kenney_city_kit_commercial", "City Kit (Commercial)", "Kenney", "https://www.kenney.nl/assets/city-kit-commercial", "Creative Commons CC0", PrefabCategory.Commercial, 100,
                DistrictType.Business, DistrictType.Commercial, DistrictType.Downtown, DistrictType.Tourism),
            new RealAssetPack("kenney_city_kit_suburban", "City Kit (Suburban)", "Kenney", "https://www.kenney.nl/assets/city-kit-suburban", "Creative Commons CC0", PrefabCategory.Residential, 88,
                DistrictType.Residential, DistrictType.MiddleResidential, DistrictType.PopularResidential, DistrictType.LuxuryResidential),
            new RealAssetPack("kenney_city_kit_industrial", "City Kit (Industrial)", "Kenney", "https://www.kenney.nl/assets/city-kit-industrial", "Creative Commons CC0", PrefabCategory.Industrial, 86,
                DistrictType.Industrial, DistrictType.FreightTerminal, DistrictType.Port),
            new RealAssetPack("kenney_city_kit_roads", "City Kit (Roads)", "Kenney", "https://www.kenney.nl/assets/city-kit-roads", "Creative Commons CC0", PrefabCategory.Transit, 74,
                DistrictType.Airport, DistrictType.Port, DistrictType.FreightTerminal, DistrictType.Utility),
            new RealAssetPack("kenney_nature_kit", "Nature Kit", "Kenney", "https://www.kenney.nl/assets/nature-kit", "Creative Commons CC0", PrefabCategory.Park, 70,
                DistrictType.PublicPark, DistrictType.Park, DistrictType.Tourism)
        };

        [MenuItem("Tools/ZZ CityGen/Assets/Import Real CC0 3D City Packs")]
        public static void ImportRealCc0CityPacks()
        {
            EnsureFolders();
            WriteLicenseManifest();

            var report = CreateReport();
            var createdPrefabs = new List<PrefabRecord>();
            try
            {
                var importedModels = new List<ImportedModel>();
                for (var i = 0; i < Packs.Length; i++)
                {
                    var pack = Packs[i];
                    var packReport = AddPackReport(report, pack);
                    EditorUtility.DisplayProgressBar("Importing real 3D assets", $"Preparing {pack.DisplayName}", i / (float)Packs.Length);

                    var archivePath = ResolveArchive(pack, packReport);
                    if (string.IsNullOrEmpty(archivePath))
                    {
                        packReport.status = "Missing archive";
                        packReport.warning = $"Download {pack.DisplayName} from {pack.SourceUrl}, put the ZIP in {ArchiveFolder}, then rerun the importer.";
                        Debug.LogWarning(packReport.warning);
                        continue;
                    }

                    packReport.archivePath = ToAssetPath(archivePath);
                    var extractedPath = ExtractArchive(pack, archivePath);
                    packReport.extractedPath = extractedPath;
                    AssetDatabase.Refresh();

                    var packModels = FindModelAssets(pack, extractedPath);
                    packReport.modelCount = packModels.Count;
                    packReport.status = packModels.Count > 0 ? "Models found" : "No supported models found";
                    importedModels.AddRange(packModels);
                }

                AssetDatabase.Refresh();
                createdPrefabs = CreatePrefabWrappers(importedModels);
                CreateDatabases(createdPrefabs);
                FillPrefabReport(report, createdPrefabs);
                SaveImportReport(report);
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }

            Debug.Log($"ZZ CityGen imported {createdPrefabs.Count} real 3D model prefabs from CC0 source packs into {RootFolder}. Report: {ImportReportPath}");
        }

        [MenuItem("Tools/ZZ CityGen/Assets/Copy Selected ZIPs To Real Asset Archives")]
        public static void CopySelectedZipsToArchiveFolder()
        {
            EnsureFolders();
            var copied = 0;
            foreach (var selectedObject in Selection.objects)
            {
                var sourceAssetPath = AssetDatabase.GetAssetPath(selectedObject);
                if (string.IsNullOrEmpty(sourceAssetPath) || !sourceAssetPath.EndsWith(".zip", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                var sourceFullPath = ToFullPath(sourceAssetPath);
                var targetFullPath = Path.Combine(ToFullPath(ArchiveFolder), Path.GetFileName(sourceFullPath));
                if (string.Equals(Path.GetFullPath(sourceFullPath), Path.GetFullPath(targetFullPath), StringComparison.OrdinalIgnoreCase))
                {
                    copied++;
                    continue;
                }

                File.Copy(sourceFullPath, targetFullPath, true);
                copied++;
            }

            AssetDatabase.Refresh();
            Debug.Log($"Copied {copied} selected ZIP archive(s) into {ArchiveFolder}. Use filenames that contain the pack name, such as city-kit-commercial.zip or nature-kit.zip.");
        }

        [MenuItem("Tools/ZZ CityGen/Assets/Validate Real 3D Prefab Database")]
        public static void ValidateRealPrefabDatabase()
        {
            var database = AssetDatabase.LoadAssetAtPath<PrefabDatabase>(PrefabDatabasePath);
            if (database == null)
            {
                Debug.LogWarning($"No real prefab database found at {PrefabDatabasePath}. Import assets first.");
                return;
            }

            var duplicateIds = database.prefabs
                .Where(entry => entry != null && !string.IsNullOrWhiteSpace(entry.id))
                .GroupBy(entry => entry.id)
                .Where(group => group.Count() > 1)
                .Select(group => group.Key)
                .ToList();
            var missingPrefabs = database.prefabs.Count(entry => entry == null || entry.prefab == null);
            var invalidDimensions = database.prefabs.Count(entry => entry != null && (entry.footprintMeters.x <= 0f || entry.footprintMeters.y <= 0f || entry.heightMeters <= 0f));

            if (duplicateIds.Count == 0 && missingPrefabs == 0 && invalidDimensions == 0)
            {
                Debug.Log($"Real prefab database validation passed. Entries: {database.prefabs.Count}.");
                return;
            }

            Debug.LogWarning($"Real prefab database validation found issues. Entries: {database.prefabs.Count}, missing prefabs: {missingPrefabs}, invalid dimensions: {invalidDimensions}, duplicate ids: {string.Join(", ", duplicateIds)}");
        }

        [MenuItem("Tools/ZZ CityGen/Assets/Open Real 3D Asset Sources")]
        public static void OpenRealAssetSources()
        {
            foreach (var pack in Packs)
            {
                Application.OpenURL(pack.SourceUrl);
            }
        }

        [MenuItem("Tools/ZZ CityGen/Assets/Reveal Real 3D Asset Folder")]
        public static void RevealRealAssetFolder()
        {
            EnsureFolders();
            EditorUtility.RevealInFinder(ToFullPath(RootFolder));
        }

        [MenuItem("Tools/ZZ CityGen/Assets/Rebuild Real Databases From Existing Prefabs")]
        public static void RebuildRealDatabasesFromExistingPrefabs()
        {
            EnsureFolders();
            var records = new List<PrefabRecord>();
            var prefabGuids = AssetDatabase.FindAssets("t:Prefab", new[] { PrefabFolder });
            foreach (var guid in prefabGuids)
            {
                var prefabPath = AssetDatabase.GUIDToAssetPath(guid);
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
                if (prefab == null)
                {
                    continue;
                }

                var pack = InferPackFromPrefabName(prefab.name);
                var bounds = CalculatePrefabAssetBounds(prefabPath);
                records.Add(new PrefabRecord(pack, prefab, prefab.name, "Existing generated prefab", bounds));
            }

            CreateDatabases(records);
            var report = CreateReport();
            foreach (var pack in Packs)
            {
                var packReport = AddPackReport(report, pack);
                packReport.status = "Rebuilt from existing prefabs";
                packReport.prefabCount = records.Count(record => record.Pack.Id == pack.Id);
                packReport.modelCount = packReport.prefabCount;
            }
            FillPrefabReport(report, records);
            SaveImportReport(report);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"Rebuilt real asset databases from {records.Count} existing prefabs. Database: {PrefabDatabasePath}");
        }

        [MenuItem("Tools/ZZ CityGen/Assets/Assign Real Assets To Selected World Generators")]
        public static void AssignRealAssetsToSelectedWorldGenerators()
        {
            var prefabDatabase = AssetDatabase.LoadAssetAtPath<PrefabDatabase>(PrefabDatabasePath);
            var assetCatalog = AssetDatabase.LoadAssetAtPath<AssetCatalog>(AssetCatalogPath);
            if (prefabDatabase == null && assetCatalog == null)
            {
                Debug.LogWarning($"No generated real asset database or catalog was found. Import or rebuild real assets first: {DatabaseFolder}");
                return;
            }

            var generators = Selection.GetFiltered<WorldGenerator>(SelectionMode.Editable);
            if (generators.Length == 0)
            {
                generators = UnityEngine.Object.FindObjectsOfType<WorldGenerator>();
            }

            foreach (var generator in generators)
            {
                generator.SetAssetLibraries(prefabDatabase, assetCatalog);
                EditorUtility.SetDirty(generator);
            }

            Debug.Log($"Assigned real asset database/catalog to {generators.Length} WorldGenerator component(s).");
        }

        [MenuItem("Tools/ZZ CityGen/Assets/Export Real Asset Import Report CSV")]
        public static void ExportImportReportCsv()
        {
            EnsureFolders();
            var report = LoadImportReport();
            if (report == null)
            {
                Debug.LogWarning($"No real asset import report found at {ImportReportPath}. Import or rebuild real assets first.");
                return;
            }

            var csvPath = Path.Combine(ToFullPath(ReportFolder), "ZZCityGenRealAssetImportReport.csv");
            using (var writer = new StreamWriter(csvPath, false))
            {
                writer.WriteLine("id,packId,category,footprintX,footprintY,heightMeters,sourceModelPath,prefabPath");
                foreach (var prefab in report.prefabs)
                {
                    writer.WriteLine(string.Join(",",
                        Csv(prefab.id),
                        Csv(prefab.packId),
                        Csv(prefab.category.ToString()),
                        prefab.footprintMeters.x.ToString("0.###"),
                        prefab.footprintMeters.y.ToString("0.###"),
                        prefab.heightMeters.ToString("0.###"),
                        Csv(prefab.sourceModelPath),
                        Csv(prefab.prefabPath)));
                }
            }

            AssetDatabase.Refresh();
            Debug.Log($"Exported real asset import CSV to {ToAssetPath(csvPath)}.");
        }

        [MenuItem("Tools/ZZ CityGen/Assets/Clean Generated Real Asset Outputs")]
        public static void CleanGeneratedRealAssetOutputs()
        {
            if (!EditorUtility.DisplayDialog("Clean generated real asset outputs", "Delete generated extracted files, prefabs, databases, and reports? Archives and license sources will be kept.", "Delete Generated Outputs", "Cancel"))
            {
                return;
            }

            DeleteAssetFolderContents(ExtractedFolder);
            DeleteAssetFolderContents(PrefabFolder);
            DeleteAssetFolderContents(DatabaseFolder);
            DeleteAssetFolderContents(ReportFolder);
            AssetDatabase.Refresh();
            Debug.Log("Cleaned generated real asset outputs. Archives were kept.");
        }

        public static Real3DAssetImportReport LoadImportReport()
        {
            return AssetDatabase.LoadAssetAtPath<Real3DAssetImportReport>(ImportReportPath);
        }

        public static PrefabDatabase LoadRealPrefabDatabase()
        {
            return AssetDatabase.LoadAssetAtPath<PrefabDatabase>(PrefabDatabasePath);
        }

        public static AssetCatalog LoadRealAssetCatalog()
        {
            return AssetDatabase.LoadAssetAtPath<AssetCatalog>(AssetCatalogPath);
        }

        public static int CountLocalArchives()
        {
            var archiveDirectory = ToFullPath(ArchiveFolder);
            return Directory.Exists(archiveDirectory) ? Directory.GetFiles(archiveDirectory, "*.zip", SearchOption.TopDirectoryOnly).Length : 0;
        }

        public static int CountGeneratedPrefabs()
        {
            return AssetDatabase.IsValidFolder(PrefabFolder) ? AssetDatabase.FindAssets("t:Prefab", new[] { PrefabFolder }).Length : 0;
        }

        private static RealAssetPack InferPackFromPrefabName(string prefabName)
        {
            var normalizedName = NormalizeToken(prefabName);
            var bestPack = Packs[0];
            var bestScore = -1;
            foreach (var pack in Packs)
            {
                var score = ScoreArchiveName(pack, normalizedName);
                if (score > bestScore)
                {
                    bestScore = score;
                    bestPack = pack;
                }
            }

            return bestPack;
        }

        private static Bounds CalculatePrefabAssetBounds(string prefabPath)
        {
            var contents = PrefabUtility.LoadPrefabContents(prefabPath);
            try
            {
                return CalculateBounds(contents);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(contents);
            }
        }

        private static void DeleteAssetFolderContents(string folder)
        {
            if (!AssetDatabase.IsValidFolder(folder))
            {
                return;
            }

            foreach (var assetPath in Directory.GetFiles(ToFullPath(folder), "*", SearchOption.AllDirectories)
                         .Where(path => !path.EndsWith(".meta", StringComparison.OrdinalIgnoreCase))
                         .Select(ToAssetPath)
                         .OrderByDescending(path => path.Length))
            {
                AssetDatabase.DeleteAsset(assetPath);
            }
        }

        private static string Csv(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return "\"\"";
            }

            return "\"" + value.Replace("\"", "\"\"") + "\"";
        }

        private static string ResolveArchive(RealAssetPack pack, Real3DAssetPackReport packReport)
        {
            var existing = FindBestArchiveMatch(pack);
            if (!string.IsNullOrEmpty(existing))
            {
                packReport.archiveResolution = "Found local archive";
                return existing;
            }

            var downloaded = TryDownloadArchive(pack, packReport);
            if (File.Exists(downloaded))
            {
                packReport.archiveResolution = "Downloaded archive";
                return downloaded;
            }

            return string.Empty;
        }

        private static string FindBestArchiveMatch(RealAssetPack pack)
        {
            var archiveDirectory = ToFullPath(ArchiveFolder);
            if (!Directory.Exists(archiveDirectory))
            {
                return string.Empty;
            }

            return Directory.GetFiles(archiveDirectory, "*.zip", SearchOption.TopDirectoryOnly)
                .Select(path => new ArchiveCandidate(path, ScoreArchiveName(pack, Path.GetFileNameWithoutExtension(path))))
                .Where(candidate => candidate.Score > 0)
                .OrderByDescending(candidate => candidate.Score)
                .ThenBy(candidate => candidate.Path, StringComparer.OrdinalIgnoreCase)
                .Select(candidate => candidate.Path)
                .FirstOrDefault() ?? string.Empty;
        }

        private static int ScoreArchiveName(RealAssetPack pack, string fileNameWithoutExtension)
        {
            var normalizedName = NormalizeToken(fileNameWithoutExtension);
            var score = 0;
            if (normalizedName.Contains(NormalizeToken(pack.Id)))
            {
                score += 100;
            }

            if (normalizedName.Contains(NormalizeToken(pack.ArchiveNameHint)))
            {
                score += 80;
            }

            foreach (var token in pack.MatchTokens)
            {
                if (normalizedName.Contains(token))
                {
                    score += 15;
                }
            }

            return score;
        }

        private static string TryDownloadArchive(RealAssetPack pack, Real3DAssetPackReport packReport)
        {
            var html = DownloadText(pack.SourceUrl, packReport);
            if (string.IsNullOrEmpty(html))
            {
                return string.Empty;
            }

            var archiveUrl = FindZipUrl(pack.SourceUrl, html);
            if (string.IsNullOrEmpty(archiveUrl))
            {
                packReport.warning = $"Could not discover a ZIP download link on {pack.SourceUrl}. Manual download is required.";
                return string.Empty;
            }

            var targetPath = Path.Combine(ToFullPath(ArchiveFolder), pack.Id + ".zip");
            using (var request = UnityWebRequest.Get(archiveUrl))
            {
                var operation = request.SendWebRequest();
                while (!operation.isDone)
                {
                    EditorUtility.DisplayProgressBar("Downloading real 3D assets", pack.DisplayName, request.downloadProgress);
                }

                if (request.result != UnityWebRequest.Result.Success)
                {
                    packReport.warning = $"Download failed for {pack.DisplayName}: {request.error}";
                    Debug.LogWarning(packReport.warning);
                    return string.Empty;
                }

                File.WriteAllBytes(targetPath, request.downloadHandler.data);
            }

            return targetPath;
        }

        private static string DownloadText(string url, Real3DAssetPackReport packReport)
        {
            using (var request = UnityWebRequest.Get(url))
            {
                var operation = request.SendWebRequest();
                while (!operation.isDone)
                {
                    EditorUtility.DisplayProgressBar("Reading real 3D source", url, request.downloadProgress);
                }

                if (request.result != UnityWebRequest.Result.Success)
                {
                    packReport.warning = $"Could not read {url}: {request.error}";
                    Debug.LogWarning(packReport.warning);
                    return string.Empty;
                }

                return request.downloadHandler.text;
            }
        }

        private static string FindZipUrl(string sourceUrl, string html)
        {
            var match = Regex.Match(html, "href=\\\"(?<url>[^\\\"]+\\.zip(?:\\?[^\\\"]*)?)\\\"", RegexOptions.IgnoreCase);
            if (!match.Success)
            {
                match = Regex.Match(html, "(?<url>https?://[^\\s'\\\"]+\\.zip(?:\\?[^\\s'\\\"]*)?)", RegexOptions.IgnoreCase);
            }

            if (!match.Success)
            {
                return string.Empty;
            }

            var zipUrl = match.Groups["url"].Value.Replace("&amp;", "&");
            if (zipUrl.StartsWith("http://", StringComparison.OrdinalIgnoreCase) || zipUrl.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
            {
                return zipUrl;
            }

            var baseUri = new Uri(sourceUrl);
            return new Uri(baseUri, zipUrl).ToString();
        }

        private static string ExtractArchive(RealAssetPack pack, string archivePath)
        {
            var extractedPath = Path.Combine(ToFullPath(ExtractedFolder), pack.Id);
            if (Directory.Exists(extractedPath))
            {
                Directory.Delete(extractedPath, true);
            }

            Directory.CreateDirectory(extractedPath);
            ZipFile.ExtractToDirectory(archivePath, extractedPath);
            return ToAssetPath(extractedPath);
        }

        private static List<ImportedModel> FindModelAssets(RealAssetPack pack, string extractedAssetPath)
        {
            var fullExtractedPath = ToFullPath(extractedAssetPath);
            var modelFiles = Directory.GetFiles(fullExtractedPath, "*.*", SearchOption.AllDirectories)
                .Where(path => path.EndsWith(".fbx", StringComparison.OrdinalIgnoreCase) || path.EndsWith(".obj", StringComparison.OrdinalIgnoreCase))
                .OrderByDescending(path => path.EndsWith(".fbx", StringComparison.OrdinalIgnoreCase))
                .ThenBy(path => path, StringComparer.OrdinalIgnoreCase)
                .Take(MaxPrefabsPerPack);

            var importedModels = new List<ImportedModel>();
            foreach (var modelFile in modelFiles)
            {
                var assetPath = ToAssetPath(modelFile);
                var modelAsset = AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);
                if (modelAsset == null)
                {
                    continue;
                }

                importedModels.Add(new ImportedModel(pack, assetPath, modelAsset));
            }

            return importedModels;
        }

        private static List<PrefabRecord> CreatePrefabWrappers(List<ImportedModel> importedModels)
        {
            var records = new List<PrefabRecord>();
            for (var i = 0; i < importedModels.Count; i++)
            {
                var importedModel = importedModels[i];
                EditorUtility.DisplayProgressBar("Creating real prefabs", importedModel.Model.name, i / Mathf.Max(1f, importedModels.Count));

                var rootName = MakeSafeName(importedModel.Pack.Id + "_" + Path.GetFileNameWithoutExtension(importedModel.AssetPath) + "_" + ShortStableId(importedModel.AssetPath));
                var root = new GameObject(rootName);
                var instance = (GameObject)PrefabUtility.InstantiatePrefab(importedModel.Model);
                instance.name = importedModel.Model.name;
                instance.transform.SetParent(root.transform, false);
                NormalizeModelScale(root.transform);

                var bounds = CalculateBounds(root);
                var prefabPath = PrefabFolder + "/" + root.name + ".prefab";
                var prefab = PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
                UnityEngine.Object.DestroyImmediate(root);

                records.Add(new PrefabRecord(importedModel.Pack, prefab, root.name, importedModel.AssetPath, bounds));
            }

            return records;
        }

        private static void NormalizeModelScale(Transform root)
        {
            var bounds = CalculateBounds(root.gameObject);
            var longestHorizontalAxis = Mathf.Max(bounds.size.x, bounds.size.z);
            if (longestHorizontalAxis <= 0.001f)
            {
                return;
            }

            var targetMeters = Mathf.Clamp(longestHorizontalAxis, 8f, 80f);
            var scale = targetMeters / longestHorizontalAxis;
            root.localScale = Vector3.one * scale;
        }

        private static void CreateDatabases(List<PrefabRecord> records)
        {
            var prefabDatabase = ScriptableObject.CreateInstance<PrefabDatabase>();
            var assetCatalog = ScriptableObject.CreateInstance<AssetCatalog>();
            prefabDatabase.prefabs = new List<PrefabEntry>();
            assetCatalog.assets = new List<PlaceableAssetDefinition>();

            foreach (var record in records)
            {
                var footprint = new Vector2(Mathf.Max(1f, record.Bounds.size.x), Mathf.Max(1f, record.Bounds.size.z));
                var height = Mathf.Max(1f, record.Bounds.size.y);
                var plainText = $"{record.Id} | real {record.Pack.Author} {record.Pack.DisplayName} | {footprint.x:0.##}m x {footprint.y:0.##}m x {height:0.##}m | {record.Pack.License}";

                prefabDatabase.prefabs.Add(new PrefabEntry
                {
                    id = record.Id,
                    prefab = record.Prefab,
                    footprintMeters = footprint,
                    heightMeters = height,
                    category = record.Pack.Category,
                    priority = record.Pack.Priority,
                    allowedDistricts = new List<DistrictType>(record.Pack.AllowedDistricts),
                    plainText = plainText
                });

                assetCatalog.assets.Add(new PlaceableAssetDefinition
                {
                    id = record.Id,
                    prefab = record.Prefab,
                    footprintMeters = footprint,
                    heightMeters = height,
                    priority = record.Pack.Priority,
                    allowedDistricts = new List<DistrictType>(record.Pack.AllowedDistricts)
                });
            }

            SaveOrReplaceAsset(prefabDatabase, PrefabDatabasePath);
            SaveOrReplaceAsset(assetCatalog, AssetCatalogPath);
        }

        private static Real3DAssetImportReport CreateReport()
        {
            var report = ScriptableObject.CreateInstance<Real3DAssetImportReport>();
            report.importedAtUtc = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss 'UTC'");
            report.sourceFolder = RootFolder;
            return report;
        }

        private static Real3DAssetPackReport AddPackReport(Real3DAssetImportReport report, RealAssetPack pack)
        {
            var packReport = new Real3DAssetPackReport
            {
                packId = pack.Id,
                displayName = pack.DisplayName,
                author = pack.Author,
                sourceUrl = pack.SourceUrl,
                license = pack.License,
                status = "Pending"
            };
            report.packs.Add(packReport);
            return packReport;
        }

        private static void FillPrefabReport(Real3DAssetImportReport report, List<PrefabRecord> records)
        {
            report.totalPrefabCount = records.Count;
            report.totalModelCount = report.packs.Sum(pack => pack.modelCount);
            report.prefabs.Clear();

            foreach (var group in records.GroupBy(record => record.Pack.Id))
            {
                var packReport = report.packs.FirstOrDefault(pack => pack.packId == group.Key);
                if (packReport != null)
                {
                    packReport.prefabCount = group.Count();
                    packReport.status = group.Any() ? "Imported" : packReport.status;
                }
            }

            foreach (var record in records)
            {
                report.prefabs.Add(new Real3DAssetPrefabReport
                {
                    id = record.Id,
                    packId = record.Pack.Id,
                    sourceModelPath = record.SourceModelPath,
                    prefabPath = AssetDatabase.GetAssetPath(record.Prefab),
                    footprintMeters = new Vector2(Mathf.Max(1f, record.Bounds.size.x), Mathf.Max(1f, record.Bounds.size.z)),
                    heightMeters = Mathf.Max(1f, record.Bounds.size.y),
                    category = record.Pack.Category
                });
            }
        }

        private static void SaveImportReport(Real3DAssetImportReport report)
        {
            SaveOrReplaceAsset(report, ImportReportPath);
        }

        private static Bounds CalculateBounds(GameObject root)
        {
            var renderers = root.GetComponentsInChildren<Renderer>();
            if (renderers.Length > 0)
            {
                var bounds = renderers[0].bounds;
                for (var i = 1; i < renderers.Length; i++)
                {
                    bounds.Encapsulate(renderers[i].bounds);
                }
                return bounds;
            }

            return new Bounds(Vector3.zero, Vector3.one);
        }

        private static void WriteLicenseManifest()
        {
            var manifestPath = Path.Combine(ToFullPath(LicenseFolder), "REAL_ASSET_SOURCES.md");
            using (var writer = new StreamWriter(manifestPath, false))
            {
                writer.WriteLine("# Real 3D Asset Sources");
                writer.WriteLine();
                writer.WriteLine("These are real downloadable 3D asset packs used by the importer, not generated placeholder examples.");
                writer.WriteLine();
                foreach (var pack in Packs)
                {
                    writer.WriteLine($"- **{pack.DisplayName}** by {pack.Author}: {pack.SourceUrl} ({pack.License}).");
                }
            }
        }

        private static void SaveOrReplaceAsset(UnityEngine.Object asset, string path)
        {
            var existing = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(path);
            if (existing != null)
            {
                AssetDatabase.DeleteAsset(path);
            }
            AssetDatabase.CreateAsset(asset, path);
        }

        private static void EnsureFolders()
        {
            EnsureFolder("Assets/ZZCityGen", "Real3DAssetPacks");
            EnsureFolder(RootFolder, "Archives");
            EnsureFolder(RootFolder, "Extracted");
            EnsureFolder(RootFolder, "Prefabs");
            EnsureFolder(RootFolder, "Databases");
            EnsureFolder(RootFolder, "Licenses");
            EnsureFolder(RootFolder, "Reports");
        }

        private static void EnsureFolder(string parent, string child)
        {
            if (!AssetDatabase.IsValidFolder(parent + "/" + child))
            {
                AssetDatabase.CreateFolder(parent, child);
            }
        }

        private static string ToFullPath(string assetPath)
        {
            if (Path.IsPathRooted(assetPath))
            {
                return assetPath;
            }

            var projectRoot = Directory.GetParent(Application.dataPath).FullName;
            return Path.GetFullPath(Path.Combine(projectRoot, assetPath));
        }

        private static string ToAssetPath(string fullPath)
        {
            var projectRoot = Directory.GetParent(Application.dataPath).FullName.Replace("\\", "/");
            var normalized = Path.GetFullPath(fullPath).Replace("\\", "/");
            if (!normalized.StartsWith(projectRoot, StringComparison.OrdinalIgnoreCase))
            {
                return normalized;
            }

            return normalized.Substring(projectRoot.Length + 1);
        }

        private static string MakeSafeName(string value)
        {
            var safe = Regex.Replace(value, "[^A-Za-z0-9_]+", "_");
            return safe.Trim('_').ToLowerInvariant();
        }

        private static string NormalizeToken(string value)
        {
            return Regex.Replace(value.ToLowerInvariant(), "[^a-z0-9]+", string.Empty);
        }

        private static string ShortStableId(string value)
        {
            unchecked
            {
                var hash = 17;
                for (var i = 0; i < value.Length; i++)
                {
                    hash = hash * 31 + value[i];
                }
                return Mathf.Abs(hash).ToString("x");
            }
        }

        private sealed class ArchiveCandidate
        {
            public ArchiveCandidate(string path, int score)
            {
                Path = path;
                Score = score;
            }

            public string Path { get; }
            public int Score { get; }
        }

        private sealed class RealAssetPack
        {
            public RealAssetPack(string id, string displayName, string author, string sourceUrl, string license, PrefabCategory category, int priority, params DistrictType[] allowedDistricts)
            {
                Id = id;
                DisplayName = displayName;
                Author = author;
                SourceUrl = sourceUrl;
                License = license;
                Category = category;
                Priority = priority;
                AllowedDistricts = allowedDistricts;
                ArchiveNameHint = id.Replace("kenney_", string.Empty).Replace("_", "-");
                MatchTokens = BuildMatchTokens(id, displayName, ArchiveNameHint);
            }

            public string Id { get; }
            public string DisplayName { get; }
            public string Author { get; }
            public string SourceUrl { get; }
            public string License { get; }
            public PrefabCategory Category { get; }
            public int Priority { get; }
            public DistrictType[] AllowedDistricts { get; }
            public string ArchiveNameHint { get; }
            public string[] MatchTokens { get; }

            private static string[] BuildMatchTokens(string id, string displayName, string archiveNameHint)
            {
                return (id + " " + displayName + " " + archiveNameHint)
                    .Split(new[] { ' ', '_', '-', '(', ')' }, StringSplitOptions.RemoveEmptyEntries)
                    .Select(NormalizeToken)
                    .Where(token => token.Length > 2)
                    .Distinct()
                    .ToArray();
            }
        }

        private sealed class ImportedModel
        {
            public ImportedModel(RealAssetPack pack, string assetPath, GameObject model)
            {
                Pack = pack;
                AssetPath = assetPath;
                Model = model;
            }

            public RealAssetPack Pack { get; }
            public string AssetPath { get; }
            public GameObject Model { get; }
        }

        private sealed class PrefabRecord
        {
            public PrefabRecord(RealAssetPack pack, GameObject prefab, string id, string sourceModelPath, Bounds bounds)
            {
                Pack = pack;
                Prefab = prefab;
                Id = id;
                SourceModelPath = sourceModelPath;
                Bounds = bounds;
            }

            public RealAssetPack Pack { get; }
            public GameObject Prefab { get; }
            public string Id { get; }
            public string SourceModelPath { get; }
            public Bounds Bounds { get; }
        }
    }

    public sealed class Real3DAssetImportReport : ScriptableObject
    {
        public string importedAtUtc;
        public string sourceFolder;
        public int totalModelCount;
        public int totalPrefabCount;
        public List<Real3DAssetPackReport> packs = new List<Real3DAssetPackReport>();
        public List<Real3DAssetPrefabReport> prefabs = new List<Real3DAssetPrefabReport>();

    }

    [Serializable]
    public sealed class Real3DAssetPackReport
    {
        public string packId;
        public string displayName;
        public string author;
        public string sourceUrl;
        public string license;
        public string archiveResolution;
        public string archivePath;
        public string extractedPath;
        public string status;
        public string warning;
        public int modelCount;
        public int prefabCount;
    }

    [Serializable]
    public sealed class Real3DAssetPrefabReport
    {
        public string id;
        public string packId;
        public string sourceModelPath;
        public string prefabPath;
        public Vector2 footprintMeters;
        public float heightMeters;
        public PrefabCategory category;
    }
}
