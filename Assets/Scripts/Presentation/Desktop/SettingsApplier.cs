using System.Collections.Generic;
using Ohabjizol.Interview;
using Ohabjizol.Managers;
using UnityEngine;
using DisplaySize = Ohabjizol.Managers.Resolution;

namespace Ohabjizol.Presentation
{
    /// <summary>
    /// Engine side of <see cref="SettingsManager"/>. Wires its actions to AudioListener and Screen,
    /// and keeps the choices in PlayerPrefs. Display changes only happen through
    /// <see cref="ApplyDisplay"/> or a stored display choice on entry, never from the launch size.
    /// </summary>
    public sealed class SettingsApplier
    {
        const string Prefix = "ohabjizol.settings.";
        const string VolumeKey = Prefix + "volume";
        const string WidthKey = Prefix + "width";
        const string HeightKey = Prefix + "height";
        const string ModeKey = Prefix + "mode";

        static readonly DisplaySize[] Choices =
        {
            new DisplaySize(1280, 720),
            new DisplaySize(1600, 900),
            new DisplaySize(1920, 1080)
        };

        readonly SettingsManager _settings;
        bool _batch;

        SettingsApplier(SettingsManager settings)
        {
            _settings = settings;
            _settings.ChangeVolume = ApplyVolume;
            _settings.ChangeResolution = _ => ApplyScreen();
            _settings.ChangeWindowMode = _ => ApplyScreen();
        }

        public static IReadOnlyList<DisplaySize> Resolutions
        {
            get { return Choices; }
        }

        public SettingsManager Settings
        {
            get { return _settings; }
        }

        /// <summary>
        /// Reads stored choices and applies them. Without a stored display choice, only volume is applied.
        /// </summary>
        public static SettingsApplier LoadAndApply()
        {
            float volume = PlayerPrefs.GetFloat(VolumeKey, 1f);
            bool hasDisplay = PlayerPrefs.HasKey(WidthKey) && PlayerPrefs.HasKey(HeightKey) && PlayerPrefs.HasKey(ModeKey);

            DisplaySize size;
            WindowMode mode;
            if (hasDisplay)
            {
                size = Choices[IndexOf(PlayerPrefs.GetInt(WidthKey), PlayerPrefs.GetInt(HeightKey))];
                mode = PlayerPrefs.GetInt(ModeKey) == (int)WindowMode.Windowed ? WindowMode.Windowed : WindowMode.Fullscreen;
            }
            else
            {
                size = Choices[IndexOf(Screen.width, Screen.height)];
                mode = Screen.fullScreenMode == FullScreenMode.Windowed ? WindowMode.Windowed : WindowMode.Fullscreen;
            }

            var applier = new SettingsApplier(new SettingsManager(volume, size, mode));
            if (hasDisplay)
            {
                applier._batch = true;
                applier._settings.RaiseStored();
                applier._batch = false;
                applier.ApplyScreen();
            }
            else
            {
                applier.ApplyVolume(applier._settings.Volume);
            }

            return applier;
        }

        /// <summary>Nearest listed resolution by area.</summary>
        public static int IndexOf(int width, int height)
        {
            int best = 0;
            long bestGap = long.MaxValue;
            long area = (long)width * height;
            for (int i = 0; i < Choices.Length; i++)
            {
                long gap = System.Math.Abs((long)Choices[i].Width * Choices[i].Height - area);
                if (gap < bestGap)
                {
                    bestGap = gap;
                    best = i;
                }
            }

            return best;
        }

        public void SetVolume(float volume)
        {
            _settings.SetVolume(volume);
            PlayerPrefs.SetFloat(VolumeKey, _settings.Volume);
        }

        public void ApplyDisplay(DisplaySize size, WindowMode mode)
        {
            _batch = true;
            _settings.SetWindowMode(mode);
            _settings.SetResolution(size);
            _batch = false;
            ApplyScreen();

            PlayerPrefs.SetInt(WidthKey, size.Width);
            PlayerPrefs.SetInt(HeightKey, size.Height);
            PlayerPrefs.SetInt(ModeKey, (int)mode);
            PlayerPrefs.Save();
        }

        public void Flush()
        {
            PlayerPrefs.Save();
        }

        public static void ResetProgress()
        {
            StageProgressStore.ResetAll();
        }

        public static void ShutDown()
        {
            PlayerPrefs.Save();
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        void ApplyVolume(float volume)
        {
            AudioListener.volume = volume;
        }

        void ApplyScreen()
        {
            if (_batch)
                return;

            DisplaySize size = _settings.Resolution;
            FullScreenMode mode = _settings.WindowMode == WindowMode.Windowed
                ? FullScreenMode.Windowed
                : FullScreenMode.FullScreenWindow;
            Screen.SetResolution(size.Width, size.Height, mode);
        }
    }
}
