using System.Collections.Generic;
using Ohabjizol.UI;
using UnityEngine;
using UnityEngine.UI;

namespace Ohabjizol.Interview
{
    public interface IInterviewTutorialTargets
    {
        Button DownloadLauncher { get; }
        Button ApplicationDownload { get; }
        Button PortfolioDownload { get; }
        Button DownloadClose { get; }
        Button LibraryLauncher { get; }
        Button LibraryClose { get; }
        Button QuestionLauncher { get; }
        Button TeamLauncher { get; }
        Button CalendarLauncher { get; }
        Button NotepadLauncher { get; }
        IReadOnlyList<Button> QuestionButtons { get; }
        RectTransform ApplicantVideo { get; }
        RectTransform DocumentWindowLayer { get; }
        DownloadPanelView Downloads { get; }
        RadialQuestionView Questions { get; }
        WindowToolbar LibraryWindow { get; }
        WindowToolbar TeamWindow { get; }
        WindowToolbar CalendarWindow { get; }
        WindowToolbar NotepadWindow { get; }
    }
}
