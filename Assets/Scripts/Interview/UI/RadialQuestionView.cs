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
        [SerializeField] Button[] _questions = new Button[5];
        [SerializeField] float _startAngle = 90f;
        [SerializeField, Min(0f)] float _radius = 180f;
        [SerializeField] RadialQuestionWindow _window;
        [SerializeField] RadialQuestionLauncher _fixedLauncher;
        float _phase;
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
            _questions = questions == null ? new Button[0] : (Button[])questions.Clone();
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
            _phase = Mathf.Repeat(_startAngle, 360f);
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
            _phase = Mathf.Repeat(_startAngle, 360f);
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
            Vector3 worldCenter = _portrait.TransformPoint(_portrait.rect.center);
            Vector3 center = _optionsRoot.InverseTransformPoint(worldCenter);
            float step = 360f / _questions.Length;
            for (int i = 0; i < _questions.Length; i++)
            {
                if (!_questions[i]) continue;
                var rect = _questions[i].transform as RectTransform;
                if (!rect) continue;
                rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(.5f, .5f);
                float angle = (_phase - step * i) * Mathf.Deg2Rad;
                rect.localPosition = new Vector3(center.x + Mathf.Cos(angle) * _radius,
                    center.y + Mathf.Sin(angle) * _radius, center.z);
                rect.localRotation = Quaternion.identity;
            }
        }
    }
}
