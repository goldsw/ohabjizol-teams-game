using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Ohabjizol.Presentation
{
    public sealed class UiSoundPlayer : MonoBehaviour
    {
        private static UiSoundPlayer instance;
        private static int lastClickFrame = -1;
        private static double nextBlipTime = double.NegativeInfinity;

        [SerializeField] private AudioClip clickClip;
        [SerializeField] private AudioClip blipClip;
        [SerializeField] private AudioSource clickSource;
        [SerializeField] private AudioSource blipSource;
        [SerializeField, Range(0f, 1f)] private float clickVolume = 0.7f;
        [SerializeField, Range(0f, 1f)] private float blipVolume = 0.3f;
        [SerializeField, Min(0f)] private float blipOffsetSeconds;
        [SerializeField, Min(0.001f)] private float blipDuration = 0.06f;
        [SerializeField, Min(0.06f)] private float blipInterval = 0.06f;

        private readonly HashSet<Button> boundButtons = new HashSet<Button>();
        private double nextButtonScan;
        private double blipStopTime = double.PositiveInfinity;

        public bool HasAuthoredAudio => clickClip != null && blipClip != null
            && clickSource != null && blipSource != null && clickSource != blipSource;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            instance = null;
            lastClickFrame = -1;
            nextBlipTime = double.NegativeInfinity;
        }

        private void Awake()
        {
            if (instance != null && instance != this)
            {
                enabled = false;
                return;
            }

            instance = this;
            if (clickSource != null && clickSource != blipSource)
                ConfigureSource(clickSource);
            if (blipSource != null && blipSource != clickSource)
                ConfigureSource(blipSource);
        }

        private void OnEnable()
        {
            if (instance != this)
                return;

            SceneManager.sceneLoaded += OnSceneLoaded;
            ScanButtons();
        }

        private void Update()
        {
            double now = Time.unscaledTimeAsDouble;
            if (now >= blipStopTime)
                StopTextBlip();
            if (now >= nextButtonScan)
                ScanButtons();
        }

        private void ScanButtons()
        {
            boundButtons.RemoveWhere(button => button == null);
            foreach (Button button in Object.FindObjectsByType<Button>(
                FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (button.GetComponentInParent<Ohabjizol.Tutorial.TutorialOverlay>(true) != null)
                    continue;
                if (boundButtons.Add(button))
                    button.onClick.AddListener(PlayClick);
            }

            nextButtonScan = Time.unscaledTimeAsDouble + 0.25d;
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            StopTextBlip();
            ScanButtons();
        }

        public static void PlayClick()
        {
            UiSoundPlayer player = instance;
            if (player == null || !player.isActiveAndEnabled
                || player.clickClip == null || player.clickSource == null
                || player.clickSource == player.blipSource
                || lastClickFrame == Time.frameCount)
                return;

            lastClickFrame = Time.frameCount;
            player.clickSource.PlayOneShot(player.clickClip, player.clickVolume);
        }

        public static void PlayTextBlip()
        {
            UiSoundPlayer player = instance;
            if (player == null || !player.isActiveAndEnabled
                || player.blipClip == null || player.blipSource == null
                || player.blipSource == player.clickSource)
                return;

            double now = Time.unscaledTimeAsDouble;
            if (now < nextBlipTime)
                return;

            AudioClip clip = player.blipClip;
            if (clip.samples <= 0 || clip.frequency <= 0)
                return;

            float lastSampleTime = (clip.samples - 1f) / clip.frequency;
            float offset = Mathf.Clamp(player.blipOffsetSeconds, 0f, lastSampleTime);
            float duration = Mathf.Min(Mathf.Max(0.001f, player.blipDuration),
                clip.length - offset);
            nextBlipTime = now + Mathf.Max(0.06f, player.blipInterval);
            player.blipSource.Stop();
            player.blipSource.clip = clip;
            player.blipSource.volume = player.blipVolume;
            player.blipSource.time = offset;
            player.blipSource.Play();
            player.blipStopTime = now + duration;
        }

        public static void StopTextBlip()
        {
            UiSoundPlayer player = instance;
            if (player == null)
                return;

            if (player.blipSource != null && player.blipSource != player.clickSource)
                player.blipSource.Stop();
            player.blipStopTime = double.PositiveInfinity;
        }

        private void OnDisable()
        {
            if (instance != this)
                return;

            SceneManager.sceneLoaded -= OnSceneLoaded;
            foreach (Button button in boundButtons)
            {
                if (button != null)
                    button.onClick.RemoveListener(PlayClick);
            }
            boundButtons.Clear();
            StopTextBlip();
            if (clickSource != null && clickSource != blipSource)
                clickSource.Stop();
        }

        private void OnDestroy()
        {
            if (instance == this)
                instance = null;
        }

        private static void ConfigureSource(AudioSource source)
        {
            source.playOnAwake = false;
            source.loop = false;
            source.spatialBlend = 0f;
            source.volume = 1f;
            source.pitch = 1f;
            source.ignoreListenerVolume = false;
            source.ignoreListenerPause = true;
            source.outputAudioMixerGroup = null;
        }

#if UNITY_EDITOR
        public void ConfigureEditor(AudioClip click, AudioClip blip,
            AudioSource clicks, AudioSource blips, float blipOffsetSeconds = 0f)
        {
            clickClip = click;
            blipClip = blip;
            clickSource = clicks;
            blipSource = blips;
            this.blipOffsetSeconds = Mathf.Max(0f, blipOffsetSeconds);
            if (clicks != null && clicks != blips)
                ConfigureSource(clicks);
            if (blips != null && blips != clicks)
                ConfigureSource(blips);
        }
#endif
    }
}
