using System;
using System.Collections;
using System.Collections.Generic;
using Ohabjizol.Interview;
using UnityEngine;
using UnityEngine.UI;
using Ohabjizol.UI;

namespace Ohabjizol.Tutorial
{
    public sealed class TutorialController : MonoBehaviour
    {
        public const string CloseDocumentsInstruction = "이제 문서함과 지원서, 포트폴리오 창을 모두 닫아보자고.";
        [SerializeField, TextArea] string[] introduction = {
            "곧 게임잼 면접일이네.", "항상 수고하고 있어.",
            "최근에 포트폴리오를 허위로 작성해서 면접에 뽑히는 사람들이 많다고 하는데.",
            "조심하는 게 좋을 거야.", "허위 지원자를 어떻게 구별했냐고?", "팁을 좀 주자면." };
        [SerializeField, TextArea] string[] instructions = {
            "자, 이걸 누르면 다운로드 버튼이 뜰 거야.", "자, 포트폴리오와 지원서를 다운로드하자고.",
            "이제 사이드바를 닫고.", "자, 이게 방금 열었던 파일들이야.", "문서함 아이콘을 눌러봐.",
            "잘했어. 이곳에는 네가 다운받은 문서들이 정리되어 있지.", CloseDocumentsInstruction,
            "자, 이제 한번 면접자에게 질문을 해볼까?", "이걸로 포트폴리오에 관해 질문할 수 있지.",
            "이걸로 일정표에 대해 질문할 수 있어.",
            "일정표상 개발일에 개발을 못 하면 계획에 차질이 생기니 주의하라고.",
            "질문 아이콘을 다시 눌러 닫아보자고.", "자, 현재 팀은 이 아이콘으로 확인해.",
            "이건 일정표 아이콘이야. 현재 배정된 일정표를 볼 수 있지.",
            "이건 메모장인데, 같은 질문을 여러 번 하면 실례니 잘 메모해두라고." };
        [SerializeField, TextArea] string[] ending = {
            "이런 식으로 하면 되고, 난 다른 팀플 때문에 가보도록 하지.", "수고해." };
        [SerializeField, TextArea] string completion = "튜토리얼이 끝났습니다. 아무 곳이나 클릭하여 게임 시작";
        InterviewScreen _screen;
        TutorialOverlay _overlay;
        IInterviewTutorialTargets _targets;
        int _phase, _line, _step, _enteredFrame, _baselineCount;
        string _key;
        bool _subscribed, _requested, _canAsk, _application, _portfolio, _running, _returning;
        bool _downloadsOpen, _questionsExpanded;
        float _missingSince = -1;
        bool _acted;
        int _closeTargetsMask;
        readonly List<KeyValuePair<Button, UnityEngine.Events.UnityAction>> _actionHandlers = new List<KeyValuePair<Button, UnityEngine.Events.UnityAction>>();

        public void Bind(InterviewScreen screen, TutorialOverlay overlay)
        {
            _screen = screen ? screen : throw new ArgumentNullException(nameof(screen));
            _overlay = overlay ? overlay : throw new ArgumentNullException(nameof(overlay));
            _targets = screen.TutorialTargets ?? throw new InvalidOperationException("Tutorial targets are missing.");
            _overlay.Bind(_targets.ApplicantVideo);
        }

        public void Begin() { StartCoroutine(StartFlow()); }

        IEnumerator StartFlow()
        {
            yield return null;
            if (!_screen || !_screen.enabled || string.IsNullOrEmpty(_screen.ApplicantKey))
            { Fail("Tutorial session failed to start."); yield break; }
            if (introduction == null || introduction.Length != 6 || instructions == null || instructions.Length != 15
                || ending == null || ending.Length != 2 || _targets.QuestionButtons.Count < 2)
            { Fail("Tutorial dialogue or question bindings are incomplete."); yield break; }
            _screen.SetDiscordVisible(true);
            _targets.Questions.CollapseContent();
            foreach (var window in new[] { _targets.LibraryWindow, _targets.TeamWindow,
                _targets.CalendarWindow, _targets.NotepadWindow })
                if (window && window.IsOpen) window.Minimize();
            Subscribe();
            _running = true; _phase = 0; _line = 0;
            _key = _screen.ApplicantKey;
            _overlay.SetSupervisor(true);
            _overlay.Show(introduction[0], true);
        }

        void Subscribe()
        {
            if (_subscribed) return;
            _subscribed = true;
            _downloadsOpen = _targets.Downloads.IsOpen;
            _questionsExpanded = _targets.Questions.IsExpanded;
            _overlay.ContinueRequested += Continue;
            _overlay.TargetUnavailable += MissingTarget;
            _screen.QuestionRequested += QuestionRequested;
            _targets.Downloads.DownloadCompleted += DownloadCompleted;
            _targets.Downloads.VisibilityChanged += VisibilityChanged;
            _targets.Questions.ExpansionChanged += ExpansionChanged;
            Observe(_targets.DownloadLauncher, 0);
            Observe(_targets.DownloadClose, 2);
            Observe(_targets.LibraryLauncher, 4);
            Observe(_targets.LibraryClose, 6);
            Observe(_targets.QuestionLauncher, 7);
            Observe(_targets.QuestionLauncher, 11);
            Observe(_targets.TeamLauncher, 12);
            Observe(_targets.CalendarLauncher, 13);
            Observe(_targets.NotepadLauncher, 14);
        }

        void Observe(Button button, int step)
        {
            if (!button) throw new InvalidOperationException("Tutorial action button is missing.");
            UnityEngine.Events.UnityAction handler = () =>
            {
                if (_running && _phase == 1 && _step == step && _overlay.InputReleased) _acted = true;
            };
            button.onClick.AddListener(handler);
            _actionHandlers.Add(new KeyValuePair<Button, UnityEngine.Events.UnityAction>(button, handler));
        }

        void Unsubscribe()
        {
            if (!_subscribed) return;
            _subscribed = false;
            foreach (var pair in _actionHandlers) if (pair.Key) pair.Key.onClick.RemoveListener(pair.Value);
            _actionHandlers.Clear();
            if (_overlay) { _overlay.ContinueRequested -= Continue; _overlay.TargetUnavailable -= MissingTarget; }
            if (_screen) _screen.QuestionRequested -= QuestionRequested;
            if (_targets != null)
            {
                if (_targets.Downloads) { _targets.Downloads.DownloadCompleted -= DownloadCompleted; _targets.Downloads.VisibilityChanged -= VisibilityChanged; }
                if (_targets.Questions) _targets.Questions.ExpansionChanged -= ExpansionChanged;
            }
        }

        void Continue()
        {
            if (_returning) return;
            if (_phase == 4) { Return(false); return; }
            if (!_running) return;
            if (_phase == 0)
            {
                if (++_line < introduction.Length) _overlay.Show(introduction[_line], true);
                else { _phase = 1; _overlay.SetSupervisor(false); Enter(0); }
            }
            else if (_phase == 1 && (_step == 3 || _step == 5 || _step == 10)) Advance();
            else if (_phase == 2)
            {
                if (++_line < ending.Length) _overlay.Show(ending[_line], true);
                else { _phase = 3; _overlay.SetSupervisor(false); _overlay.Show(completion, true); }
            }
            else if (_phase == 3) Return(true);
        }

        void Enter(int step)
        {
            _step = step; _enteredFrame = Time.frameCount; _requested = false; _acted = false; _missingSince = -1;
            if (step == 8 || step == 9)
            {
                int index = step - 8;
                _baselineCount = _screen.UsedQuestionCount;
                _canAsk = _screen.CanAskQuestion(index);
                if (!_canAsk) { Fail("Required tutorial question is unavailable."); return; }
            }
            RectTransform[] rects;
            switch (step)
            {
                case 0: rects = Rects(_targets.DownloadLauncher); break;
                case 1: rects = Rects(_targets.ApplicationDownload, _targets.PortfolioDownload); break;
                case 2: rects = Rects(_targets.DownloadClose); break;
                case 3:
                    if (!_screen.Documents.TryGetViewer(DocumentKind.Application, out var application)
                        || !_screen.Documents.TryGetViewer(DocumentKind.Portfolio, out var portfolio))
                    { Fail("Downloaded document windows are unavailable."); return; }
                    rects = new[] { application.transform as RectTransform, portfolio.transform as RectTransform }; break;
                case 4: rects = Rects(_targets.LibraryLauncher); break;
                case 5: rects = new[] { _targets.LibraryWindow.transform as RectTransform }; break;
                case 6:
                    if (!TryGetCloseDocumentTargets(out rects, out _closeTargetsMask)) return;
                    break;
                case 7: case 11: rects = Rects(_targets.QuestionLauncher); break;
                case 8: case 9: rects = Rects(_targets.QuestionButtons[step - 8]); break;
                case 10: rects = Rects(_targets.CalendarLauncher); break;
                case 12: rects = Rects(_targets.TeamLauncher); break;
                case 13: rects = Rects(_targets.CalendarLauncher); break;
                default: rects = Rects(_targets.NotepadLauncher); break;
            }
            foreach (var rect in rects)
                if (!rect) { Fail("A required tutorial target is missing."); return; }
            _overlay.Show(instructions[step], step == 3 || step == 5 || step == 10, rects);
            if (step == 6) _overlay.SetEmphasis(CloseButtons(rects));
        }

        static RectTransform[] CloseButtons(RectTransform[] controls)
        {
            var closes = new RectTransform[controls.Length];
            for (int i = 0; i < controls.Length; i++)
                closes[i] = controls[i] && controls[i].Find("Close") is RectTransform close ? close : controls[i];
            return closes;
        }

        static RectTransform[] Rects(params Button[] buttons)
        {
            var rects = new RectTransform[buttons.Length];
            for (int i = 0; i < buttons.Length; i++) rects[i] = buttons[i] ? buttons[i].transform as RectTransform : null;
            return rects;
        }

        bool TryGetCloseDocumentTargets(out RectTransform[] rects, out int mask)
        {
            rects = Array.Empty<RectTransform>();
            mask = 0;
            var library = _targets.LibraryWindow;
            if (!library) { Fail("Tutorial document library is unavailable."); return false; }
            if (library.IsOpen || library.IsMinimized)
            {
                if (library.IsMinimized) library.Restore();
                mask = 1;
                rects = new[] { WindowControls(_targets.LibraryClose) };
                return true;
            }

            PdfDocumentViewer front = null;
            int frontMask = 0;
            foreach (var kind in new[] { DocumentKind.Application, DocumentKind.Portfolio })
            {
                var state = _screen.Documents.GetState(kind);
                if (state == DocumentViewerState.Closed) continue;
                if (state == DocumentViewerState.Minimized)
                {
                    var taskbar = _screen.Documents.TaskbarView;
                    if (!taskbar || !taskbar.Queue.TryGetWindow(new DocumentKey(_key, kind), out var window))
                    { Fail("A downloaded tutorial document is unavailable."); return false; }
                    window.Maximize();
                }
                if (!_screen.Documents.TryGetViewer(kind, out var viewer) || !viewer.CloseButton)
                { Fail("A downloaded document close button is unavailable."); return false; }
                // Overlapping windows share one outline frame, so only the frontmost is targeted.
                if (front && viewer.transform.GetSiblingIndex() < front.transform.GetSiblingIndex()) continue;
                front = viewer;
                frontMask = kind == DocumentKind.Application ? 2 : 4;
            }
            if (front)
            {
                mask = frontMask;
                rects = new[] { WindowControls(front.CloseButton) };
            }
            return true;
        }

        static RectTransform WindowControls(Button close)
        {
            if (!close) return null;
            var controls = close.transform.parent as RectTransform;
            var minimize = controls ? controls.Find("Minimize") : null;
            return minimize && minimize.gameObject.activeInHierarchy ? controls : close.transform as RectTransform;
        }

        bool AllTutorialDocumentsClosed() => !_targets.LibraryWindow.IsOpen && !_targets.LibraryWindow.IsMinimized
            && _screen.Documents.GetState(DocumentKind.Application) == DocumentViewerState.Closed
            && _screen.Documents.GetState(DocumentKind.Portfolio) == DocumentViewerState.Closed;

        void Advance()
        {
            if (_step < 14) Enter(_step + 1);
            else { _phase = 2; _line = 0; _overlay.SetSupervisor(true); _overlay.Show(ending[0], true); }
        }

        void QuestionRequested(int index)
        {
            if (_running && _phase == 1 && (_step == 8 || _step == 9) && index == _step - 8
                && _canAsk && _key == _screen.ApplicantKey && _overlay.InputReleased)
            { _requested = true; _enteredFrame = Time.frameCount; }
        }

        void DownloadCompleted(DocumentDownloadRequest request)
        {
            if (!_running || _phase != 1 || _step != 1 || _key != _screen.ApplicantKey
                || !_targets.Downloads.IsCurrent(request)) return;
            if (request.Kind == DocumentKind.Application) _application = true;
            if (request.Kind == DocumentKind.Portfolio) _portfolio = true;
        }
        void VisibilityChanged(bool open) { _downloadsOpen = open; }
        void ExpansionChanged(bool expanded) { _questionsExpanded = expanded; }

        void Update()
        {
            if (!_running || _phase != 1 || Time.frameCount <= _enteredFrame) return;
            if (_key != _screen.ApplicantKey) { Fail("Tutorial applicant context changed."); return; }
            bool done = false;
            switch (_step)
            {
                case 0: done = _downloadsOpen && _targets.Downloads.IsOpen; break;
                case 1: done = _application && _portfolio
                    && _screen.Documents.GetState(DocumentKind.Application) == DocumentViewerState.Normal
                    && _screen.Documents.GetState(DocumentKind.Portfolio) == DocumentViewerState.Normal; break;
                case 2: done = !_downloadsOpen && !_targets.Downloads.IsOpen; break;
                case 4: done = Visible(_targets.LibraryWindow); break;
                case 6:
                    done = AllTutorialDocumentsClosed();
                    if (!done)
                    {
                        if (!TryGetCloseDocumentTargets(out var closeTargets, out int mask)) return;
                        if (mask != _closeTargetsMask)
                        {
                            _closeTargetsMask = mask;
                            _missingSince = -1;
                            _overlay.Show(instructions[6], false, closeTargets);
                            _overlay.SetEmphasis(CloseButtons(closeTargets));
                        }
                    }
                    break;
                case 7: done = _questionsExpanded && _targets.Questions.IsExpanded; break;
                case 8: case 9: done = _requested && _screen.UsedQuestionCount > _baselineCount && !_screen.IsAnswering; break;
                case 11: done = !_questionsExpanded && !_targets.Questions.IsExpanded; break;
                case 12: done = Visible(_targets.TeamWindow); break;
                case 13: done = Visible(_targets.CalendarWindow); break;
                case 14: done = Visible(_targets.NotepadWindow); break;
            }
            if (done && (_acted || _step == 1 || _step == 6 || _step == 8 || _step == 9)) { Advance(); return; }
            if (_overlay.TargetsAvailable) _missingSince = -1;
            if (_missingSince >= 0 && Time.unscaledTime - _missingSince >= .2f)
                Fail("Tutorial target is hidden or missing.");
        }

        static bool Visible(WindowToolbar window) => window && window.IsOpen && !window.IsMinimized
            && window.Info && window.Info.gameObject.activeInHierarchy;

        void MissingTarget()
        {
            if (_running && _phase == 1 && _missingSince < 0) _missingSince = Time.unscaledTime;
        }
        void Fail(string error)
        {
            Debug.LogError(error, this);
            ShowError(_overlay, error);
        }
        public void ShowError(TutorialOverlay overlay, string error)
        {
            Unsubscribe(); _running = false; _phase = 4; _overlay = overlay;
            _overlay.ContinueRequested -= Continue;
            _overlay.ContinueRequested += Continue;
            _overlay.SetSupervisor(false);
            _overlay.Show("튜토리얼을 진행할 수 없습니다. 메인 화면으로 돌아가기\n" + error, true);
        }

        void Return(bool completed)
        {
            _returning = true; _running = false;
            Unsubscribe();
            if (_screen) _screen.DisposeSession();
            if (completed) TutorialProgress.MarkSeen();
            TutorialSceneFlow.ReturnMain();
        }
        void OnDisable()
        {
            StopAllCoroutines(); _running = false; Unsubscribe();
            if (_overlay) _overlay.ContinueRequested -= Continue;
            if (_screen) _screen.DisposeSession();
        }
    }
}
