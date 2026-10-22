using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Ohabjizol.Interview
{
    public sealed class RadialQuestionView : MonoBehaviour
    {
        [SerializeField] Button _launcher;
        [SerializeField] RectTransform _optionsRoot;
        [SerializeField] RectTransform _portrait;
        [SerializeField] Button[] _questions = new Button[InterviewQuestionCatalog.QuestionCount];
        [SerializeField] Vector2 questionSize = new Vector2(300f, 56f);
        [SerializeField, Min(0f)] float questionSpacing = 12f;
        [SerializeField, Min(0f)] float portraitGap = 24f;
        [SerializeField] RadialQuestionWindow _window;
        [SerializeField] RadialQuestionLauncher _fixedLauncher;
        bool _expanded;
        bool _available;

        public bool IsExpanded => _expanded;
        public event Action<bool> ExpansionChanged;
        public RectTransform OptionsRoot => _optionsRoot;
        public IReadOnlyList<Button> QuestionButtons => Array.AsReadOnly(_questions ?? Array.Empty<Button>());

        public void ConfigureEditor(Button launcher, RectTransform optionsRoot,
            RectTransform portrait, Button[] questions)
        {
            if (isActiveAndEnabled && _launcher) _launcher.onClick.RemoveListener(ToggleExpanded);
            _launcher = launcher;
            _optionsRoot = optionsRoot;
            _portrait = portrait;
            _questions = new Button[questions == null ? 0 : Mathf.Min(questions.Length, InterviewQuestionCatalog.QuestionCount)];
            if (questions != null) Array.Copy(questions, _questions, _questions.Length);
            if (isActiveAndEnabled && _launcher) _launcher.onClick.AddListener(ToggleExpanded);
            ResetCollapsed();
            SetAvailable(false);
        }

        public void ConfigureWindowEditor(RadialQuestionWindow window, RadialQuestionLauncher fixedLauncher)
        {
            _window = window;
            _fixedLauncher = fixedLauncher;
        }

        void OnEnable()
        {
            if (_launcher)
            {
                _launcher.onClick.RemoveListener(ToggleExpanded);
                _launcher.onClick.AddListener(ToggleExpanded);
                _launcher.interactable = _available;
            }
        }

        void OnDisable()
        {
            if (_launcher) _launcher.onClick.RemoveListener(ToggleExpanded);
            if (_launcher) _launcher.interactable = false;
            ResetCollapsed();
        }

        public void ResetCollapsed()
        {
            CollapseContent();
        }

        public void CollapseContent()
        {
            bool changed = _expanded;
            _expanded = false;
            if (_optionsRoot) _optionsRoot.gameObject.SetActive(false);
            if (changed) ExpansionChanged?.Invoke(false);
        }

        public void SetAvailable(bool available)
        {
            _available = available;
            if (_launcher) _launcher.interactable = available && isActiveAndEnabled;
        }

        public void ShowQuestions()
        {
            if (!isActiveAndEnabled || !_available || !_optionsRoot || !_portrait) return;
            bool changed = !_expanded;
            _expanded = true;
            PositionQuestions();
            BringToFront();
            _optionsRoot.gameObject.SetActive(true);
            if (changed) ExpansionChanged?.Invoke(true);
        }

        public void ToggleExpanded()
        {
            if (_expanded) CollapseContent();
            else ShowQuestions();
        }

        void BringToFront()
        {
            // Raise the authored branch alongside WindowLayer without reparenting.
            Transform branch = _optionsRoot;
            while (branch.parent)
            {
                Transform parent = branch.parent;
                for (int i = 0; i < parent.childCount; i++)
                {
                    if (parent.GetChild(i).name != "WindowLayer") continue;
                    branch.SetAsLastSibling();
                    _optionsRoot.SetAsLastSibling();
                    return;
                }
                branch = parent;
            }
            _optionsRoot.SetAsLastSibling();
        }

        void LateUpdate()
        {
            if (!_expanded || !_optionsRoot || !_portrait) return;
            PositionQuestions();
        }

        void PositionQuestions()
        {
            if (_questions == null || _questions.Length == 0) return;
            // Follow the visible portrait rectangle, including preserve-aspect letterboxing.
            Rect visible = _portrait.rect;
            var image = _portrait.GetComponent<Image>();
            var sprite = image ? image.overrideSprite : null;
            if (image && image.preserveAspect && sprite && sprite.rect.width > 0 && sprite.rect.height > 0 && visible.width > 0 && visible.height > 0)
            {
                float aspect = sprite.rect.width / sprite.rect.height;
                if (aspect > visible.width / visible.height)
                {
                    float height = visible.width / aspect;
                    visible.y += (visible.height - height) * _portrait.pivot.y;
                    visible.height = height;
                }
                else
                {
                    float width = visible.height * aspect;
                    visible.x += (visible.width - width) * _portrait.pivot.x;
                    visible.width = width;
                }
            }
            var left = _optionsRoot.InverseTransformPoint(_portrait.TransformPoint(new Vector3(visible.xMin, visible.center.y, 0)));
            int count = Mathf.Min(_questions.Length, InterviewQuestionCatalog.QuestionCount);
            float heightPerButton = Mathf.Max(1f, questionSize.y);
            for (int i = 0; i < count; i++)
            {
                if (!_questions[i]) continue;
                var rect = _questions[i].transform as RectTransform;
                if (!rect) continue;
                rect.anchorMin = rect.anchorMax = new Vector2(.5f, .5f);
                rect.pivot = new Vector2(1f, .5f);
                rect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, Mathf.Max(1f, questionSize.x));
                rect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, heightPerButton);
                rect.localPosition = new Vector3(left.x - portraitGap,
                    left.y + ((count - 1) * .5f - i) * (heightPerButton + questionSpacing), left.z);
                rect.localRotation = Quaternion.identity;
            }
        }
    }
}
