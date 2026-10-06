using System;

namespace Ohabjizol.Managers
{
    /// <summary>
    /// Stores volume, resolution, and window-mode choices and raises actions.
    /// Does not apply audio, resolution, or window mode.
    /// The caller points each action at the owning manager.
    /// </summary>
    public sealed class SettingsManager
    {
        private float _volume;
        private Resolution _resolution;
        private WindowMode _windowMode;

        public SettingsManager()
            : this(1f, new Resolution(1280, 720), WindowMode.Windowed)
        {
        }

        public SettingsManager(float volume, Resolution resolution, WindowMode windowMode)
        {
            _volume = ClampGauge(volume);
            _resolution = resolution;
            _windowMode = RequireMode(windowMode);
        }

        /// <summary>Stored volume choice, about 0–1. Not the applied gauge.</summary>
        public float Volume
        {
            get { return _volume; }
        }

        public Resolution Resolution
        {
            get { return _resolution; }
        }

        public WindowMode WindowMode
        {
            get { return _windowMode; }
        }

        /// <summary>Assign <see cref="SoundManager.SetVolumeGauge"/>.</summary>
        public Action<float> ChangeVolume { get; set; }

        /// <summary>Assign <see cref="WindowManager.ApplyResolution"/>.</summary>
        public Action<Resolution> ChangeResolution { get; set; }

        /// <summary>Assign <see cref="WindowManager.ApplyWindowMode"/>.</summary>
        public Action<WindowMode> ChangeWindowMode { get; set; }

        public void SetVolume(float volume)
        {
            _volume = ClampGauge(volume);
            Raise(ChangeVolume, _volume);
        }

        public void SetResolution(Resolution resolution)
        {
            _resolution = resolution;
            Raise(ChangeResolution, _resolution);
        }

        public void SetWindowMode(WindowMode mode)
        {
            _windowMode = RequireMode(mode);
            Raise(ChangeWindowMode, _windowMode);
        }

        /// <summary>
        /// Invokes the assigned actions with the stored choices.
        /// Call this after wiring so owners apply the current selection.
        /// </summary>
        public void RaiseStored()
        {
            Raise(ChangeVolume, _volume);
            Raise(ChangeResolution, _resolution);
            Raise(ChangeWindowMode, _windowMode);
        }

        private static void Raise<T>(Action<T> action, T value)
        {
            if (action != null)
                action(value);
        }

        private static WindowMode RequireMode(WindowMode mode)
        {
            if (!Enum.IsDefined(typeof(WindowMode), mode))
                throw new ArgumentOutOfRangeException(nameof(mode), "Unknown window mode.");
            return mode;
        }

        private static float ClampGauge(float gauge)
        {
            if (float.IsNaN(gauge) || float.IsInfinity(gauge))
            {
                throw new ArgumentOutOfRangeException(
                    nameof(gauge),
                    "Volume must be finite.");
            }

            if (gauge < 0f)
                return 0f;
            if (gauge > 1f)
                return 1f;
            return gauge;
        }
    }
}
