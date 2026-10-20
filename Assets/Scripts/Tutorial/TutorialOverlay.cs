using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace Ohabjizol.Tutorial
{
    [RequireComponent(typeof(Canvas), typeof(GraphicRaycaster))]
    public sealed class TutorialOverlay : MonoBehaviour, ICanvasRaycastFilter, IPointerClickHandler
    {
        [SerializeField] RectTransform highlight;
        [SerializeField] Text caption;
        [SerializeField] Button next;
        [SerializeField] Text continueHint;
        [SerializeField] Mask spotlightMask;
        [SerializeField] Image shade;
        [SerializeField] RectTransform supervisorPanel;
        [SerializeField] Image supervisorPortrait;
        [SerializeField] CanvasGroup supervisorFade;
        [SerializeField] Sprite supervisorIdle;
        [SerializeField] float fadeDuration = .25f;
        readonly List<RectTransform> _targets = new List<RectTransform>();
        readonly Vector3[] _corners = new Vector3[4];
        readonly RectTransform[] _screenBounds = new RectTransform[1];
        EventSystem _events;
        GameObject _previousSelection;
        bool _previousNavigation;
        bool _explanation;
        bool _released;
        int _enteredFrame;
        float _supervisorAlpha;
        RectTransform _video;
        public event Action ContinueRequested;
        public event Action TargetUnavailable;
        public bool InputReleased => _released;
        public bool TargetsAvailable { get; private set; } = true;
        public bool HasAuthoredUi => highlight && caption && continueHint && spotlightMask && shade &&
            supervisorPanel && supervisorPortrait && supervisorFade;

        public void Bind(RectTransform video)
        {
            _video = video;
            if (supervisorPortrait) supervisorPortrait.sprite = supervisorIdle;
        }

        void OnEnable()
        {
            CaptureEventSystem();
            if (next) next.gameObject.SetActive(false);
        }

        void CaptureEventSystem()
        {
            if (_events) return;
            _events = EventSystem.current;
            if (_events)
            {
                _previousNavigation = _events.sendNavigationEvents;
                _previousSelection = _events.currentSelectedGameObject;
                _events.sendNavigationEvents = false;
                _events.SetSelectedGameObject(null);
            }
        }

        void OnDisable()
        {
            if (_events)
            {
                _events.sendNavigationEvents = _previousNavigation;
                _events.SetSelectedGameObject(_previousSelection && _previousSelection.activeInHierarchy ? _previousSelection : null);
            }
        }

        public void Show(string text, bool explanation, params RectTransform[] targets)
        {
            caption.text = text;
            _explanation = explanation;
            _released = false;
            _enteredFrame = Time.frameCount;
            _targets.Clear();
            TargetsAvailable = true;
            if (targets != null) _targets.AddRange(targets);
            if (next) next.gameObject.SetActive(false);
            if (continueHint) continueHint.gameObject.SetActive(explanation);
            if (_events) _events.SetSelectedGameObject(null);
        }

        public void SetSupervisor(bool visible) { _supervisorAlpha = visible ? 1 : 0; }

        public bool IsRaycastLocationValid(Vector2 point, Camera camera)
        {
            if (!_released || _explanation) return true;
            foreach (var target in _targets)
                if (target && target.gameObject.activeInHierarchy && RectTransformUtility.RectangleContainsScreenPoint(target, point, camera))
                    return false;
            return true;
        }

        public void OnPointerClick(PointerEventData data)
        {
            if (data.button != PointerEventData.InputButton.Left) return;
            data.Use();
            Continue();
        }

        void Continue()
        {
            if (!_explanation || !_released || Time.frameCount <= _enteredFrame) return;
            _released = false;
            ContinueRequested?.Invoke();
        }

        static bool Held() => (Mouse.current != null && (Mouse.current.leftButton.isPressed || Mouse.current.rightButton.isPressed))
            || (Keyboard.current != null && Keyboard.current.anyKey.isPressed)
            || (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.isPressed)
            || (Gamepad.current != null && (Gamepad.current.buttonSouth.isPressed || Gamepad.current.startButton.isPressed));

        void LateUpdate()
        {
            CaptureEventSystem();
            if (!_released && Time.frameCount > _enteredFrame && !Held()) _released = true;
            if (_events)
            {
                _events.sendNavigationEvents = false;
                _events.SetSelectedGameObject(null);
            }
            if (supervisorFade)
            {
                supervisorFade.alpha = Mathf.MoveTowards(supervisorFade.alpha, _supervisorAlpha,
                    Time.unscaledDeltaTime / Mathf.Max(.01f, fadeDuration));
                supervisorFade.blocksRaycasts = false;
                supervisorFade.interactable = false;
            }
            if (_video && supervisorPanel) Follow(supervisorPanel, new[] { _video });
            if (!highlight) { UpdateSpotlight(false); return; }
            TargetsAvailable = true;
            highlight.gameObject.SetActive(_targets.Count > 0);
            if (_targets.Count == 0) { UpdateSpotlight(false); return; }
            foreach (var target in _targets)
                if (!target || !target.gameObject.activeInHierarchy)
                {
                    TargetsAvailable = false;
                    _released = false;
                    UpdateSpotlight(false);
                    TargetUnavailable?.Invoke();
                    return;
                }
            // Follow the targets without changing their hierarchy or layout order.
            Follow(highlight, _targets);
            UpdateSpotlight(true);
        }

        void UpdateSpotlight(bool revealTargets)
        {
            if (!spotlightMask || !shade) return;
            spotlightMask.enabled = revealTargets;
            if (spotlightMask.graphic) spotlightMask.graphic.enabled = revealTargets;
            if (revealTargets) Follow(spotlightMask.rectTransform, _targets);
            _screenBounds[0] = transform as RectTransform;
            Follow(shade.rectTransform, _screenBounds);
        }

        void Follow(RectTransform frame, IReadOnlyList<RectTransform> targets)
        {
            var parent = frame.parent as RectTransform;
            var canvas = GetComponent<Canvas>();
            Vector2 min = new Vector2(float.PositiveInfinity, float.PositiveInfinity);
            Vector2 max = new Vector2(float.NegativeInfinity, float.NegativeInfinity);
            foreach (var target in targets)
            {
                target.GetWorldCorners(_corners);
                var sourceCanvas = target.GetComponentInParent<Canvas>();
                Camera sourceCamera = sourceCanvas && sourceCanvas.renderMode != RenderMode.ScreenSpaceOverlay ? sourceCanvas.worldCamera : null;
                for (int i = 0; i < 4; i++)
                {
                    RectTransformUtility.ScreenPointToLocalPointInRectangle(parent,
                        RectTransformUtility.WorldToScreenPoint(sourceCamera, _corners[i]),
                        canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera, out var local);
                    min = Vector2.Min(min, local); max = Vector2.Max(max, local);
                }
            }
            frame.anchorMin = frame.anchorMax = new Vector2(.5f, .5f);
            frame.pivot = new Vector2(.5f, .5f);
            frame.localPosition = (min + max) * .5f;
            frame.sizeDelta = max - min;
        }

#if UNITY_EDITOR
        public void ConfigureSpotlightEditor(Mask cutout, Image dimmer, Text hint)
        {
            spotlightMask = cutout;
            shade = dimmer;
            continueHint = hint;
            if (next) next.gameObject.SetActive(false);
            UnityEditor.EditorUtility.SetDirty(this);
        }

        public void SetSupervisorIdleEditor(Sprite idle)
        {
            if (!HasAuthoredUi || !idle) throw new InvalidOperationException("Tutorial overlay references are incomplete.");
            supervisorIdle = idle;
            supervisorPortrait.sprite = idle;
            UnityEditor.EditorUtility.SetDirty(this);
            UnityEditor.EditorUtility.SetDirty(supervisorPortrait);
        }

        public void ConfigureEditor(RectTransform border, Text label, Button nextButton, RectTransform panel,
            Image portrait, CanvasGroup fade, Sprite idle)
        {
            highlight = border; caption = label; next = nextButton; supervisorPanel = panel;
            supervisorPortrait = portrait; supervisorFade = fade; supervisorIdle = idle;
        }
#endif
    }
}
