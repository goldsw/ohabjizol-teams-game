using UnityEngine;
using UnityEngine.UI;

namespace Ohabjizol.Interview
{
    public sealed partial class InterviewScreen
    {
        string _tutorialNotificationText;
        bool _tutorialGreeting;

        public Button TutorialAcceptButton => _acceptButton;
        public SelectedTeamDisplaySnapshot TutorialSelectedTeam => _runtime?.SelectedTeam;
        public RectTransform TutorialScreenRegion => _discordViewRoot;
        public RectTransform TutorialApplicantSpawnRegion => _call ? _call.PortraitRect : null;
        public RectTransform TutorialColleagueRegion => _notification && _notification.transform.parent
            ? _notification.transform.parent.parent as RectTransform : null;
        public RectTransform TutorialCalendarRequirements => _calendarWindow
            ? _calendarWindow.transform.Find("Info/ThemeRequirements") as RectTransform : null;
        public RectTransform TutorialPortfolioFileTarget => _workspace && _workspace.Library
            && !string.IsNullOrEmpty(ApplicantKey)
            ? _workspace.Library.GetDocumentTarget(new DocumentKey(ApplicantKey, DocumentKind.Portfolio)) : null;

        public void ShowTutorialColleagueGreeting()
        {
            if (!_notification) return;
            if (!_tutorialGreeting) _tutorialNotificationText = _notification.text;
            _tutorialGreeting = true;
            _notification.text = "예 안녕하십니까!";
            var bubble = _notification.GetComponent<InterviewNotificationBubble>();
            if (bubble) bubble.SetVisible(true);
        }

        public void ClearTutorialColleagueGreeting()
        {
            if (!_tutorialGreeting) return;
            _tutorialGreeting = false;
            if (!_notification) return;
            _notification.text = _tutorialNotificationText;
            var bubble = _notification.GetComponent<InterviewNotificationBubble>();
            if (bubble) bubble.SetVisible(InboxUnread);
        }
    }
}
