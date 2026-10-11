using System;
using UnityEngine;
using UnityEngine.UI;

namespace Ohabjizol.Interview
{
    /// <summary>
    /// Writes one session <see cref="InterviewResult"/> into the result color panels.
    /// Grade, fulfillment, and team fraud are read from that result.
    /// Skill cells also show the theme requirement. A zero skill stays on screen.
    /// </summary>
    public sealed class ResultBoard : MonoBehaviour
    {
        const int FontSize = 28;

        InterviewResult _result;
        Font _font;
        bool _ownsFont;

        public InterviewResult Result
        {
            get { return _result; }
        }

        public void Show(InterviewResult result)
        {
            if (result == null)
                throw new ArgumentNullException(nameof(result));

            _result = result;
            Paint(result);
        }

        void OnDestroy()
        {
            if (_ownsFont && _font != null)
                Destroy(_font);
        }

        void Paint(InterviewResult result)
        {
            SkillSet actual = result.ActualSkills;
            SkillSet required = PrototypeCatalog.Theme.RequiredSkills;
            WriteSkill(JobRole.Planning, actual, required);
            WriteSkill(JobRole.Programming, actual, required);
            WriteSkill(JobRole.Graphics, actual, required);
            WritePanel("FulfillmentPanel", "충족률 " + result.FulfillmentPercent + "%");
            WritePanel("VerificationSuccessPanel", "검증 성공 " + result.VerificationSuccessCount);
            WritePanel("VerificationFailurePanel", "검증 실패 " + result.VerificationFailureCount);
            WritePanel("FraudPanel", "허위 인원 " + result.FraudCount);
            WritePanel("GradePanel", "임시 등급 " + result.Grade);
        }

        void WriteSkill(JobRole role, SkillSet actual, SkillSet required)
        {
            int actualValue = actual == null ? 0 : actual.Read(role);
            int requiredValue = required == null ? 0 : required.Read(role);
            string caption = JobRoleText.Korean(role)
                + "\n실제 능력 " + actualValue
                + "\n요구 능력 " + requiredValue;
            WritePanel(PanelName(role), caption);
        }

        static string PanelName(JobRole role)
        {
            switch (role)
            {
                case JobRole.Planning:
                    return "PlanningPanel";
                case JobRole.Programming:
                    return "ProgrammingPanel";
                case JobRole.Graphics:
                    return "GraphicsPanel";
                default:
                    return string.Empty;
            }
        }

        void WritePanel(string panelName, string caption)
        {
            if (string.IsNullOrEmpty(panelName))
                return;

            GameObject panel = FindPanel(panelName);
            if (panel == null)
                return;

            panel.SetActive(true);
            Text label = LabelOn(panel);
            label.text = caption;
            Image image = panel.GetComponent<Image>();
            label.color = LabelColor(image != null ? image.color : Color.black);
        }

        static GameObject FindPanel(string panelName)
        {
            Transform[] transforms = FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            for (int i = 0; i < transforms.Length; i++)
            {
                if (transforms[i] != null && transforms[i].name == panelName)
                    return transforms[i].gameObject;
            }

            return null;
        }

        Text LabelOn(GameObject panel)
        {
            Text label = panel.GetComponentInChildren<Text>(true);
            if (label == null)
            {
                GameObject child = new GameObject("Label");
                child.transform.SetParent(panel.transform, false);
                RectTransform rect = child.AddComponent<RectTransform>();
                rect.anchorMin = Vector2.zero;
                rect.anchorMax = Vector2.one;
                rect.offsetMin = new Vector2(12f, 8f);
                rect.offsetMax = new Vector2(-12f, -8f);
                rect.localScale = Vector3.one;
                label = child.AddComponent<Text>();
                label.fontSize = FontSize;
                label.fontStyle = FontStyle.Bold;
                label.alignment = TextAnchor.MiddleCenter;
                label.horizontalOverflow = HorizontalWrapMode.Wrap;
                label.verticalOverflow = VerticalWrapMode.Overflow;
                label.raycastTarget = false;
                label.supportRichText = false;
            }

            label.gameObject.SetActive(true);
            label.font = UiFont();
            return label;
        }

        Font UiFont()
        {
            if (_font != null)
                return _font;

            _font = Font.CreateDynamicFontFromOSFont(new[] { "Malgun Gothic", "맑은 고딕", "Gulim" }, FontSize);
            _ownsFont = _font != null;
            if (_font == null)
                _font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            return _font;
        }

        static Color LabelColor(Color panel)
        {
            float luminance = panel.r * 0.299f + panel.g * 0.587f + panel.b * 0.114f;
            if (luminance > 0.62f)
                return new Color(0.08f, 0.08f, 0.1f, 1f);
            return new Color(0.96f, 0.97f, 0.95f, 1f);
        }
    }
}
