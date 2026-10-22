using System;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Ohabjizol.Interview
{
    public sealed class ProductionProgressView : MonoBehaviour
    {
        public const string SceneName = "ProductionProgressScene";
        static string queuedResultScene;
        static readonly string[] Activities = {
            "컨셉 기획중", "시스템 기획서 작성 중", "작업 지시 중",
            "컨셉 아트 그리는중", "프로그래밍 중", "버그 픽스중",
            "회의", "스크래치 제작 중", "프로토타입 제작중"
        };

        [SerializeField] Text dayNumber;
        [SerializeField] Text activityCaption;
        [SerializeField, Min(.01f)] float dayInterval = .3f;
        [SerializeField, Min(.01f)] float dotInterval = .3f;
        [SerializeField, Min(.3f)] float presentationHold = 1.2f;
        [SerializeField] Color ordinaryDateColor = Color.white;
        [SerializeField] Color presentationDateColor = new Color(1f, .2f, .2f);
        float elapsed;
        string destination;
        bool loading;

        public bool HasAuthoredUi => dayNumber != null && activityCaption != null;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetRoute() => queuedResultScene = null;

        public static string RouteBeforeResult(string resultScene, InterviewResult result)
        {
            if (result == null || result.SelectedMembers.Count != 6) return resultScene;
            if (string.IsNullOrWhiteSpace(resultScene)) throw new ArgumentException("Result scene required.", nameof(resultScene));
            queuedResultScene = resultScene;
            return SceneName;
        }

        void Start()
        {
            if (!HasAuthoredUi)
            {
                Debug.LogError("ProductionProgressView: authored date and caption required.", this);
                enabled = false;
                return;
            }
            destination = queuedResultScene ?? InterviewScreen.ResultSceneName;
            queuedResultScene = null;
            elapsed = 0f;
            Render();
        }

        void Update()
        {
            if (loading || destination == null || !HasAuthoredUi) return;
            elapsed += Time.unscaledDeltaTime;
            Render();
            if (elapsed < 30f * Mathf.Max(.01f, dayInterval) + Mathf.Max(.3f, presentationHold)) return;
            loading = true;
            // The result payload remains in ResultCarry until ResultSceneEntry consumes it.
            try { SceneManager.LoadScene(destination, LoadSceneMode.Single); }
            catch (Exception error)
            {
                enabled = false;
                Debug.LogException(error, this);
            }
        }

        void Render()
        {
            float interval = Mathf.Max(.01f, dayInterval);
            float productionDuration = 30f * interval;
            int day = Mathf.Clamp(Mathf.FloorToInt(elapsed / interval) + 1, 1, 30);
            string date = day + "일";
            if (dayNumber.text != date) dayNumber.text = date;
            dayNumber.color = day == 30 ? presentationDateColor : ordinaryDateColor;
            bool presenting = elapsed >= productionDuration;
            int index = Mathf.Clamp(Mathf.FloorToInt(elapsed / productionDuration * Activities.Length), 0, Activities.Length - 1);
            float captionElapsed = presenting ? elapsed - productionDuration : elapsed;
            int dotCount = Mathf.FloorToInt(captionElapsed / Mathf.Max(.01f, dotInterval)) % 3 + 1;
            string caption = (presenting ? "발표일!!!" : Activities[index]) + new string('.', dotCount);
            if (activityCaption.text != caption) activityCaption.text = caption;
        }

#if UNITY_EDITOR
        public void ConfigureEditor(Text day, Text caption)
        {
            dayNumber = day;
            activityCaption = caption;
            if (!HasAuthoredUi) throw new InvalidOperationException("Date and caption must be assigned.");
            elapsed = 0f;
            Render();
            UnityEditor.EditorUtility.SetDirty(this);
        }
#endif
    }
}
