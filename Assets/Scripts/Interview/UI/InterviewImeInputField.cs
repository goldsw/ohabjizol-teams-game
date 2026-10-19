using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Ohabjizol.Interview
{
    public sealed class InterviewImeInputField : InputField
    {
        bool _deselecting;
        bool _labelRefreshPending;
        public bool HasComposition => caretPositionInternal != m_CaretPosition;

        protected override void LateUpdate()
        {
            base.LateUpdate();
            if (!_labelRefreshPending || _deselecting || HasComposition) return;
            _labelRefreshPending = false;
            UpdateLabel();
        }

        public override void OnDeselect(BaseEventData eventData)
        {
            _deselecting = true;
            try { base.OnDeselect(eventData); }
            finally { _deselecting = false; }
        }

        protected override void Append(string value)
        {
            if (!_deselecting) { base.Append(value); return; }
            if (readOnly || string.IsNullOrEmpty(value)) return;
            int start = Mathf.Clamp(Mathf.Min(m_CaretPosition, m_CaretSelectPosition), 0, m_Text.Length);
            int end = Mathf.Clamp(Mathf.Max(m_CaretPosition, m_CaretSelectPosition), 0, m_Text.Length);
            string result = m_Text;
            int position = start;
            bool replaced = false;
            foreach (char original in value)
            {
                if (char.IsSurrogate(original) || (original < ' ' && original != '\t' && original != '\r' && original != '\n')) continue;
                char c = original;
                if (lineType == LineType.SingleLine && (c == '\n' || c == '\r')) continue;
                if (c == '\r') c = '\n';
                if (lineType == LineType.MultiLineSubmit && c == '\n') break;
                string candidate = replaced ? result : result.Remove(start, end - start);
                if (onValidateInput != null) c = onValidateInput(candidate, position, c);
                else if (characterValidation != CharacterValidation.None) c = Validate(candidate, position, c);
                if (c == '\0' || char.IsSurrogate(c)) continue;
                if (candidate.Length >= 16382 || (characterLimit > 0 && candidate.Length >= characterLimit)) break;
                result = candidate.Insert(position++, c.ToString());
                replaced = true;
            }
            m_CaretPosition = m_CaretSelectPosition = replaced ? position : start;
            if (!replaced) return;
            m_Text = result;
            _labelRefreshPending = true;
            if (touchScreenKeyboard != null) touchScreenKeyboard.text = result;
            onValueChanged.Invoke(result);
        }
    }
}
