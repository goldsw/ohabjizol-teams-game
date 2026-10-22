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
        public Button CloseButton => _closeButton;

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
            if (Application.isPlaying || UnityEditor.EditorApplication.isPlayingOrWillChangePlaymode) return;
            UnityEditor.Undo.RecordObject(this, "Capture window controls");
            _root = GetComponent<RectTransform>();
            _toolbar = Toolbar != null ? Toolbar : (_toolbar != null ? _toolbar : transform.Find("Toolbar") as RectTransform);
            _controls = Controls != null ? Controls : (_controls != null ? _controls : _toolbar?.Find("Controls") as RectTransform);
            _info = Info != null ? Info : (_info != null ? _info : transform.Find("Info") as RectTransform);
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
            EnsureControlsEditor();
#endif
        }

#if UNITY_EDITOR
        /// <summary>Explicit Edit Mode authoring; never called by runtime initialization.</summary>
        public void EnsureControlsEditor()
        {
            if (Application.isPlaying || UnityEditor.EditorApplication.isPlayingOrWillChangePlaymode)
                throw new InvalidOperationException("Window controls can only be authored in Edit Mode.");

            UnityEditor.Undo.RecordObject(this, "Author window controls");
            _root = GetComponent<RectTransform>();
            _toolbar = _toolbar != null ? _toolbar : (Toolbar != null ? Toolbar : transform.Find("Toolbar") as RectTransform);
            if (_toolbar == null) return;
            _controls = _controls != null ? _controls : (Controls != null ? Controls : _toolbar.Find("Controls") as RectTransform);
            if (_controls == null) _controls = CreateControlObjectEditor("Controls", _toolbar);
            Root = _root;
            Toolbar = _toolbar;
            Controls = _controls;
            RecordControlEditor(_controls);
            RecordControlEditor(_controls.gameObject);
            _controls.gameObject.SetActive(true);
            _controls.anchorMin = new Vector2(1f, 0f);
            _controls.anchorMax = Vector2.one;
            _controls.pivot = new Vector2(1f, 0.5f);
            _controls.sizeDelta = new Vector2(80f, 0f);
            _controls.anchoredPosition = Vector2.zero;
            SaveControlEditor(_controls);
            SaveControlEditor(_controls.gameObject);

            _closeButton = _closeButton != null ? _closeButton : _controls.Find("Close")?.GetComponent<Button>();
            _minimizeButton = _minimizeButton != null ? _minimizeButton : _controls.Find("Minimize")?.GetComponent<Button>();
            if (_closeButton == null) _closeButton = CreateControlButtonEditor("Close", null);
            if (_minimizeButton == null) _minimizeButton = CreateControlButtonEditor("Minimize", _closeButton);
            AuthorControlButtonEditor(_minimizeButton, "-", 0f, 0.5f);
            AuthorControlButtonEditor(_closeButton, "X", 0.5f, 1f);

            _titleLabel = _titleLabel != null ? _titleLabel : _toolbar.Find("Title")?.GetComponent<Text>();
            ReserveControlsEditor(_titleLabel != null ? _titleLabel.rectTransform : null);
            ReserveControlsEditor(_toolbar.Find("Drag") as RectTransform);
            _authoredInitialized = false;
            SaveControlEditor(this);
        }

        static void RecordControlEditor(UnityEngine.Object target)
        {
            UnityEditor.Undo.RecordObject(target, "Author window controls");
        }

        static void SaveControlEditor(UnityEngine.Object target)
        {
            UnityEditor.EditorUtility.SetDirty(target);
            UnityEditor.PrefabUtility.RecordPrefabInstancePropertyModifications(target);
        }

        static RectTransform CreateControlObjectEditor(string name, Transform parent)
        {
            var instance = new GameObject(name, typeof(RectTransform));
            UnityEditor.Undo.RegisterCreatedObjectUndo(instance, "Create window control");
            UnityEditor.Undo.SetTransformParent(instance.transform, parent, "Parent window control");
            var rect = (RectTransform)instance.transform;
            rect.localScale = Vector3.one;
            rect.localRotation = Quaternion.identity;
            return rect;
        }

        Button CreateControlButtonEditor(string name, Button source)
        {
            var rect = CreateControlObjectEditor(name, _controls);
            var image = UnityEditor.Undo.AddComponent<Image>(rect.gameObject);
            var sourceImage = source != null ? source.targetGraphic as Image : null;
            image.color = sourceImage != null ? sourceImage.color : Color.white;
            image.sprite = sourceImage != null ? sourceImage.sprite : null;
            image.raycastTarget = true;
            var button = UnityEditor.Undo.AddComponent<Button>(rect.gameObject);
            button.targetGraphic = image;
            if (source != null) button.colors = source.colors;
            else UiKit.Paint(button, CloseNormal);
            var navigation = button.navigation;
            navigation.mode = Navigation.Mode.None;
            button.navigation = navigation;
            return button;
        }

        static void AuthorControlButtonEditor(Button button, string glyph, float left, float right)
        {
            var rect = (RectTransform)button.transform;
            RecordControlEditor(rect);
            RecordControlEditor(button);
            RecordControlEditor(button.gameObject);
            rect.anchorMin = new Vector2(left, 0f);
            rect.anchorMax = new Vector2(right, 1f);
            rect.offsetMin = rect.offsetMax = Vector2.zero;
            button.enabled = true;
            button.gameObject.SetActive(true);
            var label = button.GetComponentInChildren<Text>(true);
            if (label == null)
            {
                var labelRect = CreateControlObjectEditor("Label", rect);
                label = UnityEditor.Undo.AddComponent<Text>(labelRect.gameObject);
                label.color = TitleColor;
                label.fontSize = 20;
            }
            RecordControlEditor(label);
            RecordControlEditor(label.rectTransform);
            RecordControlEditor(label.gameObject);
            if (label.transform.parent != rect)
                UnityEditor.Undo.SetTransformParent(label.transform, rect, "Parent window glyph");
            label.text = glyph;
            label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            label.alignment = TextAnchor.MiddleCenter;
            label.horizontalOverflow = HorizontalWrapMode.Overflow;
            label.verticalOverflow = VerticalWrapMode.Overflow;
            label.raycastTarget = false;
            label.enabled = true;
            label.gameObject.SetActive(true);
            label.rectTransform.localScale = Vector3.one;
            label.rectTransform.localRotation = Quaternion.identity;
            UiKit.Stretch(label.rectTransform, 0f);
            SaveControlEditor(rect);
            SaveControlEditor(button);
            SaveControlEditor(button.gameObject);
            SaveControlEditor(label);
            SaveControlEditor(label.rectTransform);
            SaveControlEditor(label.gameObject);
        }

        static void ReserveControlsEditor(RectTransform rect)
        {
            if (rect == null) return;
            RecordControlEditor(rect);
            var offset = rect.offsetMax;
            offset.x = -80f;
            rect.offsetMax = offset;
            SaveControlEditor(rect);
        }
#endif

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

            if (GetComponent<WindowPointerFocus>() == null) gameObject.AddComponent<WindowPointerFocus>();
            OnBuilt();
            CaptureEditorReferences();
#if UNITY_EDITOR
            EnsureControlsEditor();
#endif
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
