using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
namespace Ohabjizol.Interview
{
    public sealed class EndingBoard : MonoBehaviour
    {
        [SerializeField] RectTransform authoredRoot;
        [SerializeField] GameObject goodPanel;
        [SerializeField] GameObject normalPanel;
        [SerializeField] GameObject badPanel;
        [SerializeField] Text goodText;
        [SerializeField] Text normalText;
        [SerializeField] Text badText;
        [SerializeField] Text pendingText;
        [SerializeField] Text totalStarsText;
        [SerializeField] Button newRunButton;
        [SerializeField] int goodHalfStarThreshold = 36;
        [SerializeField] int normalHalfStarThreshold = 28;
        [SerializeField] bool useFixedKind;
        [SerializeField] EndingKind fixedKind;
        [SerializeField, TextArea] string goodTemplate = "좋음\n함께 만든 프로젝트가 멋진 결실을 맺었습니다.";
        [SerializeField, TextArea] string normalTemplate = "보통\n프로젝트를 마쳤습니다. 다음 도전이 기다립니다.";
        [SerializeField, TextArea] string badTemplate = "나쁨\n아쉬움이 남았지만 새로운 도전을 시작할 수 있습니다.";
        public GameObject GoodPanel => goodPanel;
        public GameObject NormalPanel => normalPanel;
        public GameObject BadPanel => badPanel;
        public string GoodTemplate => goodTemplate;
        public string NormalTemplate => normalTemplate;
        public string BadTemplate => badTemplate;
        public bool HasAuthoredUi => authoredRoot != null && goodPanel != null && normalPanel != null
            && badPanel != null && goodText != null && normalText != null && badText != null
            && pendingText != null && totalStarsText != null && newRunButton != null;
        void Awake()
        {
            if (!HasAuthoredUi) return;
            newRunButton.onClick.AddListener(StartNewRun);
            var run = CampaignRunStore.Load();
            bool complete = run.IsComplete;
            var ending = useFixedKind ? fixedKind : run.ResolveEnding(goodHalfStarThreshold, normalHalfStarThreshold);
            bool showArtwork = complete || useFixedKind;
            goodPanel.SetActive(showArtwork && ending == EndingKind.Good);
            normalPanel.SetActive(showArtwork && ending == EndingKind.Normal);
            badPanel.SetActive(showArtwork && ending == EndingKind.Bad);
            pendingText.gameObject.SetActive(!showArtwork);
            pendingText.text = "엔딩 준비중";
            goodText.text = goodTemplate; normalText.text = normalTemplate; badText.text = badTemplate;
            int halves = run.TotalHalfStars;
            totalStarsText.text = useFixedKind && !complete ? "" : "누적 " + (halves / 2) + (halves % 2 == 0 ? "" : ".5") + " / " + (StageExecutionPolicy.ActiveStages.Count * 5) + "별";
        }
        void OnDestroy()
        {
            if (newRunButton != null) newRunButton.onClick.RemoveListener(StartNewRun);
        }
        void StartNewRun()
        {
            CampaignRunStore.StartNewRun();
            SceneManager.LoadScene("MainMenuScene", LoadSceneMode.Single);
        }
        public void BakeAuthoredUi()
        {
#if UNITY_EDITOR
            if (authoredRoot != null) return;
            authoredRoot = ResultAuthoredUi.Canvas(transform);
            ResultAuthoredUi.Label(authoredRoot, "EndingTitle", "프로젝트의 끝", 56, new Vector2(.1f,.78f), new Vector2(.9f,.9f));
            goodPanel = Panel("GoodPanel", goodTemplate, out goodText);
            normalPanel = Panel("NormalPanel", normalTemplate, out normalText);
            badPanel = Panel("BadPanel", badTemplate, out badText);
            normalPanel.SetActive(false); badPanel.SetActive(false);
            pendingText = ResultAuthoredUi.Label(authoredRoot, "Pending", "엔딩 준비중", 44, new Vector2(.1f,.4f), new Vector2(.9f,.7f));
            pendingText.gameObject.SetActive(false);
            totalStarsText = ResultAuthoredUi.Label(authoredRoot, "TotalStars", "", 36, new Vector2(.1f,.25f), new Vector2(.9f,.35f));
            Text label;
            newRunButton = ResultAuthoredUi.Button(authoredRoot, "NewRunButton", "새 게임", out label);
            UnityEditor.EditorUtility.SetDirty(this);
#endif
        }
#if UNITY_EDITOR
        public void ConfigureArtworkEditor(Sprite good, Sprite normal, Sprite bad)
        {
            ConfigurePanelArtwork(goodPanel, good);
            ConfigurePanelArtwork(normalPanel, normal);
            ConfigurePanelArtwork(badPanel, bad);
            foreach (var story in new[] { goodText, normalText, badText })
            {
                var rect = story.rectTransform;
                UnityEditor.Undo.RecordObject(rect, "Position ending story");
                rect.anchorMin = new Vector2(.1f, .21f);
                rect.anchorMax = new Vector2(.9f, .30f);
                rect.offsetMin = Vector2.zero;
                rect.offsetMax = Vector2.zero;
                UnityEditor.EditorUtility.SetDirty(rect);
                UnityEditor.PrefabUtility.RecordPrefabInstancePropertyModifications(rect);
            }
            var starsRect = totalStarsText.rectTransform;
            UnityEditor.Undo.RecordObject(starsRect, "Position ending total stars");
            starsRect.anchorMin = new Vector2(starsRect.anchorMin.x, .02f);
            starsRect.anchorMax = new Vector2(starsRect.anchorMax.x, .08f);
            starsRect.offsetMin = new Vector2(starsRect.offsetMin.x, 0f);
            starsRect.offsetMax = new Vector2(starsRect.offsetMax.x, 0f);
            UnityEditor.EditorUtility.SetDirty(starsRect);
            UnityEditor.PrefabUtility.RecordPrefabInstancePropertyModifications(starsRect);
        }

        public void ConfigureFixedKindEditor(EndingKind kind)
        {
            if (!HasAuthoredUi) throw new System.InvalidOperationException("Ending authored UI required.");
            UnityEditor.Undo.RecordObject(this, "Configure fixed ending");
            useFixedKind = true;
            fixedKind = kind;
            UnityEditor.EditorUtility.SetDirty(this);
            UnityEditor.PrefabUtility.RecordPrefabInstancePropertyModifications(this);
            foreach (var panel in new[] { goodPanel, normalPanel, badPanel, pendingText.gameObject })
                UnityEditor.Undo.RecordObject(panel, "Preview fixed ending");
            goodPanel.SetActive(kind == EndingKind.Good);
            normalPanel.SetActive(kind == EndingKind.Normal);
            badPanel.SetActive(kind == EndingKind.Bad);
            pendingText.gameObject.SetActive(false);
            foreach (var text in new[] { goodText, normalText, badText, pendingText, totalStarsText })
                UnityEditor.Undo.RecordObject(text, "Preview fixed ending text");
            goodText.text = goodTemplate;
            normalText.text = normalTemplate;
            badText.text = badTemplate;
            pendingText.text = "엔딩 준비중";
            var run = CampaignRunStore.Load();
            int halves = run.TotalHalfStars;
            totalStarsText.text = !run.IsComplete ? "" : "누적 " + (halves / 2) + (halves % 2 == 0 ? "" : ".5") + " / " + (StageExecutionPolicy.ActiveStages.Count * 5) + "별";
            foreach (var panel in new[] { goodPanel, normalPanel, badPanel, pendingText.gameObject })
            {
                UnityEditor.EditorUtility.SetDirty(panel);
                UnityEditor.PrefabUtility.RecordPrefabInstancePropertyModifications(panel);
            }
            foreach (var text in new[] { goodText, normalText, badText, pendingText, totalStarsText })
            {
                UnityEditor.EditorUtility.SetDirty(text);
                UnityEditor.PrefabUtility.RecordPrefabInstancePropertyModifications(text);
            }
        }

        static void ConfigurePanelArtwork(GameObject panel, Sprite sprite)
        {
            if (!panel || !sprite) throw new System.InvalidOperationException("Ending panel and native artwork Sprite required.");
            var image = panel.GetComponent<Image>();
            if (!image) throw new System.InvalidOperationException("Ending panel Image missing: " + panel.name);
            var rect = image.rectTransform;
            UnityEditor.Undo.RecordObject(rect, "Stretch ending artwork");
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            var background = rect.parent.Find("OrangeBackground");
            if (!background) throw new System.InvalidOperationException("Ending OrangeBackground missing.");
            int backgroundIndex = background.GetSiblingIndex();
            // Moving a panel from before the background shifts the background left.
            rect.SetSiblingIndex(backgroundIndex + (rect.GetSiblingIndex() < backgroundIndex ? 0 : 1));
            UnityEditor.EditorUtility.SetDirty(rect);
            UnityEditor.PrefabUtility.RecordPrefabInstancePropertyModifications(rect);
            UnityEditor.Undo.RecordObject(image, "Configure ending artwork");
            image.sprite = sprite;
            image.overrideSprite = null;
            image.color = Color.white;
            image.type = Image.Type.Simple;
            image.preserveAspect = true;
            image.raycastTarget = false;
            UnityEditor.EditorUtility.SetDirty(image);
            UnityEditor.PrefabUtility.RecordPrefabInstancePropertyModifications(image);
        }

        GameObject Panel(string name, string template, out Text text)
        {
            var rect = ResultAuthoredUi.Rect(authoredRoot, name, new Vector2(.1f,.4f), new Vector2(.9f,.72f));
            rect.gameObject.AddComponent<Image>().color = new Color(1f,.75f,.40f);
            text = ResultAuthoredUi.Label(rect, "Story", template, 44, new Vector2(.04f,.04f), new Vector2(.96f,.96f));
            return rect.gameObject;
        }
#endif
    }
}
