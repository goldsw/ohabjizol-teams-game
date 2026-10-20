using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Globalization;
using System.Text.RegularExpressions;
using Ohabjizol.Interview;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
namespace Ohabjizol.EditorTools
{
    public static class CharacterStatsImportBake
    {
        const string Root = "Assets/Data/Interview";
        const string Normalized = "Logs/UIEffects/20261015-MainMenuStats/character-stats-normalized.json";
        const string Original = "Logs/UIEffects/20261015-MainMenuStats/character-stats-source.json";
        const string CatalogPath = Root + "/ApplicantCatalog.asset";
        const string StagesPath = Root + "/StageStatsCatalog.asset";
        static readonly string[] DisplayNames =
        {
            "강서윤", "차도현", "백하린", "윤세아", "한유진", "서도윤",
            "정미소", "이가은", "백시현", "임태오", "김재하", "오지안"
        };
        [Serializable] sealed class Source { public Character[] characters; }
        [Serializable] sealed class Character { public string name, sourceSheet; public int sourceRow; public Role[] roles; }
        [Serializable] sealed class Role { public string role, roleCell; public SkillRanges baseStats, fakeStats; }
        [Serializable] sealed class Cell { public string cell, value, formula, cachedValue; public bool blank; }
        // JsonUtility does not preserve JSON null reliably for string fields.
        // Read source scalar tokens explicitly; keep normalized DTO parsing separate.
        static string ScalarToken(string json, string key)
        {
            var match = Regex.Match(json, "\"" + Regex.Escape(key) + "\"\\s*:\\s*(?<token>\"(?:\\\\.|[^\"\\\\])*\"|null|true|false|-?(?:0|[1-9][0-9]*)(?:\\.[0-9]+)?(?:[eE][+-]?[0-9]+)?)\\s*(?=[,}])");
            if (!match.Success) throw new InvalidDataException("Missing or invalid source scalar: " + key);
            return match.Groups["token"].Value;
        }
        static string SourceText(string json, string key, bool allowNumber = false)
        {
            string token = ScalarToken(json, key);
            if (token == "null") return null;
            if (token[0] != '"')
            {
                if (allowNumber && token != "true" && token != "false") return token;
                throw new InvalidDataException("Expected source string or null: " + key);
            }
            var text = new StringBuilder();
            for (int i = 1; i < token.Length - 1; i++)
            {
                char c = token[i];
                if (c != '\\') { if (c < 32) throw new InvalidDataException("Invalid JSON control character."); text.Append(c); continue; }
                c = token[++i];
                switch (c)
                {
                    case '"': case '\\': case '/': text.Append(c); break;
                    case 'b': text.Append('\b'); break;
                    case 'f': text.Append('\f'); break;
                    case 'n': text.Append('\n'); break;
                    case 'r': text.Append('\r'); break;
                    case 't': text.Append('\t'); break;
                    case 'u':
                        if (i + 4 >= token.Length - 1) throw new InvalidDataException("Invalid JSON Unicode escape.");
                        text.Append((char)int.Parse(token.Substring(i + 1, 4), NumberStyles.HexNumber, CultureInfo.InvariantCulture)); i += 4; break;
                    default: throw new InvalidDataException("Invalid JSON escape.");
                }
            }
            return text.ToString();
        }
        static Cell ParseCell(string json)
        {
            string blank = ScalarToken(json, "blank");
            if (blank != "true" && blank != "false") throw new InvalidDataException("Expected source boolean: blank");
            return new Cell { cell = SourceText(json, "cell"), value = SourceText(json, "value", true),
                formula = SourceText(json, "formula"), cachedValue = SourceText(json, "cachedValue", true), blank = blank == "true" };
        }
        static Dictionary<string, string> ReadCells(string json)
        {
            var result = new Dictionary<string, string>();
            foreach (Match match in Regex.Matches(json, @"\{\s*""cell""\s*:\s*""[^""]+""[^{}]*\}"))
            {
                var cell = ParseCell(match.Value);
                result.Add(cell.cell, match.Value);
            }
            return result;
        }
        static Cell ReadCell(Dictionary<string, string> cells, string address)
        {
            if (!cells.TryGetValue(address, out var json)) throw new InvalidDataException("Missing source cell: " + address);
            return ParseCell(json);
        }
        static void ValidateRange(StatRange range, bool optional, Dictionary<string, string> cells)
        {
            if (range == null) throw new InvalidDataException("Missing range metadata.");
            var original = ReadCell(cells, range.cell);
            string raw = original.formula == null ? original.value : original.cachedValue;
            if (original.blank != range.blank || (!range.blank && raw != range.raw)) throw new InvalidDataException("Source mismatch: " + range.cell);
            if (range.blank) { if (!optional) throw new InvalidDataException("Blank base range: " + range.cell); return; }
            var match = Regex.Match(range.raw ?? "", @"^\s*(\d+)\s*(?:~\s*(\d+)\s*)?$");
            if (!match.Success) throw new InvalidDataException("Invalid range: " + range.cell);
            int min = int.Parse(match.Groups[1].Value);
            int max = match.Groups[2].Success ? int.Parse(match.Groups[2].Value) : min;
            if (min != range.min || max != range.max || min > max || max == int.MaxValue) throw new InvalidDataException("Invalid endpoints: " + range.cell);
        }
        static void ValidateSkills(SkillRanges skills, bool optional, Dictionary<string, string> cells)
        {
            if (skills == null) throw new InvalidDataException("Missing skill group metadata.");
            ValidateRange(skills.planning, optional, cells); ValidateRange(skills.programming, optional, cells); ValidateRange(skills.graphics, optional, cells);
        }
        static JobRole ParseRole(string role)
        {
            switch (role) { case "기획": return JobRole.Planning; case "프로그래밍": return JobRole.Programming; case "아트": return JobRole.Graphics; default: throw new InvalidDataException("Unknown role: " + role); }
        }
        [MenuItem("Ohabjizol/Interview/Import Character Stats")]
        public static void Apply()
        {
            bool logInputs = File.Exists(Normalized) && File.Exists(Original);
            string normalized = File.ReadAllText(logInputs ? Normalized : Root + "/CharacterStatsSource.json");
            string original = File.ReadAllText(logInputs ? Original : Root + "/CharacterStatsOriginal.json");
            var source = JsonUtility.FromJson<Source>(normalized);
            var cells = ReadCells(original);
            if (source?.characters == null || source.characters.Length != 13) throw new InvalidDataException("Exactly thirteen characters required.");
            // Preserve the ID-ordered display names when legacy Logs inputs take precedence.
            int nameIndex = 0;
            normalized = Regex.Replace(normalized, @"(""name""\s*:\s*)""(?:\\.|[^""\\])*""", match =>
            {
                int index = nameIndex++;
                return index < DisplayNames.Length ? match.Groups[1].Value + "\"" + DisplayNames[index] + "\"" : match.Value;
            });
            for (int i = 0; i < DisplayNames.Length; i++)
            {
                source.characters[i].name = DisplayNames[i];
                string nameCell = "B" + source.characters[i].sourceRow.ToString(CultureInfo.InvariantCulture);
                original = Regex.Replace(original, "(\"cell\"\\s*:\\s*\"" + nameCell + "\"\\s*,\\s*\"value\"\\s*:\\s*)\"(?:\\\\.|[^\"\\\\])*\"",
                    match => match.Groups[1].Value + "\"" + DisplayNames[i] + "\"");
            }
            var names = new HashSet<string>();
            foreach (var character in source.characters)
            {
                if (string.IsNullOrEmpty(character.name) || !names.Add(character.name) || character.roles == null || character.roles.Length != 3) throw new InvalidDataException("Invalid character or role count.");
                var roles = new HashSet<JobRole>();
                foreach (var role in character.roles)
                {
                    if (!roles.Add(ParseRole(role.role)) || ReadCell(cells, role.roleCell).value != role.role) throw new InvalidDataException("Duplicate or mismatched role.");
                    ValidateSkills(role.baseStats, false, cells); ValidateSkills(role.fakeStats, true, cells);
                }
            }
            Directory.CreateDirectory(Root + "/Profiles");
            File.WriteAllText(Root + "/CharacterStatsSource.json", normalized);
            File.WriteAllText(Root + "/CharacterStatsOriginal.json", original);
            AssetDatabase.Refresh();
            var profiles = new ApplicantProfileAsset[13];
            var ids = new string[13];
            for (int i = 0; i < profiles.Length; i++)
            {
                var character = source.characters[i];
                string id = "character-" + (i + 1).ToString("00"); ids[i] = id;
                bool created = AssetDatabase.LoadAssetAtPath<ApplicantProfileAsset>(Root + "/Profiles/" + id + ".asset") == null;
                var asset = LoadOrCreate<ApplicantProfileAsset>(Root + "/Profiles/" + id + ".asset");
                asset.characterId = id; asset.displayName = character.name; asset.sourceSheet = character.sourceSheet; asset.sourceRow = character.sourceRow;
                asset.roleProfiles = new ApplicantRoleProfile[3];
                for (int r = 0; r < 3; r++)
                {
                    var role = character.roles[r];
                    string fakeCell = "G" + role.roleCell.Substring(1);
                    var rawCells = new List<string> { cells[role.roleCell], cells[fakeCell] };
                    foreach (var range in new[] { role.baseStats.planning, role.baseStats.programming, role.baseStats.graphics, role.fakeStats.planning, role.fakeStats.programming, role.fakeStats.graphics }) rawCells.Add(cells[range.cell]);
                    asset.roleProfiles[r] = new ApplicantRoleProfile { role = ParseRole(role.role), roleCell = role.roleCell, baseStatRanges = role.baseStats, fakeStatRanges = role.fakeStats,
                        fakeRoleMetadata = ReadCell(cells, fakeCell).value, observedSourceCellsJson = "[" + string.Join(",", rawCells) + "]" };
                }
                if (created) asset.truePortfolioQuality = PrototypeCatalog.Applicants[i % PrototypeCatalog.Applicants.Count].TruePortfolioQuality;
                EditorUtility.SetDirty(asset); profiles[i] = asset;
            }
            var catalog = LoadOrCreate<ApplicantCatalogAsset>(CatalogPath); catalog.profiles = profiles; EditorUtility.SetDirty(catalog);
            var stages = LoadOrCreate<StageStatsCatalogAsset>(StagesPath);
            var entries = new StageStatsEntry[4];
            for (int i = 0; i < entries.Length; i++)
            {
                var id = (StageId)i;
                entries[i] = stages.Find(id) ?? new StageStatsEntry { stageId = id };
                if (id == StageId.EntranceGameJam) { entries[i].useOverride = true; entries[i].characterIds = ids; entries[i].requiredPlanning = 30; entries[i].requiredProgramming = 30; entries[i].requiredGraphics = 30; }
            }
            stages.stages = entries; EditorUtility.SetDirty(stages);
            AssetDatabase.SaveAssets(); ApplyBindings();
            Debug.Log("Character stats imported: 13 profiles / 39 role variants.");
        }
        static T LoadOrCreate<T>(string path) where T : ScriptableObject
        {
            var asset = AssetDatabase.LoadAssetAtPath<T>(path);
            if (asset != null) return asset;
            asset = ScriptableObject.CreateInstance<T>(); AssetDatabase.CreateAsset(asset, path); return asset;
        }
        static string UniqueAsset(string name, string type)
        {
            string found = null;
            foreach (string guid in AssetDatabase.FindAssets(name + " t:" + type))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                if (Path.GetFileNameWithoutExtension(path) != name) continue;
                if (found != null) throw new InvalidOperationException("Ambiguous asset: " + name);
                found = path;
            }
            return found ?? throw new FileNotFoundException(name);
        }
        static void Bind(GameObject target, ApplicantCatalogAsset catalog, StageStatsCatalogAsset stages)
        {
            var installer = target.GetComponent<InterviewDataInstaller>();
            if (installer == null) installer = target.AddComponent<InterviewDataInstaller>();
            installer.applicantCatalog = catalog; installer.stageStatsCatalog = stages; EditorUtility.SetDirty(installer);
            PrefabUtility.RecordPrefabInstancePropertyModifications(installer);
        }
        [MenuItem("Ohabjizol/Interview/Bind Character Stats")]
        public static void ApplyBindings()
        {
            var catalog = AssetDatabase.LoadAssetAtPath<ApplicantCatalogAsset>(CatalogPath);
            var stages = AssetDatabase.LoadAssetAtPath<StageStatsCatalogAsset>(StagesPath);
            if (catalog == null || stages == null) throw new InvalidOperationException("Import stats first.");
            string prefabPath = UniqueAsset("InterviewScreen", "Prefab");
            var root = PrefabUtility.LoadPrefabContents(prefabPath);
            try { Bind(root, catalog, stages); PrefabUtility.SaveAsPrefabAsset(root, prefabPath); }
            finally { PrefabUtility.UnloadPrefabContents(root); }
            string scenePath = UniqueAsset("MainMenuScene", "Scene");
            Scene active = SceneManager.GetActiveScene();
            Scene scene = SceneManager.GetSceneByPath(scenePath);
            bool opened = !scene.IsValid() || !scene.isLoaded;
            if (!opened && scene.isDirty) throw new InvalidOperationException("Save MainMenuScene edits before binding.");
            if (opened) scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Additive);
            try
            {
                GameObject target = null;
                foreach (var sceneRoot in scene.GetRootGameObjects()) foreach (var component in sceneRoot.GetComponentsInChildren<MonoBehaviour>(true))
                    if (component != null && component.GetType().Name == "MainMenuController")
                    { if (target != null) throw new InvalidOperationException("Multiple MainMenuControllers."); target = component.gameObject; }
                if (target == null) throw new InvalidOperationException("Missing MainMenuController.");
                Bind(target, catalog, stages); EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene);
            }
            finally { if (opened) EditorSceneManager.CloseScene(scene, true); if (active.IsValid() && active.isLoaded) SceneManager.SetActiveScene(active); }
        }
    }
}