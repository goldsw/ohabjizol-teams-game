using System;
using Ohabjizol.Interview;
using Ohabjizol.Interview.Diagnostics;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

[InitializeOnLoad]
public static class InterviewRefreshRuntimeValidation
{
    const string Running = "InterviewRefresh.Runtime.Running";
    const string Result = "InterviewRefresh.Runtime.Result";
    const string Setup = "InterviewRefresh.Runtime.Setup";
    const string PrefsBackup = "InterviewRefresh.Runtime.PersistenceBackup";
    [Serializable] sealed class PrefsDto { public PrefEntry[] entries; }
    [Serializable] sealed class PrefEntry { public string key; public bool existed; public string value; }
    [Serializable] sealed class SceneDto { public SceneSetup[] scenes; }
    static InterviewRefreshRuntimeValidation() { EditorApplication.playModeStateChanged += Changed; }

    public static void Start()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Runtime checks start from Edit Mode.");
        if (!EditorSceneManager.SaveOpenScenes()) throw new InvalidOperationException("Save open scenes before runtime checks.");
        SessionState.SetString(Setup, JsonUtility.ToJson(new SceneDto { scenes = EditorSceneManager.GetSceneManagerSetup() }));
        BackupPersistence();
        SessionState.SetBool("InterviewRefresh.Runtime.Background", Application.runInBackground);
        SessionState.SetString(Result, "pending");
        SessionState.SetBool(Running, true);
        try
        {
            EditorSceneManager.OpenScene("Assets/Scenes/MainMenuScene.unity", OpenSceneMode.Single);
            EditorApplication.EnterPlaymode();
        }
        catch
        {
            RestorePersistence();
            SessionState.SetBool(Running, false);
            RestoreSceneSetup();
            throw;
        }
    }

    public static string Status() => SessionState.GetString(Result, "idle");

    static void BackupPersistence()
    {
        var keys = StageCompletionService.PersistenceKeys;
        var dto = new PrefsDto { entries = new PrefEntry[keys.Count] };
        for (int i = 0; i < keys.Count; i++)
            dto.entries[i] = new PrefEntry { key = keys[i], existed = PlayerPrefs.HasKey(keys[i]),
                value = PlayerPrefs.GetString(keys[i], "") };
        SessionState.SetString(PrefsBackup, JsonUtility.ToJson(dto));
    }
    static void RestorePersistence()
    {
        var dto = JsonUtility.FromJson<PrefsDto>(SessionState.GetString(PrefsBackup, ""));
        if (dto?.entries == null) throw new InvalidOperationException("Persistence backup is missing.");
        foreach (var entry in dto.entries)
        {
            if (entry.existed) PlayerPrefs.SetString(entry.key, entry.value);
            else PlayerPrefs.DeleteKey(entry.key);
        }
        PlayerPrefs.Save();
    }
    static void RestoreSceneSetup()
    {
        var dto = JsonUtility.FromJson<SceneDto>(SessionState.GetString(Setup, "{}"));
        if (dto?.scenes != null && dto.scenes.Length > 0) EditorSceneManager.RestoreSceneManagerSetup(dto.scenes);
    }

    static void Changed(PlayModeStateChange state)
    {
        if (!SessionState.GetBool(Running, false)) return;
        if (state == PlayModeStateChange.EnteredPlayMode)
        {
            try
            {
                Application.runInBackground = true;
                var host = new GameObject("InterviewRefreshRuntimeProbe");
                UnityEngine.Object.DontDestroyOnLoad(host);
                host.AddComponent<InterviewRefreshRuntimeProbe>();
            }
            catch
            {
                RestorePersistence();
                SessionState.SetString(Result, "FAIL: Runtime probe initialization failed.");
                EditorApplication.ExitPlaymode();
                throw;
            }
        }
        if (state == PlayModeStateChange.EnteredEditMode)
        {
            RestorePersistence();
            SessionState.SetBool(Running, false);
            Application.runInBackground = SessionState.GetBool("InterviewRefresh.Runtime.Background", false);
            RestoreSceneSetup();
            if (Status() == "pending") SessionState.SetString(Result, "FAIL: Play Mode exited before validation finished.");
            Debug.Log("[InterviewRefreshRuntimeValidation] " + Status());
        }
    }
}
