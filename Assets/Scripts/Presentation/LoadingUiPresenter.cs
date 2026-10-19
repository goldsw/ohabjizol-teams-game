using UnityEngine;
using UnityEngine.UI;

namespace Ohabjizol.Presentation
{
    /// <summary>Displays existing scene-authored UI. Creates no UI or scene operations.</summary>
    public sealed class LoadingUiPresenter : MonoBehaviour
    {
        [SerializeField] Text bootLabel;
        [SerializeField] Text waitLabel;
        [SerializeField] Text percentLabel;
        [SerializeField] Image progressFill;
        [SerializeField] RectTransform scan;
        [SerializeField] string bootText = "청강코드 실행 중…";
        [SerializeField] string waitText = "서버에 연결하는 중…";
        [SerializeField] string completeText = "연결 완료";
        [SerializeField] bool showPercent = true;
        [SerializeField] float labelPulseSpeed = 2.2f;
        bool _bound;

        public bool Bind(out string error)
        {
            _bound = bootLabel != null && waitLabel != null && percentLabel != null && progressFill != null && scan != null;
            error = _bound ? null : "LoadingUiPresenter requires authored bootLabel, waitLabel, percentLabel, progressFill and scan references.";
            return _bound;
        }

        // Legacy adapter supplies references from its existing generated/override UI.
        public bool Bind(Text boot, Text wait, Text percent, Image fill, RectTransform scanLine, out string error)
        {
            bootLabel = boot;
            waitLabel = wait;
            percentLabel = percent;
            progressFill = fill;
            scan = scanLine;
            return Bind(out error);
        }

        public void Configure(string boot, string wait, string complete, bool percent, float pulseSpeed)
        {
            bootText = boot;
            waitText = wait;
            completeText = complete;
            showPercent = percent;
            labelPulseSpeed = pulseSpeed;
        }

        public void Show()
        {
            if (!_bound)
            {
                if (!Bind(out var error))
                {
                    Debug.LogError(error, this);
                    return;
                }
            }
            gameObject.SetActive(true);
            bootLabel.text = bootText;
            waitLabel.text = waitText;
            percentLabel.gameObject.SetActive(showPercent);
            Render(default);
        }

        public void Render(SceneLaunchSnapshot snapshot)
        {
            if (!_bound)
                return;
            progressFill.fillAmount = snapshot.Progress;
            if (showPercent)
                percentLabel.text = Mathf.RoundToInt(snapshot.Progress * 100f) + "%";
            bootLabel.text = snapshot.IsComplete ? completeText : bootText;
            waitLabel.text = snapshot.IsComplete ? string.Empty : waitText;
            var color = bootLabel.color;
            if (snapshot.IsComplete)
                color.a = 1f;
            else if (labelPulseSpeed > 0f)
                color.a = 0.65f + 0.35f * Mathf.Sin(Time.unscaledTime * labelPulseSpeed);
            bootLabel.color = color;
            if (!snapshot.IsComplete && labelPulseSpeed > 0f)
            {
                float y = Mathf.Lerp(-360f, 360f, Mathf.PingPong(Time.unscaledTime * 0.35f, 1f));
                scan.anchoredPosition = new Vector2(0f, y);
            }
        }

        public void Hide() => gameObject.SetActive(false);
    }
}
