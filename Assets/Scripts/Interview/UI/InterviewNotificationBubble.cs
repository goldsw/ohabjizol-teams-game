using UnityEngine;
using UnityEngine.UI;

namespace Ohabjizol.Interview
{
    public sealed class InterviewNotificationBubble : MonoBehaviour
    {
        [SerializeField] Image background;
        public void SetVisible(bool visible)
        {
            if (background) background.gameObject.SetActive(visible);
        }
#if UNITY_EDITOR
        public void ConfigureEditor(Image bubble)
        {
            background = bubble;
            SetVisible(false);
            UnityEditor.EditorUtility.SetDirty(this);
        }
#endif
    }
}
