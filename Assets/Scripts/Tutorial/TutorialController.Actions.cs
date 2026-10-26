using Ohabjizol.Interview;
using Ohabjizol.UI;
using UnityEngine;
using UnityEngine.UI;

namespace Ohabjizol.Tutorial
{
    public sealed partial class TutorialController
    {
        static bool IsQuestion(TutorialAction action) =>
            action == TutorialAction.AskPortfolio || action == TutorialAction.AskSchedule;
        static int QuestionIndex(TutorialAction action) => action == TutorialAction.AskPortfolio ? 0 : 1;
        static bool IsClose(TutorialAction action) =>
            action == TutorialAction.CloseTeam || action == TutorialAction.CloseApplication ||
            action == TutorialAction.ClosePortfolio || action == TutorialAction.CloseDownloads ||
            action == TutorialAction.CloseLibrary || action == TutorialAction.CloseCalendar ||
            action == TutorialAction.CloseNotepad;
        static bool Visible(WindowToolbar window) => window && window.IsOpen && !window.IsMinimized
            && window.Info && window.Info.gameObject.activeInHierarchy;
        static bool Closed(WindowToolbar window) => window && !window.IsOpen && !window.IsMinimized;

        Button DocumentClose(DocumentKind kind) =>
            _screen.Documents.TryGetViewer(kind, out var viewer) ? viewer.CloseButton : null;

        Button ResolveButton(TutorialAction action)
        {
            switch (action)
            {
                case TutorialAction.OpenTeam: return _targets.TeamLauncher;
                case TutorialAction.CloseTeam: return _targets.TeamWindow.CloseButton;
                case TutorialAction.OpenDownloads: return _targets.DownloadLauncher;
                case TutorialAction.DownloadApplication: return _targets.ApplicationDownload;
                case TutorialAction.CloseApplication: return DocumentClose(DocumentKind.Application);
                case TutorialAction.DownloadPortfolio: return _targets.PortfolioDownload;
                case TutorialAction.CloseDownloads: return _targets.DownloadClose;
                case TutorialAction.ClosePortfolio: return DocumentClose(DocumentKind.Portfolio);
                case TutorialAction.OpenQuestions: return _targets.QuestionLauncher;
                case TutorialAction.AskPortfolio: return _targets.QuestionButtons[0];
                case TutorialAction.AskSchedule: return _targets.QuestionButtons[1];
                case TutorialAction.OpenLibrary: return _targets.LibraryLauncher;
                case TutorialAction.CloseLibrary: return _targets.LibraryClose;
                case TutorialAction.OpenCalendar: return _targets.CalendarLauncher;
                case TutorialAction.CloseCalendar: return _targets.CalendarWindow.CloseButton;
                case TutorialAction.Accept: return _screen.TutorialAcceptButton;
                case TutorialAction.OpenNotepad: return _targets.NotepadLauncher;
                case TutorialAction.CloseNotepad: return _targets.NotepadWindow.CloseButton;
                default: return null;
            }
        }

        RectTransform DocumentRect(DocumentKind kind) =>
            _screen.Documents.TryGetViewer(kind, out var viewer) ? viewer.transform as RectTransform : null;

        RectTransform[] FocusTargets(TutorialFocus focus)
        {
            RectTransform target;
            switch (focus)
            {
                case TutorialFocus.Screen: target = _screen.TutorialScreenRegion; break;
                case TutorialFocus.Applicant: target = _screen.TutorialApplicantSpawnRegion; break;
                case TutorialFocus.ApplicantSpawn: target = _screen.TutorialApplicantSpawnRegion; break;
                case TutorialFocus.Colleague: target = _screen.TutorialColleagueRegion; break;
                case TutorialFocus.Team: target = _targets.TeamWindow.transform as RectTransform; break;
                case TutorialFocus.Application: target = DocumentRect(DocumentKind.Application); break;
                case TutorialFocus.Portfolio: target = DocumentRect(DocumentKind.Portfolio); break;
                case TutorialFocus.Calendar: target = _targets.CalendarWindow.transform as RectTransform; break;
                case TutorialFocus.Requirements: target = _screen.TutorialCalendarRequirements; break;
                case TutorialFocus.Notepad: target = _targets.NotepadWindow.transform as RectTransform; break;
                default: return System.Array.Empty<RectTransform>();
            }
            return new[] { target };
        }

        bool ActionComplete(TutorialAction action)
        {
            switch (action)
            {
                case TutorialAction.OpenTeam: return Visible(_targets.TeamWindow);
                case TutorialAction.CloseTeam: return Closed(_targets.TeamWindow);
                case TutorialAction.OpenDownloads: return _targets.Downloads.IsOpen;
                case TutorialAction.DownloadApplication:
                    return _downloaded && _screen.Documents.GetState(DocumentKind.Application) == DocumentViewerState.Normal;
                case TutorialAction.CloseApplication:
                    return _screen.Documents.GetState(DocumentKind.Application) == DocumentViewerState.Closed;
                case TutorialAction.DownloadPortfolio:
                    return _downloaded && _screen.Documents.GetState(DocumentKind.Portfolio) == DocumentViewerState.Normal;
                case TutorialAction.CloseDownloads: return !_targets.Downloads.IsOpen;
                case TutorialAction.ClosePortfolio:
                    return _screen.Documents.GetState(DocumentKind.Portfolio) == DocumentViewerState.Closed;
                case TutorialAction.OpenQuestions: return _targets.Questions.IsExpanded;
                case TutorialAction.AskPortfolio:
                case TutorialAction.AskSchedule:
                    return _requested && _screen.UsedQuestionCount > _questionBaseline && !_screen.IsAnswering;
                case TutorialAction.OpenLibrary: return Visible(_targets.LibraryWindow);
                case TutorialAction.OpenPortfolioFile:
                    return _screen.Documents.GetState(DocumentKind.Portfolio) == DocumentViewerState.Normal;
                case TutorialAction.CloseLibrary: return Closed(_targets.LibraryWindow);
                case TutorialAction.OpenCalendar: return Visible(_targets.CalendarWindow);
                case TutorialAction.CloseCalendar: return Closed(_targets.CalendarWindow);
                case TutorialAction.Accept: return _acceptRequested && _screen.TutorialSelectedTeam.Count > _teamBaseline;
                case TutorialAction.OpenNotepad: return Visible(_targets.NotepadWindow);
                case TutorialAction.CloseNotepad: return Closed(_targets.NotepadWindow);
                default: return false;
            }
        }
    }
}
