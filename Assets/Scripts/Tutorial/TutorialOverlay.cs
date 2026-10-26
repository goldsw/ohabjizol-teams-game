using System;
using System.Collections.Generic;
using System.Globalization;
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
        [SerializeField] Sprite supervisorTalk;
        [SerializeField, Min(1f)] float charactersPerSecond = 35f;
        const float SpeechFrameDuration = .12f;
        string _dialogue = string.Empty;
        int[] _textElements = Array.Empty<int>();
        int _visibleElements;
        float _speechElapsed;
        bool _typing;
        [SerializeField] float fadeDuration = .25f;
        [SerializeField, Min(.01f)] float highlightBlinkPeriod = .8f;
        CanvasGroup _highlightFade;
        float _blinkElapsed;
        readonly List<RectTransform> _targets = new List<RectTransform>();
        readonly List<RectTransform> _emphasis = new List<RectTransform>();
        readonly Vector3[] _corners = new Vector3[4];
        readonly RectTransform[] _screenBounds = new RectTransform[1];
        EventSystem _events;
        GameObject _previousSelection;
        bool _previousNavigation;
        bool _explanation;
        bool _released;
        bool _continueWithAnyKey;
        bool _targetInputEnabled = true;
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
            CompleteSpeech();
            if (_events)
            {
                _events.sendNavigationEvents = _previousNavigation;
                _events.SetSelectedGameObject(_previousSelection && _previousSelection.activeInHierarchy ? _previousSelection : null);
            }
        }

        public void Show(string text, bool explanation, params RectTransform[] targets)
        {
            _continueWithAnyKey = false;
            _targetInputEnabled = true;
            _dialogue = text ?? string.Empty;
            _textElements = StringInfo.ParseCombiningCharacters(_dialogue);
            _visibleElements = 0;
            _speechElapsed = 0;
            _typing = _textElements.Length > 0;
            caption.text = string.Empty;
            SetIdlePortrait();
            _explanation = explanation;
            _released = false;
            _enteredFrame = Time.frameCount;
            _targets.Clear();
            _emphasis.Clear();
            _blinkElapsed = 0;
            TargetsAvailable = true;
            if (targets != null) _targets.AddRange(targets);
            if (next) next.gameObject.SetActive(false);
            if (continueHint) continueHint.gameObject.SetActive(explanation);
            if (_events) _events.SetSelectedGameObject(null);
        }

        public void SetContinueWithAnyKey(bool enabled)
        {
            _continueWithAnyKey = enabled;
            if (continueHint)
                continueHint.text = enabled ? "아무 키나 눌러 계속 . . ." : "아무 곳이나 눌러 계속 . . .";
        }

        public void SetTargetInputEnabled(bool enabled) => _targetInputEnabled = enabled;

        /// <summary>Outlines only these rects while the spotlight still reveals every Show target.</summary>
        public void SetEmphasis(params RectTransform[] targets)
        {
            _emphasis.Clear();
            if (targets != null) foreach (var target in targets) if (target) _emphasis.Add(target);
        }

        public void SetSupervisor(bool visible)
        {
            _supervisorAlpha = visible ? 1 : 0;
            if (!visible) CompleteSpeech();
        }

        void SetIdlePortrait()
        {
            if (supervisorPortrait) supervisorPortrait.sprite = supervisorIdle;
        }

        void CompleteSpeech()
        {
            _typing = false;
            if (caption) caption.text = _dialogue;
            SetIdlePortrait();
        }

        void UpdateSpeech()
        {
            if (!_typing) return;
            _speechElapsed += Time.unscaledDeltaTime;
            int count = Mathf.Min(_textElements.Length,
                Mathf.FloorToInt(_speechElapsed * Mathf.Max(1f, charactersPerSecond)));
            if (count >= _textElements.Length) { CompleteSpeech(); return; }
            if (count != _visibleElements)
            {
                _visibleElements = count;
                caption.text = _dialogue.Substring(0, _textElements[count]);
            }
            if (supervisorPortrait)
                supervisorPortrait.sprite = _supervisorAlpha > 0 && supervisorTalk &&
                    Mathf.FloorToInt(_speechElapsed / SpeechFrameDuration) % 2 != 0
                    ? supervisorTalk : supervisorIdle;
        }

        public bool IsRaycastLocationValid(Vector2 point, Camera camera)
        {
            if (!_released || _explanation || !_targetInputEnabled) return true;
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
            if (_typing)
            {
                CompleteSpeech();
                _released = false;
                _enteredFrame = Time.frameCount;
                return;
            }
            _released = false;
            ContinueRequested?.Invoke();
        }

        static bool Held() => (Mouse.current != null && (Mouse.current.leftButton.isPressed || Mouse.current.rightButton.isPressed))
            || (Keyboard.current != null && Keyboard.current.anyKey.isPressed)
            || (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.isPressed)
            || (Gamepad.current != null && (Gamepad.current.buttonSouth.isPressed || Gamepad.current.startButton.isPressed));

        void LateUpdate()
        {
            UpdateSpeech();
            CaptureEventSystem();
            if (!_released && Time.frameCount > _enteredFrame && !Held()) _released = true;
            if (_continueWithAnyKey && _released &&
                ((Keyboard.current != null && Keyboard.current.anyKey.wasPressedThisFrame)
                || (Gamepad.current != null && (Gamepad.current.buttonSouth.wasPressedThisFrame
                    || Gamepad.current.startButton.wasPressedThisFrame))))
                Continue();
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
            Follow(highlight, EmphasisVisible() ? _emphasis : _targets);
            UpdateBlink();
            UpdateSpotlight(true);
        }

        bool EmphasisVisible()
        {
            if (_emphasis.Count == 0) return false;
            foreach (var target in _emphasis)
                if (!target || !target.gameObject.activeInHierarchy) return false;
            return true;
        }

        void UpdateBlink()
        {
            if (!_highlightFade && !highlight.TryGetComponent(out _highlightFade))
                _highlightFade = highlight.gameObject.AddComponent<CanvasGroup>();
            _blinkElapsed += Time.unscaledDeltaTime;
            float half = highlightBlinkPeriod * .5f;
            _highlightFade.alpha = Mathf.Lerp(1f, 0f, Mathf.PingPong(_blinkElapsed / half, 1f));
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

        public void SetSupervisorSpeechEditor(Sprite idle, Sprite talk)
        {
            if (!talk) throw new InvalidOperationException("Tutorial supervisor Talk is required.");
            SetSupervisorIdleEditor(idle);
            supervisorTalk = talk;
            UnityEditor.EditorUtility.SetDirty(this);
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
