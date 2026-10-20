using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Ohabjizol.UI
{
    [DisallowMultipleComponent]
    public sealed class ButtonActiveBackground : MonoBehaviour, ISelectHandler, IDeselectHandler
    {
        [SerializeField] Image _background;
        [SerializeField] Button _button;
        [SerializeField] bool _hasOverride;
        [SerializeField] bool _activeState;
        [SerializeField] GameObject _visibleContent;
        [SerializeField] bool _tracksVisibleContent;
        [SerializeField] Image _sourceIcon;
        [SerializeField] Image _iconVisual;
        [SerializeField] Sprite _lastSourceSprite;
        bool _selected;

        public static ButtonActiveBackground Ensure(Button button)
        {
            if (!button) return null;
            var view = button.GetComponent<ButtonActiveBackground>();
            if (!view) view = button.gameObject.AddComponent<ButtonActiveBackground>();
            view._button = button;
            if (!view._background)
            {
                // Preserve a root sprite above the background without changing its hit area.
                var rootImage = button.image;
                if (rootImage && rootImage.transform == button.transform && rootImage.sprite &&
                    rootImage.sprite.texture != Texture2D.whiteTexture && rootImage.type == Image.Type.Simple &&
                    (rootImage.preserveAspect || (rootImage.sprite.rect.width > 4f && rootImage.sprite.rect.height > 4f &&
                     rootImage.sprite.name != "UISprite" && rootImage.sprite.name != "Background")))
                {
                    var visualObject = new GameObject("ButtonIcon", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(LayoutElement));
                    visualObject.transform.SetParent(button.transform, false);
                    visualObject.GetComponent<LayoutElement>().ignoreLayout = true;
                    var visual = visualObject.GetComponent<Image>();
                    visual.sprite = rootImage.sprite;
                    if (rootImage.overrideSprite != rootImage.sprite) visual.overrideSprite = rootImage.overrideSprite;
                    view._sourceIcon = rootImage;
                    view._iconVisual = visual;
                    view._lastSourceSprite = rootImage.sprite;
                    visual.color = rootImage.color;
                    visual.material = rootImage.material;
                    visual.type = rootImage.type;
                    visual.preserveAspect = rootImage.preserveAspect;
                    visual.fillCenter = rootImage.fillCenter;
                    visual.fillMethod = rootImage.fillMethod;
                    visual.fillAmount = rootImage.fillAmount;
                    visual.fillClockwise = rootImage.fillClockwise;
                    visual.fillOrigin = rootImage.fillOrigin;
                    visual.pixelsPerUnitMultiplier = rootImage.pixelsPerUnitMultiplier;
                    visual.raycastTarget = false;
                    UiKit.Stretch(visual.rectTransform, 0f);
                    visual.transform.SetAsFirstSibling();
                    if (button.targetGraphic == rootImage) button.targetGraphic = visual;
                    rootImage.color = Color.clear;
                }
                var surface = new GameObject("ActiveBackground", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(LayoutElement));
                surface.transform.SetParent(button.transform, false);
                surface.GetComponent<LayoutElement>().ignoreLayout = true;
                view._background = surface.GetComponent<Image>();
                view._background.color = new Color(1f, 1f, 1f, .24f);
    #if UNITY_EDITOR
            // Recover references for icons authored before sprite tracking was introduced.
            if (!Application.isPlaying && !view._sourceIcon)
            {
                var source = button.GetComponent<Image>();
                for (int i = 0; source && i < button.transform.childCount; i++)
                {
                    var child = button.transform.GetChild(i);
                    if (child.name != "ButtonIcon") continue;
                    var visual = child.GetComponent<Image>();
                    if (!visual || visual == source || button.targetGraphic != visual) continue;
                    view._sourceIcon = source;
                    view._iconVisual = visual;
                    view._lastSourceSprite = visual.sprite;
                    break;
                }
            }
#endif
            view._background.raycastTarget = false;
                UiKit.Stretch(view._background.rectTransform, 0f);
                surface.transform.SetAsFirstSibling();
            }
#if UNITY_EDITOR
            // Recover references for icons authored before sprite tracking was introduced.
            if (!Application.isPlaying && !view._sourceIcon)
            {
                var source = button.GetComponent<Image>();
                for (int i = 0; source && i < button.transform.childCount; i++)
                {
                    var child = button.transform.GetChild(i);
                    if (child.name != "ButtonIcon") continue;
                    var visual = child.GetComponent<Image>();
                    if (!visual || visual == source || button.targetGraphic != visual) continue;
                    view._sourceIcon = source;
                    view._iconVisual = visual;
                    view._lastSourceSprite = visual.sprite;
                    break;
                }
            }
#endif
            view._background.raycastTarget = false;
            if (button.transition == Selectable.Transition.ColorTint)
            {
                var colors = button.colors;
                colors.highlightedColor = colors.selectedColor = colors.disabledColor = colors.normalColor;
                button.colors = colors;
            }
            view.Refresh();
            return view;
        }

        public void TrackVisibleContent(GameObject content)
        {
            _visibleContent = content;
            _tracksVisibleContent = content != null;
            Refresh();
        }

        void LateUpdate()
        {
            if (_sourceIcon && _iconVisual && _sourceIcon.sprite != _lastSourceSprite)
            {
                if (_iconVisual.overrideSprite == _lastSourceSprite) _iconVisual.overrideSprite = null;
                _lastSourceSprite = _sourceIcon.sprite;
                _iconVisual.sprite = _lastSourceSprite;
            }
            if (_tracksVisibleContent) Refresh();
        }

        public void SetActiveState(bool active)
        {
            _hasOverride = true;
            _activeState = active;
            Refresh();
        }

        public void ClearActiveStateOverride() { _hasOverride = false; Refresh(); }
        public void OnSelect(BaseEventData eventData) { _selected = true; Refresh(); }
        public void OnDeselect(BaseEventData eventData) { _selected = false; Refresh(); }
        void OnEnable()
        {
            _selected = EventSystem.current && EventSystem.current.currentSelectedGameObject == gameObject;
            Refresh();
        }
        void OnDisable() { _selected = false; if (_background) _background.enabled = false; }
        void Refresh()
        {
            bool active = _tracksVisibleContent ? _visibleContent && _visibleContent.activeInHierarchy
                : (_hasOverride ? _activeState : _selected);
            if (_background) _background.enabled = isActiveAndEnabled && active;
        }
    }
}
