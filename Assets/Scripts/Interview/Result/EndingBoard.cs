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
            goodPanel.SetActive(complete && ending == EndingKind.Good);
            normalPanel.SetActive(complete && ending == EndingKind.Normal);
            badPanel.SetActive(complete && ending == EndingKind.Bad);
            pendingText.gameObject.SetActive(!complete);
            pendingText.text = "엔딩 준비중";
            goodText.text = goodTemplate; normalText.text = normalTemplate; badText.text = badTemplate;
            int halves = run.TotalHalfStars;
            totalStarsText.text = "누적 " + (halves / 2) + (halves % 2 == 0 ? "" : ".5") + " / " + (StageExecutionPolicy.ActiveStages.Count * 5) + "별";
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
