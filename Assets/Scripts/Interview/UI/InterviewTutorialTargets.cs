using System;
using System.Collections.Generic;
using Ohabjizol.UI;
using UnityEngine;
using UnityEngine.UI;

namespace Ohabjizol.Interview
{
    [DisallowMultipleComponent]
    public sealed class InterviewTutorialTargets : MonoBehaviour, IInterviewTutorialTargets
    {
        [SerializeField] Button _downloadLauncher;
        [SerializeField] Button _applicationDownload;
        [SerializeField] Button _portfolioDownload;
        [SerializeField] Button _downloadClose;
        [SerializeField] Button _libraryLauncher;
        [SerializeField] Button _libraryClose;
        [SerializeField] Button _questionLauncher;
        [SerializeField] Button _teamLauncher;
        [SerializeField] Button _calendarLauncher;
        [SerializeField] Button _notepadLauncher;
        [SerializeField] Button[] _questionButtons = Array.Empty<Button>();
        [SerializeField] RectTransform _applicantVideo;
        [SerializeField] RectTransform _documentWindowLayer;
        [SerializeField] DownloadPanelView _downloads;
        [SerializeField] RadialQuestionView _questions;
        [SerializeField] WindowToolbar _libraryWindow;
        [SerializeField] WindowToolbar _teamWindow;
        [SerializeField] WindowToolbar _calendarWindow;
        [SerializeField] WindowToolbar _notepadWindow;

        public Button DownloadLauncher => _downloadLauncher;
        public Button ApplicationDownload => _applicationDownload;
        public Button PortfolioDownload => _portfolioDownload;
        public Button DownloadClose => _downloadClose;
        public Button LibraryLauncher => _libraryLauncher;
        public Button LibraryClose => _libraryClose;
        public Button QuestionLauncher => _questionLauncher;
        public Button TeamLauncher => _teamLauncher;
        public Button CalendarLauncher => _calendarLauncher;
        public Button NotepadLauncher => _notepadLauncher;
        public IReadOnlyList<Button> QuestionButtons => Array.AsReadOnly(_questionButtons ?? Array.Empty<Button>());
        public RectTransform ApplicantVideo => _applicantVideo;
        public RectTransform DocumentWindowLayer => _documentWindowLayer;
        public DownloadPanelView Downloads => _downloads;
        public RadialQuestionView Questions => _questions;
        public WindowToolbar LibraryWindow => _libraryWindow;
        public WindowToolbar TeamWindow => _teamWindow;
        public WindowToolbar CalendarWindow => _calendarWindow;
        public WindowToolbar NotepadWindow => _notepadWindow;

        internal void Validate(InterviewScreen owner)
        {
            if (!owner || transform.parent != owner.transform)
                throw new InvalidOperationException("Tutorial targets must be a direct child of their InterviewScreen.");
            Component[] references = { _downloadLauncher, _applicationDownload, _portfolioDownload, _downloadClose,
                _libraryLauncher, _libraryClose, _questionLauncher, _teamLauncher, _calendarLauncher, _notepadLauncher,
                _applicantVideo, _documentWindowLayer, _downloads, _questions,
                _libraryWindow, _teamWindow, _calendarWindow, _notepadWindow };
            foreach (var reference in references)
                if (!reference || !reference.transform.IsChildOf(owner.transform))
                    throw new InvalidOperationException("Tutorial target is missing or references another Screen hierarchy.");
            if (_questionButtons == null || _questionButtons.Length != InterviewQuestionCatalog.QuestionCount)
                throw new InvalidOperationException("Tutorial requires two question buttons in Screen order.");
            var unique = new HashSet<Button>();
            for (int i = 0; i < _questionButtons.Length; i++)
                if (!_questionButtons[i] || !_questionButtons[i].transform.IsChildOf(owner.transform) ||
                    !unique.Add(_questionButtons[i]) || _questions.QuestionButtons.Count != InterviewQuestionCatalog.QuestionCount ||
                    _questions.QuestionButtons[i] != _questionButtons[i])
                    throw new InvalidOperationException("Tutorial question references must match the authored radial buttons.");
        }

#if UNITY_EDITOR
        internal void ConfigureEditor(Button[] buttons, Button[] questionButtons, RectTransform video,
            RectTransform windowLayer, DownloadPanelView downloads, RadialQuestionView questions, WindowToolbar[] windows)
        {
            if (Application.isPlaying || UnityEditor.EditorApplication.isPlayingOrWillChangePlaymode)
                throw new InvalidOperationException("Tutorial target binding requires EditMode.");
            UnityEditor.Undo.RecordObject(this, "Bind interview tutorial targets");
            _downloadLauncher = buttons[0]; _applicationDownload = buttons[1]; _portfolioDownload = buttons[2];
            _downloadClose = buttons[3]; _libraryLauncher = buttons[4]; _libraryClose = buttons[5];
            _questionLauncher = buttons[6]; _teamLauncher = buttons[7]; _calendarLauncher = buttons[8];
            _notepadLauncher = buttons[9];
            _questionButtons = (Button[])questionButtons.Clone();
            _applicantVideo = video; _documentWindowLayer = windowLayer;
            _downloads = downloads; _questions = questions;
            _libraryWindow = windows[0]; _teamWindow = windows[1];
            _calendarWindow = windows[2]; _notepadWindow = windows[3];
            UnityEditor.EditorUtility.SetDirty(this);
            UnityEditor.PrefabUtility.RecordPrefabInstancePropertyModifications(this);
        }
#endif
    }
}
