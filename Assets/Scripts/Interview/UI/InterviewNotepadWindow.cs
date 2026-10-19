using Ohabjizol.UI;
using UnityEngine;
using UnityEngine.UI;

namespace Ohabjizol.Interview
{
    public sealed class InterviewNotepadWindow : InfoWindow
    {
        [SerializeField] InputField _input;
        StageNotepad _notepad;
        InputField _listeningInput;
        bool _sendingLocal;
        string _localContent;
        bool _hasPending;
        string _pendingContent;

        public bool HasAuthoredInput => _input != null;

        protected override void Awake()
        {
            base.Awake();
            AttachInput();
            if (_notepad != null) UpdateInput(_notepad.Content);
        }

        protected override void BuildInfo(RectTransform info) { }

        public void Bind(StageNotepad notepad)
        {
            Unbind();
            _notepad = notepad;
            if (_notepad == null) return;
            _notepad.Changed += UpdateInput;
            AttachInput();
            UpdateInput(_notepad.Content);
        }

        public void Unbind()
        {
            if (_notepad != null) _notepad.Changed -= UpdateInput;
            _notepad = null;
            _hasPending = false;
            DetachInput();
        }

        void AttachInput()
        {
            DetachInput();
            if (_notepad == null || _input == null) return;
            _listeningInput = _input;
            _listeningInput.onValueChanged.RemoveListener(OnInputChanged);
            _listeningInput.onValueChanged.AddListener(OnInputChanged);
        }

        void DetachInput()
        {
            if (_listeningInput != null)
                _listeningInput.onValueChanged.RemoveListener(OnInputChanged);
            _listeningInput = null;
        }

        void OnInputChanged(string content)
        {
            if (_hasPending) return;
            _sendingLocal = true;
            _localContent = content;
            try { _notepad?.SetContent(content); }
            finally { _sendingLocal = false; }
        }

        void UpdateInput(string content)
        {
            if (_sendingLocal && content == _localContent) return;
            if (_input == null) return;
            content = content ?? string.Empty;
            if (_input.isFocused && (!(_input is InterviewImeInputField imeInput) || imeInput.HasComposition))
            {
                _pendingContent = content;
                _hasPending = true;
                return;
            }
            _hasPending = false;
            if (_input.text != content) _input.SetTextWithoutNotify(content);
        }

        void LateUpdate()
        {
            if (_hasPending && _input != null && (!_input.isFocused || (_input is InterviewImeInputField imeInput && !imeInput.HasComposition)))
                UpdateInput(_pendingContent);
        }

        void OnDestroy()
        {
            Unbind();
        }

#if UNITY_EDITOR
        public InputField AuthoredInputEditor => _input;
        public void ConfigureEditor(InputField input)
        {
            _input = input;
        }
#endif
    }
}
