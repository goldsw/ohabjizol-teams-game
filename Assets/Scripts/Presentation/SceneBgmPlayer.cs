using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Ohabjizol.Presentation
{
[RequireComponent(typeof(AudioSource))]
public sealed class SceneBgmPlayer : MonoBehaviour
{
    [Serializable]
    public sealed class Entry
    {
        public string[] sceneNames;
        public AudioClip clip;
        public bool loop = true;

        public Entry(string[] sceneNames, AudioClip clip, bool loop = true)
        {
            this.sceneNames = sceneNames;
            this.clip = clip;
            this.loop = loop;
        }
    }

    [SerializeField] private Entry[] entries = Array.Empty<Entry>();
    [SerializeField, Range(0f, 1f)] private float volume = .6f;
    [SerializeField, Min(0f)] private float fadeDuration = .35f;
    private static SceneBgmPlayer instance;
    private static bool lobbyMusicUnlocked;
    private AudioSource source;
    private AudioListener ownListener;
    private Coroutine transition;
    private AudioClip requestedClip;

    public bool HasAuthoredAudio => entries != null && entries.Length > 0 &&
        Array.TrueForAll(entries, entry => entry != null && entry.clip != null &&
            entry.sceneNames != null && entry.sceneNames.Length > 0);

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetInstance()
    {
        instance = null;
        lobbyMusicUnlocked = false;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Bootstrap()
    {
        if (instance != null) return;
        var prefab = Resources.Load<SceneBgmPlayer>("Audio/SceneBgmPlayer");
        if (prefab != null) Instantiate(prefab);
    }

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            if (TryGetComponent<AudioSource>(out var duplicateSource)) duplicateSource.Stop();
            if (TryGetComponent<AudioListener>(out var duplicateListener)) duplicateListener.enabled = false;
            enabled = false;
            Destroy(gameObject);
            return;
        }
        instance = this;
        DontDestroyOnLoad(gameObject);
        source = GetComponent<AudioSource>();
        ownListener = GetComponent<AudioListener>();
        ConfigureSource();
        source.volume = 0f;
    }

    private void OnEnable()
    {
        if (instance != this) return;
        SceneManager.sceneLoaded += OnSceneLoaded;
        RefreshListener();
        SelectScene(SceneManager.GetActiveScene());
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        if (transition != null) StopCoroutine(transition);
        transition = null;
        if (source != null) source.Stop();
        if (ownListener != null) ownListener.enabled = false;
    }

    private void OnDestroy()
    {
        if (instance == this) instance = null;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        RefreshListener();
        SelectScene(scene);
    }

    private void RefreshListener()
    {
        if (ownListener == null) return;
        ownListener.enabled = false;
        foreach (var listener in FindObjectsByType<AudioListener>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (listener != ownListener && listener.isActiveAndEnabled) return;
        }
        ownListener.enabled = true;
    }

    private void SelectScene(Scene scene)
    {
        if (scene.name == "LoadingScene") return;
        var sceneName = scene.name;
        if (sceneName == "EndingScene")
        {
            sceneName = "NormalEndingScene";
            foreach (var root in scene.GetRootGameObjects())
            {
                var board = root.GetComponentInChildren<Ohabjizol.Interview.EndingBoard>(true);
                if (board == null) continue;
                if (board.NormalPanel != null && board.NormalPanel.activeInHierarchy) break;
                if (board.GoodPanel != null && board.GoodPanel.activeInHierarchy) sceneName = "GoodEndingScene";
                else if (board.BadPanel != null && board.BadPanel.activeInHierarchy) sceneName = "BadEndingScene";
                break;
            }
        }
        var clip = FindClip(sceneName);
        var loop = FindLoop(sceneName);
        if (clip == source.clip) source.loop = loop;
        if (transition != null && requestedClip == clip) return;
        if (transition != null) StopCoroutine(transition);
        transition = null;
        requestedClip = clip;
        if (clip == source.clip && source.isPlaying && Mathf.Approximately(source.volume, volume)) return;
        if (clip == null && !source.isPlaying) { source.clip = null; return; }
        transition = StartCoroutine(ChangeTrack(clip, loop));
    }

    public static void RefreshCurrentScene()
    {
        if (instance != null) instance.SelectScene(SceneManager.GetActiveScene());
    }

    public static void UnlockLobbyMusicForSession()
    {
        lobbyMusicUnlocked = true;
        RefreshCurrentScene();
    }

    private AudioClip FindClip(string sceneName)
    {
        if (sceneName == "MainMenuScene" && !lobbyMusicUnlocked) return null;
        if (entries == null) return null;
        foreach (var entry in entries)
            if (entry != null && entry.sceneNames != null && Array.IndexOf(entry.sceneNames, sceneName) >= 0)
                return entry.clip;
        return null;
    }

    private bool FindLoop(string sceneName)
    {
        if (entries != null)
            foreach (var entry in entries)
                if (entry != null && entry.sceneNames != null && Array.IndexOf(entry.sceneNames, sceneName) >= 0)
                    return entry.loop;
        return true;
    }

    private IEnumerator ChangeTrack(AudioClip clip, bool loop)
    {
        if (source.clip != clip)
        {
            if (source.isPlaying) yield return FadeTo(0f);
            source.Stop();
            source.clip = clip;
            source.volume = 0f;
        }
        if (clip != null)
        {
            source.loop = loop;
            if (!source.isPlaying) source.Play();
            yield return FadeTo(volume);
        }
        transition = null;
    }

    private IEnumerator FadeTo(float target)
    {
        var initial = source.volume;
        if (fadeDuration <= 0f) { source.volume = target; yield break; }
        // Start the clock after the first rendered frame, excluding scene-load stalls.
        yield return null;
        var started = Time.realtimeSinceStartupAsDouble;
        while (true)
        {
            var progress = (float)((Time.realtimeSinceStartupAsDouble - started) / fadeDuration);
            source.volume = Mathf.Lerp(initial, target, progress);
            if (progress >= 1f) break;
            yield return null;
        }
    }

    private void ConfigureSource()
    {
        source = GetComponent<AudioSource>();
        source.spatialBlend = 0f;
        source.loop = true;
        source.playOnAwake = false;
    }

#if UNITY_EDITOR
    public void ConfigureEditor(Entry[] authoredEntries, float baseVolume, float fadeSeconds)
    {
        entries = authoredEntries ?? Array.Empty<Entry>();
        volume = Mathf.Clamp01(baseVolume);
        fadeDuration = Mathf.Max(0f, fadeSeconds);
        ConfigureSource();
        source.volume = volume;
    }
#endif
}
}
