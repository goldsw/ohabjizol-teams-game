using UnityEngine;
using UnityEngine.UI;

namespace Ohabjizol.Interview
{
    [DisallowMultipleComponent]
    public sealed class IllustratedPortfolioCard : MonoBehaviour
    {
        [SerializeField] Image artworkImage;
        [SerializeField] Text memoText;
        [SerializeField] PortfolioDisplayState state;
        [SerializeField, TextArea] string badNote = "초보적이네";
        [SerializeField, TextArea] string normalNote = "중수 정도?";
        [SerializeField, TextArea] string goodNote = "잘하는데!!";

        public static Vector2 PreferredWindowSize => new Vector2(840f, 640f);
        public Image ArtworkImage => artworkImage;
        public Text MemoText => memoText;
        public PortfolioDisplayState State => state;
        public bool HasAuthoredUi => artworkImage && artworkImage.sprite && memoText;

        void Awake()
        {
            if (artworkImage && artworkImage.sprite)
                artworkImage.sprite.texture.filterMode = FilterMode.Point;
            ApplyState(state);
        }
        void OnEnable() { FitToHost(); }

        public void ApplyState(PortfolioDisplayState value)
        {
            state = value;
            if (memoText) memoText.text = state == PortfolioDisplayState.Good ? goodNote
                : state == PortfolioDisplayState.Normal ? normalNote : badNote;
        }

        public void FitToHost()
        {
            var rect = transform as RectTransform;
            if (!rect) return;
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = rect.offsetMax = Vector2.zero;
            rect.localScale = Vector3.one;
        }

#if UNITY_EDITOR
        public void ConfigureEditor(Image artwork, Text memo, PortfolioDisplayState value)
        {
            if (Application.isPlaying)
                throw new System.InvalidOperationException("Illustrated card authoring requires Edit Mode.");
            artworkImage = artwork;
            memoText = memo;
            FitToHost();
            ApplyState(value);
        }
#endif
    }
}
