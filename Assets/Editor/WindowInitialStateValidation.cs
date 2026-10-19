using System;
using System.IO;
using System.Text.RegularExpressions;
using Ohabjizol.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;

public static class WindowInitialStateValidation
{
    public static void Validate()
    {
        try
        {
            Windows();
            Scene("MainMenuScene", "MainMenuController", new[] { "initialWindows.codeOpen", "initialWindows.settingsOpen", "initialWindows.tutorialOpen" });
            Scene("InterviewScene", "InterviewScreen", new[] { "discordVisible", "initialDocuments.applicationOpen", "initialDocuments.portfolioOpen", "initialDocuments.scheduleOpen", "initialDocuments.interviewRecordOpen" });
            Debug.Log("[WindowInitialStateValidation] PASS");
        }
        catch (Exception error)
        {
            Debug.LogError("[WindowInitialStateValidation] FAIL: " + error);
            if (Application.isBatchMode) EditorApplication.Exit(1);
            throw;
        }
    }

    static void Windows()
    {
        GameObject canvas = null, events = null;
        try
        {
            canvas = new GameObject("ValidationCanvas", typeof(RectTransform), typeof(Canvas));
            canvas.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
            events = new GameObject("ValidationEvents", typeof(EventSystem));
            var parent = (RectTransform)canvas.transform;
            var w = WindowFactory.Create<WindowInitialStateProbeWindow>(parent, "Probe", new Vector2(300, 200), Vector2.zero);
            Check(w.BuildCount == 1 && !w.ActiveDuringBuild && !w.IsOpen && !w.gameObject.activeInHierarchy && w.RefreshCount == 0, "Fresh window must build inactive once without refresh");
            int opened = 0, closed = 0;
            w.Opened += sender => { Check(sender == w, "Opened sender"); opened++; };
            w.Closed += sender => { Check(sender == w, "Closed sender"); closed++; };
            w.ApplyInitialState(true);
            Check(w.IsOpen && opened == 1 && closed == 0 && w.RefreshCount == 1, "Initial true: Opened1/Refresh1");
            w.Close();
            Check(!w.IsOpen && closed == 1, "Close");
            w.ApplyInitialState(true);
            w.ApplyInitialState(false);
            Check(!w.IsOpen && opened == 1 && closed == 1 && w.RefreshCount == 1, "Duplicate initialization reopened or emitted");
            w.Toggle();
            Check(w.IsOpen && opened == 2 && w.RefreshCount == 2, "Toggle reopen");
            canvas.SetActive(false);
            Check(w.IsOpen && !w.gameObject.activeInHierarchy, "Hidden parent must preserve IsOpen");
            canvas.SetActive(true);
            Check(w.IsOpen && w.gameObject.activeInHierarchy && opened == 2 && w.RefreshCount == 2, "Parent reveal changed state or refreshed");
            w.Toggle();
            Check(!w.IsOpen && closed == 2, "Toggle close");
            w.Open();
            Check(w.IsOpen && opened == 3 && w.RefreshCount == 3, "Open after Close");
            var hidden = WindowFactory.Create<WindowInitialStateProbeWindow>(parent, "Hidden", new Vector2(300, 200), Vector2.zero);
            int hiddenClosed = 0, hiddenOpened = 0;
            hidden.Closed += sender => hiddenClosed++;
            hidden.Opened += sender => hiddenOpened++;
            hidden.ApplyInitialState(false);
            hidden.ApplyInitialState(true);
            Check(!hidden.IsOpen && hiddenClosed == 0 && hiddenOpened == 0 && hidden.RefreshCount == 0, "Initial false must be silent and consume initialization");
        }
        finally
        {
            if (canvas != null) UnityEngine.Object.DestroyImmediate(canvas);
            if (events != null) UnityEngine.Object.DestroyImmediate(events);
        }
    }

    static void Scene(string name, string hostName, string[] properties)
    {
        string path = null;
        foreach (string guid in AssetDatabase.FindAssets(name + " t:Scene"))
        {
            string candidate = AssetDatabase.GUIDToAssetPath(guid);
            if (Path.GetFileNameWithoutExtension(candidate) != name) continue;
            Check(path == null, "Ambiguous scene: " + name);
            path = candidate;
        }
        Check(path != null, "Missing scene: " + name);
        string yaml = File.ReadAllText(path);
        string first = ReadScene(path, hostName, properties, yaml);
        Check(first == ReadScene(path, hostName, properties, yaml), "Initial values changed on scene reopen: " + name);
    }

    static string ReadScene(string path, string hostName, string[] properties, string yaml)
    {
        var scene = EditorSceneManager.OpenPreviewScene(path);
        try
        {
            string snapshot = "";
            int count = 0;
            foreach (GameObject root in scene.GetRootGameObjects())
            foreach (MonoBehaviour host in root.GetComponentsInChildren<MonoBehaviour>(true))
            {
                if (host == null || host.GetType().Name != hostName) continue;
                count++;
                var serialized = new SerializedObject(host);
                serialized.Update();
                bool hasInitialDocuments = serialized.FindProperty("initialDocuments") != null;
                SavedHost savedHost = ReadSavedHost(host, yaml);
                snapshot += savedHost.InstanceKey + ":";
                foreach (string propertyPath in properties)
                {
                    if (hostName == "InterviewScreen" && !hasInitialDocuments
                        && propertyPath.StartsWith("initialDocuments.", StringComparison.Ordinal))
                        continue;
                    var property = serialized.FindProperty(propertyPath);
                    Check(property != null && property.propertyType == SerializedPropertyType.Boolean, "Missing serialized bool: " + propertyPath);
                    bool savedValue;
                    Check(savedHost.TryReadBool(propertyPath, out savedValue), "Initial value not explicitly saved: " + propertyPath);
                    Check(property.boolValue == savedValue, "Loaded/saved mismatch: " + propertyPath);
                    snapshot += propertyPath + "=" + property.boolValue + ";";
                }
            }
            Check(count > 0, "Missing existing host: " + hostName);
            return snapshot;
        }
        finally { EditorSceneManager.ClosePreviewScene(scene); }
    }

    // Read-only saved-value provider. Only freshly loaded preview hosts are accepted.
    internal sealed class SavedHost
    {
        internal string Block, SourceGuid, InstanceKey;
        internal long SourceId;
        internal PropertyModification[] Modifications;

        internal bool TryReadBool(string path, out bool value)
        {
            value = false;
            string[] parts = path.Split('.');
            Check(parts.Length == 1 || parts.Length == 2, "Unsupported saved bool path: " + path);
            string body = Block;
            if (parts.Length == 2)
            {
                var parents = Regex.Matches(Block, @"(?m)^  " + Regex.Escape(parts[0]) + @":([^\r\n]*)\r?$");
                Check(parents.Count <= 1, "Duplicate saved state: " + path);
                body = "";
                if (parents.Count == 1)
                {
                    Check(string.IsNullOrWhiteSpace(parents[0].Groups[1].Value), "Invalid saved state: " + path);
                    body = Regex.Match(Block.Substring(parents[0].Index), @"\A[^\r\n]*\r?\n(?<body>(?:    [^\r\n]*(?:\r?\n|\z))*)").Groups["body"].Value;
                }
            }
            var fields = Regex.Matches(body, @"(?m)^" + (parts.Length == 1 ? "  " : "    ") + Regex.Escape(parts[parts.Length - 1]) + @":([^\r\n]*)\r?$");
            Check(fields.Count <= 1, "Duplicate saved bool: " + path);
            bool hasSource = fields.Count == 1;
            bool sourceValue = hasSource && ParseBool(fields[0].Groups[1].Value.Trim(), path);
            int count = 0;
            if (Modifications != null)
            foreach (var modification in Modifications)
            {
                if (modification == null || modification.propertyPath != path) continue;
                Check(modification.target != null, "Missing prefab override target: " + path);
                string guid;
                long id;
                Check(AssetDatabase.TryGetGUIDAndLocalFileIdentifier(modification.target, out guid, out id), "Cannot identify prefab override target: " + path);
                if (id != SourceId || !string.Equals(guid, SourceGuid, StringComparison.OrdinalIgnoreCase)) continue;
                Check(++count == 1, "Duplicate prefab bool override: " + path);
                Check(modification.objectReference == null, "Invalid bool override object reference: " + path);
                value = ParseBool(modification.value, path);
            }
            if (count == 1) return true;
            value = sourceValue;
            return hasSource;
        }

        static bool ParseBool(string text, string path)
        {
            Check(text == "0" || text == "1", "Invalid saved bool (expected 0/1): " + path);
            return text == "1";
        }
    }

    internal static SavedHost ReadSavedHost(MonoBehaviour host, string sceneYaml)
    {
        Check(EditorSceneManager.IsPreviewScene(host.gameObject.scene), "Saved-value provider requires a preview scene host.");
        var source = PrefabUtility.GetCorrespondingObjectFromSource(host);
        if (source == null)
        {
            Check(!PrefabUtility.IsPartOfPrefabInstance(host), "Unsupported prefab host without source.");
            long id;
            Match block = FindSavedHostBlock(host, sceneYaml, out id);
            return new SavedHost { Block = block.Value, InstanceKey = "scene:" + id };
        }
        GameObject outermost = PrefabUtility.GetOutermostPrefabInstanceRoot(host);
        Check(outermost != null && PrefabUtility.GetNearestPrefabInstanceRoot(host) == outermost, "Unsupported nested prefab host.");
        Check(PrefabUtility.GetPrefabAssetType(outermost) == PrefabAssetType.Regular
            && PrefabUtility.GetPrefabAssetType(source) == PrefabAssetType.Regular, "Unsupported prefab variant host.");
        string guid;
        long sourceId;
        Check(AssetDatabase.TryGetGUIDAndLocalFileIdentifier(source, out guid, out sourceId), "Cannot identify source prefab host.");
        string assetPath = AssetDatabase.GUIDToAssetPath(guid);
        Check(!string.IsNullOrEmpty(assetPath) && assetPath.EndsWith(".prefab", StringComparison.OrdinalIgnoreCase), "Missing source prefab asset.");
        string yaml = File.ReadAllText(assetPath);
        Check(!Regex.IsMatch(yaml, @"(?m)^--- !u!1001 &"), "Unsupported nested/variant source prefab: " + assetPath);
        var blocks = Regex.Matches(yaml, @"(?ms)^--- !u!114 &" + sourceId + @"\r?\n.*?(?=^--- |\z)");
        Check(blocks.Count == 1, "Missing or duplicate exact source prefab host block: " + guid + "/" + sourceId);
        var serialized = new SerializedObject(host);
        serialized.Update();
        var scriptProperty = serialized.FindProperty("m_Script");
        MonoScript script = MonoScript.FromMonoBehaviour(host);
        Check(script != null && scriptProperty != null && scriptProperty.objectReferenceValue == script, "Host script identity mismatch.");
        string scriptGuid;
        long scriptId;
        Check(AssetDatabase.TryGetGUIDAndLocalFileIdentifier(script, out scriptGuid, out scriptId), "Cannot identify host script.");
        var scripts = Regex.Matches(blocks[0].Value, @"(?m)^  m_Script: \{fileID: (-?\d+), guid: ([0-9a-fA-F]{32}), type: \d+\}\r?$");
        long savedScriptId;
        Check(scripts.Count == 1 && long.TryParse(scripts[0].Groups[1].Value, out savedScriptId)
            && savedScriptId == scriptId && string.Equals(scripts[0].Groups[2].Value, scriptGuid, StringComparison.OrdinalIgnoreCase), "Source prefab m_Script identity mismatch.");
        string hierarchy = "";
        for (Transform current = host.transform; current != null; current = current.parent)
            hierarchy = current.GetSiblingIndex() + "/" + hierarchy;
        return new SavedHost
        {
            Block = blocks[0].Value, SourceGuid = guid, SourceId = sourceId,
            Modifications = PrefabUtility.GetPropertyModifications(outermost),
            InstanceKey = "prefab:" + hierarchy + ":" + guid + "/" + sourceId
        };
    }

    static Match FindSavedHostBlock(MonoBehaviour host, string yaml, out long fileId)
    {
        string hostName = host.GetType().Name;
        MonoScript script = MonoScript.FromMonoBehaviour(host);
        Check(script != null && EditorUtility.IsPersistent(script), "Missing persistent host script: " + hostName);
        string scriptPath = AssetDatabase.GetAssetPath(script);
        Check(!string.IsNullOrEmpty(scriptPath), "Missing host script asset path: " + hostName);
        string scriptGuid = AssetDatabase.AssetPathToGUID(scriptPath);
        Check(Regex.IsMatch(scriptGuid, @"\A[0-9a-fA-F]{32}\z"), "Invalid host script GUID: " + hostName);

        Match savedBlock = null;
        int count = 0;
        foreach (Match block in Regex.Matches(yaml, @"(?ms)^--- !u!114 &[^\r\n]*\r?\n.*?(?=^--- |\z)"))
        {
            Match scriptReference = Regex.Match(block.Value, @"(?m)^  m_Script: \{fileID: -?\d+, guid: ([0-9a-fA-F]{32}), type: \d+\}\r?$");
            if (!scriptReference.Success || !string.Equals(scriptReference.Groups[1].Value, scriptGuid, StringComparison.OrdinalIgnoreCase)) continue;
            savedBlock = block;
            count++;
        }
        Check(count > 0, "Missing or unparseable saved host script block: " + hostName);
        Check(count == 1, "Ambiguous saved host script blocks: " + hostName + " (" + count + ")");
        Match header = Regex.Match(savedBlock.Value, @"\A--- !u!114 &(-?\d+)\r?\n");
        fileId = 0;
        Check(header.Success && long.TryParse(header.Groups[1].Value, out fileId), "Cannot parse saved host scene fileID: " + hostName);
        return savedBlock;
    }

    static void Check(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}

public sealed class WindowInitialStateProbeWindow : InfoWindow
{
    public int BuildCount { get; private set; }
    public int RefreshCount { get; private set; }
    public bool ActiveDuringBuild { get; private set; }
    protected override void BuildInfo(RectTransform info)
    {
        BuildCount++;
        ActiveDuringBuild = gameObject.activeSelf;
        if (info == null) throw new InvalidOperationException("Missing probe info");
    }
    public override void Refresh() { RefreshCount++; }
}
