using System;
using Ohabjizol.Interview;
using Ohabjizol.UI;
using UnityEngine;
using UnityEngine.Events;
namespace Ohabjizol.Presentation
{
    [Serializable]
    public sealed class DesktopAppBinding
    {
        [SerializeField] DesktopIcon icon;
        [SerializeField] InfoWindow window;
        [SerializeReference] DesktopAppHandler activation;
        [SerializeField] UnityEvent onActivated = new UnityEvent();
        [SerializeField] bool initiallyOpen;
        public DesktopIcon Icon => icon;
        public InfoWindow Window => window;
        public bool InitiallyOpen { get => initiallyOpen; set => initiallyOpen = value; }
        public bool HasAuthoredContent => icon != null && window != null &&
            (activation == null || activation.HasAuthoredContent(window));
        public DesktopAppBinding(DesktopIcon icon, InfoWindow window, DesktopAppHandler activation = null)
        {
            this.icon = icon;
            this.window = window;
            this.activation = activation;
        }
        public void Activate(DesktopAppContext context)
        {
            // Custom apps open their authored window without a specialized handler.
            if (activation == null) window.Open();
            else activation.Activate(window, context);
            onActivated?.Invoke();
        }
        public void Attach(DesktopAppContext context) { activation?.Attach(window, context); }
        public void Detach(DesktopAppContext context) { activation?.Detach(window, context); }
    }
    public sealed class DesktopAppContext
    {
        public SettingsApplier Settings { get; }
        public Func<float> ConnectDelay { get; }
        public Action<StageId> Launch { get; }
        public Action Refresh { get; }
        public DesktopAppContext(SettingsApplier settings, Func<float> delay, Action<StageId> launch, Action refresh)
        {
            Settings = settings;
            ConnectDelay = delay;
            Launch = launch;
            Refresh = refresh;
        }
    }
    [Serializable]
    public abstract class DesktopAppHandler
    {
        public abstract bool HasAuthoredContent(InfoWindow window);
        public virtual void Activate(InfoWindow window, DesktopAppContext context) { window.Open(); }
        public virtual void Attach(InfoWindow window, DesktopAppContext context) { }
        public virtual void Detach(InfoWindow window, DesktopAppContext context) { }
    }
    [Serializable]
    public sealed class ChungkangCodeAppHandler : DesktopAppHandler
    {
        public override bool HasAuthoredContent(InfoWindow window) =>
            window is ChungkangCodeWindow code && code.HasAuthoredContent;
        public override void Activate(InfoWindow window, DesktopAppContext context)
        {
            var code = (ChungkangCodeWindow)window;
            code.Open();
            code.Connect(context.ConnectDelay(), context.Launch);
        }
        public override void Attach(InfoWindow window, DesktopAppContext context)
        {
            var code = (ChungkangCodeWindow)window;
            code.RunReset -= context.Refresh;
            code.RunReset += context.Refresh;
        }
        public override void Detach(InfoWindow window, DesktopAppContext context)
        {
            if (window is ChungkangCodeWindow code && context != null) code.RunReset -= context.Refresh;
        }
    }
    [Serializable]
    public sealed class SettingsAppHandler : DesktopAppHandler
    {
        public override bool HasAuthoredContent(InfoWindow window) =>
            window is SettingsWindow settings && settings.HasAuthoredContent;
        public override void Attach(InfoWindow window, DesktopAppContext context) =>
            ((SettingsWindow)window).Bind(context.Settings, context.Refresh);
        public override void Detach(InfoWindow window, DesktopAppContext context)
        {
            if (window is SettingsWindow settings && context != null) settings.Unbind();
        }
    }
    [Serializable]
    public sealed class TutorialAppHandler : DesktopAppHandler
    {
        public override bool HasAuthoredContent(InfoWindow window) =>
            window is TutorialWindow tutorial && tutorial.HasAuthoredContent;
    }
}