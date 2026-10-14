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
            _notification.text = Unread ? "새 면접자 서류입니다." : "";
            _unreadMark.SetActive(Unread);
        }
    }
}