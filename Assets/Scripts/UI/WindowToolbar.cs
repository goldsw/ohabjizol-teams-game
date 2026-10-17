using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Ohabjizol.UI
{
    /// <summary>
    /// Draggable window chrome. Toolbar, right-side controls, then the info area underneath.
    /// Close hides the object. The last anchored position stays for the next open.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(RectTransform))]
    public class WindowToolbar : MonoBehaviour
    {
        public const float ToolbarHeight = 40f;

        static readonly Color ToolbarColor = new Color(0.16f, 0.17f, 0.19f, 1f);
        static readonly Color InfoColor = new Color32(200, 208, 216, 255);
        static readonly Color TitleColor = new Color(0.93f, 0.95f, 0.92f, 1f);
        static readonly Color CloseNormal = new Color(0.28f, 0.29f, 0.32f, 1f);
        static readonly Color CloseHover = new Color(0.78f, 0.22f, 0.20f, 1f);
        static readonly Color ClosePressed = new Color(0.55f, 0.12f, 0.12f, 1f);

        bool _built;
        bool _initialStateApplied;
        string _title = string.Empty;
        [SerializeField] Text _titleLabel;
        [SerializeField] RectTransform _root;
        [SerializeField] RectTransform _toolbar;
        [SerializeField] RectTransform _controls;
        [SerializeField] RectTransform _info;
        [SerializeField] Button _closeButton;
        [SerializeField] Button _minimizeButton;
        [SerializeField] WindowDragHandle _dragHandle;
        bool _authoredInitialized;
        public bool IsMinimized { get; private set; }

        protected virtual void Awake() { InitializeAuthored(); }

        public void InitializeAuthored()
        {
            if (_authoredInitialized) return;
            Root = _root != null ? _root : GetComponent<RectTransform>();
            Toolbar = _toolbar;
            Controls = _controls;
            Info = _info;
            if (Toolbar == null || Info == null) return;
            _authoredInitialized = true;
            _built = true;
            if (_titleLabel != null) _title = _titleLabel.text;
            if (_closeButton != null)
            {
                _closeButton.onClick.RemoveListener(Close);
                _closeButton.onClick.AddListener(Close);
            }
            if (_minimizeButton != null)
            {
                _minimizeButton.onClick.RemoveListener(Minimize);
                _minimizeButton.onClick.AddListener(Minimize);
            }
            if (_dragHandle != null) _dragHandle.Attach(this);
            OnAuthoredInitialized();
        }

        protected virtual void OnAuthoredInitialized() { }

        public virtual void CaptureEditorReferences()
        {
#if UNITY_EDITOR
            _root = GetComponent<RectTransform>();
            _toolbar = Toolbar;
            _controls = Controls;
            _info = Info;
            if (_toolbar != null)
            {
                _dragHandle = _toolbar.GetComponentInChildren<WindowDragHandle>(true);
                if (_titleLabel == null)
                    _titleLabel = _toolbar.Find("Title")?.GetComponent<Text>();
            }
            if (_controls != null)
            {
                _closeButton = _controls.Find("Close")?.GetComponent<Button>();
                _minimizeButton = _controls.Find("Minimize")?.GetComponent<Button>();
            }
#endif
        }

        public virtual void Minimize()
        {
            InitializeAuthored();
            if (!IsOpen) return;
            IsMinimized = true;
            gameObject.SetActive(false);
        }

        public virtual void Restore()
        {
            InitializeAuthored();
            bool minimized = IsMinimized;
            IsMinimized = false;
            if (Root == null) return;
            if (!minimized) { Open(); return; }
            gameObject.SetActive(true);
            BringToFront();
            if (Opened != null) Opened(this);
        }

        public virtual void Maximize() { Restore(); }

        public RectTransform Root { get; private set; }

        public RectTransform Toolbar { get; private set; }

        public RectTransform Controls { get; private set; }

        public RectTransform Info { get; private set; }

        public string Title
        {
            get { return _title; }
            set
            {
                _title = value ?? string.Empty;
                if (_titleLabel != null)
                    _titleLabel.text = _title;
            }
        }

        public bool IsOpen
        {
            get { return gameObject.activeSelf; }
        }

        public event Action<WindowToolbar> Foregrounded;

        public event Action<WindowToolbar> Opened;

        public event Action<WindowToolbar> Closed;

        public void Build(RectTransform parent, string title, Vector2 size, Vector2 anchoredPosition)
        {
            if (!Application.isEditor || Application.isPlaying)
                throw new InvalidOperationException("Window Build is only available for explicit Editor baking.");
            if (_built || parent == null)
                return;

            _built = true;
            _title = title ?? string.Empty;
            Root = GetComponent<RectTransform>();
            Root.SetParent(parent, false);
            Root.localScale = Vector3.one;
            Root.localRotation = Quaternion.identity;
            Root.anchorMin = new Vector2(0.5f, 0.5f);
            Root.anchorMax = new Vector2(0.5f, 0.5f);
            Root.pivot = new Vector2(0.5f, 0.5f);
            Root.sizeDelta = new Vector2(
                Mathf.Max(ToolbarHeight, size.x),
                Mathf.Max(ToolbarHeight, size.y));
            Root.anchoredPosition = anchoredPosition;
            gameObject.name = string.IsNullOrEmpty(_title) ? GetType().Name : _title;

            Image infoImage = UiKit.Image(Root, "Info", InfoColor);
            infoImage.raycastTarget = true;
            Info = infoImage.rectTransform;
            Info.anchorMin = Vector2.zero;
            Info.anchorMax = Vector2.one;
            Info.offsetMin = Vector2.zero;
            Info.offsetMax = new Vector2(0f, -ToolbarHeight);

            Image toolbarImage = UiKit.Image(Root, "Toolbar", ToolbarColor);
            toolbarImage.raycastTarget = true;
            Toolbar = toolbarImage.rectTransform;
            Toolbar.anchorMin = new Vector2(0f, 1f);
            Toolbar.anchorMax = new Vector2(1f, 1f);
            Toolbar.pivot = new Vector2(0.5f, 1f);
            Toolbar.anchoredPosition = Vector2.zero;
            Toolbar.sizeDelta = new Vector2(0f, ToolbarHeight);

            Image dragPlate = UiKit.Image(Toolbar, "Drag", Color.clear);
            dragPlate.raycastTarget = true;
            UiKit.Stretch(dragPlate.rectTransform, 0f);
            dragPlate.rectTransform.offsetMax = new Vector2(-ToolbarHeight, 0f);
            dragPlate.gameObject.AddComponent<WindowDragHandle>().Attach(this);

            _titleLabel = UiKit.Text(Toolbar, "Title", 20, FontStyle.Bold, TextAnchor.MiddleLeft);
            _titleLabel.text = _title;
            _titleLabel.color = TitleColor;
            UiKit.Stretch(_titleLabel.rectTransform, 0f);
            _titleLabel.rectTransform.offsetMin = new Vector2(12f, 0f);
            _titleLabel.rectTransform.offsetMax = new Vector2(-ToolbarHeight, 0f);

            var controlsObject = new GameObject("Controls");
            controlsObject.transform.SetParent(Toolbar, false);
            Controls = controlsObject.AddComponent<RectTransform>();
            Controls.localScale = Vector3.one;
            Controls.anchorMin = new Vector2(1f, 0f);
            Controls.anchorMax = new Vector2(1f, 1f);
            Controls.pivot = new Vector2(1f, 0.5f);
            Controls.sizeDelta = new Vector2(ToolbarHeight, 0f);
            Controls.anchoredPosition = Vector2.zero;

            Button close = UiKit.Button(Controls, "Close", "X", CloseNormal, 20);
            UiKit.Stretch(close.GetComponent<RectTransform>(), 0f);
            ColorBlock colors = close.colors;
            colors.highlightedColor = CloseHover;
            colors.pressedColor = ClosePressed;
            colors.selectedColor = CloseNormal;
            close.colors = colors;
            close.onClick.AddListener(Close);

            if (GetComponent<WindowPointerFocus>() == null) gameObject.AddComponent<WindowPointerFocus>();
            OnBuilt();
            CaptureEditorReferences();
        }

        /// <summary>
        /// Applies the initial visibility once, after binding and event subscriptions.
        /// </summary>
        public void ApplyInitialState(bool isOpen)
        {
            InitializeAuthored();
            if (_initialStateApplied || Root == null)
                return;

            _initialStateApplied = true;
            gameObject.SetActive(false);
            if (isOpen)
                Open();
        }

        public void Open()
        {
            if (Root == null)
                return;

            if (IsMinimized) { Restore(); return; }
            bool wasOpen = IsOpen;
            gameObject.SetActive(true);
            BringToFront();
            OnOpened();
            if (!wasOpen && Opened != null)
                Opened(this);
        }

        public virtual void Close()
        {
            bool wasMinimized = IsMinimized;
            IsMinimized = false;
            if (!IsOpen && !wasMinimized)
                return;

            gameObject.SetActive(false);
            if (Closed != null)
                Closed(this);
        }

        public void Toggle()
        {
            if (IsOpen)
                Close();
            else
                Open();
        }

        public virtual void BringToFront()
        {
            if (Root == null)
                return;

            Root.SetAsLastSibling();
            Foregrounded?.Invoke(this);
        }

        protected virtual void OnBuilt()
        {
        }

        protected virtual void OnOpened()
        {
        }
    }
}
