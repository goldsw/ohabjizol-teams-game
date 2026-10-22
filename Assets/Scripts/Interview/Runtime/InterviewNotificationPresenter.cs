using UnityEngine;
using UnityEngine.UI;

namespace Ohabjizol.Interview
{
    public sealed class InterviewNotificationPresenter
    {
        readonly Text _notification;
        readonly GameObject _unreadMark;
        public bool Unread { get; private set; }
        public InterviewNotificationPresenter(Text notification, GameObject unreadMark)
        { _notification = notification; _unreadMark = unreadMark; }
        public void NewApplicant() { Unread = true; Render(); }
        public void Clear() { Unread = false; Render(); }
        public void Render()
        {
            _notification.text = Unread ? "면접자 자료 보냈습니다." : "";
            var bubble = _notification.GetComponent<InterviewNotificationBubble>();
            if (bubble != null) bubble.SetVisible(Unread);
            _unreadMark.SetActive(Unread);
        }
    }
}
