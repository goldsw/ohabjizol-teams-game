using System;
using System.Collections;
using Ohabjizol.Interview;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace Ohabjizol.Tutorial
{
    public sealed partial class TutorialController : MonoBehaviour
    {
        public const string CloseDocumentsInstruction = "이제 문서함과 지원서, 포트폴리오 창을 모두 닫아보자고.";
        InterviewScreen _screen;
        TutorialOverlay _overlay;
        IInterviewTutorialTargets _targets;
        int _step, _enteredFrame, _questionBaseline, _teamBaseline;
        string _key;
        bool _running, _returning, _error, _subscribed, _acted, _requested, _downloaded, _acceptRequested, _waitingPresentation;
        float _missingSince = -1;
        Button _observedButton;
        UnityAction _observedHandler;
        RectTransform[] _shownTargets = Array.Empty<RectTransform>();
        TutorialStep Current => TutorialScenario.Steps[_step];

        public void Bind(InterviewScreen screen, TutorialOverlay overlay)
        {
            _screen = screen ? screen : throw new ArgumentNullException(nameof(screen));
            _overlay = overlay ? overlay : throw new ArgumentNullException(nameof(overlay));
            _targets = screen.TutorialTargets ?? throw new InvalidOperationException("Tutorial targets are missing.");
            _overlay.Bind(_targets.ApplicantVideo);
        }

        public void Begin()
        {
            if (_running || _returning) return;
            StopAllCoroutines();
            StartCoroutine(StartFlow());
        }

        IEnumerator StartFlow()
        {
            yield return null;
            if (!_screen || !_screen.enabled || !_overlay || _targets == null ||
                string.IsNullOrEmpty(_screen.ApplicantKey) || !_screen.Documents ||
                !_targets.Downloads || !_targets.Questions || _targets.QuestionButtons.Count < 2 ||
                !_targets.LibraryWindow || !_targets.TeamWindow || !_targets.CalendarWindow || !_targets.NotepadWindow)
            { Fail("Tutorial session or required bindings are unavailable."); yield break; }
            _screen.SetDiscordVisible(true);
            _targets.Questions.CollapseContent();
            _screen.Documents.CloseAllWindows();
            foreach (var window in new[] { _targets.LibraryWindow, _targets.TeamWindow,
                _targets.CalendarWindow, _targets.NotepadWindow })
                if (window.IsOpen) window.Minimize();
            if (_targets.Downloads.IsOpen)
            {
                if (!_targets.DownloadClose) { Fail("Download close button is missing."); yield break; }
                _targets.DownloadClose.onClick.Invoke();
                if (_targets.Downloads.IsOpen) { Fail("Download panel could not be closed."); yield break; }
            }
            _key = _screen.ApplicantKey;
            _error = false;
            _acceptRequested = false;
            _running = true;
            Subscribe();
            Enter(0);
        }

        void Subscribe()
        {
            if (_subscribed) return;
            _subscribed = true;
            _overlay.ContinueRequested += Continue;
            _overlay.TargetUnavailable += MissingTarget;
            _screen.QuestionRequested += QuestionRequested;
            _targets.Downloads.DownloadCompleted += DownloadCompleted;
        }

        void ClearButton()
        {
            if (_observedButton && _observedHandler != null)
                _observedButton.onClick.RemoveListener(_observedHandler);
            _observedButton = null;
            _observedHandler = null;
        }

        void Unsubscribe()
        {
            ClearButton();
            if (_overlay)
            {
                _overlay.ContinueRequested -= Continue;
                _overlay.TargetUnavailable -= MissingTarget;
            }
            if (!_subscribed) return;
            _subscribed = false;
            if (_screen) _screen.QuestionRequested -= QuestionRequested;
            if (_targets != null && _targets.Downloads)
                _targets.Downloads.DownloadCompleted -= DownloadCompleted;
        }

        void Enter(int index)
        {
            ClearButton();
            if (_screen) _screen.ClearTutorialColleagueGreeting();
            _step = index;
            _acted = _requested = _downloaded = false;
            _waitingPresentation = false;
            _enteredFrame = Time.frameCount;
            _missingSince = -1;
            var action = Current.Action;
            if (action == TutorialAction.OpenLibrary || action == TutorialAction.OpenCalendar)
                _targets.Questions.CollapseContent();
            if (IsQuestion(action))
            {
                _questionBaseline = _screen.UsedQuestionCount;
                if (!_screen.CanAskQuestion(QuestionIndex(action)))
                { Fail("Required tutorial question is unavailable."); return; }
            }
            if (action == TutorialAction.Accept) _teamBaseline = _screen.TutorialSelectedTeam.Count;
            if (action == TutorialAction.OpenPortfolioFile &&
                _screen.Documents.GetState(DocumentKind.Portfolio) != DocumentViewerState.Closed)
            { Fail("Tutorial portfolio must be closed before opening its library row."); return; }
            // A close step can safely be omitted if the window is already closed.
            if (IsClose(action) && ActionComplete(action)) { Advance(); return; }
            _overlay.SetSupervisor(Current.Supervisor);
            if (Current.Greeting) _screen.ShowTutorialColleagueGreeting();
            ShowCurrent();
        }

        void ShowCurrent()
        {
            var action = Current.Action;
            var button = ResolveButton(action);
            _shownTargets = action == TutorialAction.None
                ? FocusTargets(Current.Focus)
                : action == TutorialAction.Complete ? Array.Empty<RectTransform>()
                : action == TutorialAction.OpenPortfolioFile ? new[] { _screen.TutorialPortfolioFileTarget }
                : new[] { button ? button.transform as RectTransform : null };
            if (button)
            {
                _observedButton = button;
                int observedStep = _step;
                _observedHandler = () =>
                {
                    if (!_running || _step != observedStep || !_overlay.InputReleased) return;
                    _acted = true;
                    if (action == TutorialAction.Accept) _acceptRequested = true;
                };
                button.onClick.AddListener(_observedHandler);
            }
            _overlay.Show(Current.Text, action == TutorialAction.None || action == TutorialAction.Complete, _shownTargets);
            _overlay.SetContinueWithAnyKey(action == TutorialAction.Complete);
            if (IsClose(action)) _overlay.SetEmphasis(button ? button.transform as RectTransform : null);
        }

        void Continue()
        {
            if (_returning) return;
            if (_error) { Return(false); return; }
            if (!_running) return;
            if (Current.Action == TutorialAction.Complete) Return(true);
            else if (Current.Action == TutorialAction.None) Advance();
        }

        void Advance()
        {
            if (_step + 1 < TutorialScenario.Steps.Length) Enter(_step + 1);
            else Fail("Tutorial completion step is missing.");
        }

        void QuestionRequested(int index)
        {
            if (_running && IsQuestion(Current.Action) && index == QuestionIndex(Current.Action)
                && _key == _screen.ApplicantKey && _overlay.InputReleased)
                _requested = true;
        }

        void DownloadCompleted(DocumentDownloadRequest request)
        {
            if (!_running || (!_acted && !_overlay.InputReleased) || _key != _screen.ApplicantKey ||
                !_targets.Downloads.IsCurrent(request)) return;
            if ((Current.Action == TutorialAction.DownloadApplication && request.Kind == DocumentKind.Application) ||
                (Current.Action == TutorialAction.DownloadPortfolio && request.Kind == DocumentKind.Portfolio))
                _downloaded = true;
        }

        void Update()
        {
            if (!_running || Time.frameCount <= _enteredFrame) return;
            if (!_screen || (!_acceptRequested && _key != _screen.ApplicantKey))
            { Fail("Tutorial applicant context changed."); return; }
            // Check success before checking targets: the expected click may destroy or hide its button.
            if (Current.Action != TutorialAction.None && Current.Action != TutorialAction.Complete &&
                (_acted || Current.Action == TutorialAction.OpenPortfolioFile) && ActionComplete(Current.Action))
            { Advance(); return; }
            // During asynchronous responses/fades the clicked control may legitimately disappear.
            // Keep the overlay modal while waiting, without leaving other controls exposed.
            if (!_waitingPresentation && _acted &&
                ((IsQuestion(Current.Action) && _requested) || Current.Action == TutorialAction.Accept ||
                 Current.Action == TutorialAction.DownloadApplication || Current.Action == TutorialAction.DownloadPortfolio))
            {
                _waitingPresentation = true;
                var waitingButton = ResolveButton(Current.Action);
                var waitingTarget = Current.Action == TutorialAction.DownloadApplication || Current.Action == TutorialAction.DownloadPortfolio
                    ? _targets.Downloads.transform as RectTransform
                    : waitingButton ? waitingButton.transform as RectTransform : _screen.TutorialApplicantSpawnRegion;
                _shownTargets = new[] { waitingTarget };
                _missingSince = -1;
                _overlay.Show(Current.Text, false, _shownTargets);
                _overlay.SetTargetInputEnabled(false);
            }
            bool available = true;
            foreach (var target in _shownTargets)
                if (!target || !target.gameObject.activeInHierarchy) { available = false; break; }
            if (available && _overlay.TargetsAvailable) _missingSince = -1;
            else MissingTarget();
            if (_missingSince >= 0 && Time.unscaledTime - _missingSince >= .2f)
                Fail("Tutorial target is hidden or missing.");
        }

        void MissingTarget()
        {
            if (_running && _missingSince < 0) _missingSince = Time.unscaledTime;
        }

        void Fail(string error)
        {
            Debug.LogError(error, this);
            ShowError(_overlay, error);
        }

        public void ShowError(TutorialOverlay overlay, string error)
        {
            Unsubscribe();
            _running = false;
            _error = true;
            if (_screen) _screen.ClearTutorialColleagueGreeting();
            _overlay = overlay;
            if (!_overlay) return;
            _overlay.ContinueRequested -= Continue;
            _overlay.ContinueRequested += Continue;
            _overlay.SetContinueWithAnyKey(false);
            _overlay.SetSupervisor(false);
            _overlay.Show("튜토리얼을 진행할 수 없습니다. 메인 화면으로 돌아가기\n" + error, true);
        }

        void Return(bool completed)
        {
            _returning = true;
            _running = false;
            Unsubscribe();
            if (_screen) { _screen.ClearTutorialColleagueGreeting(); _screen.DisposeSession(); }
            if (completed)
            {
                TutorialProgress.MarkSeen();
                Ohabjizol.Presentation.SceneBgmPlayer.UnlockLobbyMusicForSession();
            }
            TutorialSceneFlow.ReturnMain();
        }

        void OnDisable()
        {
            StopAllCoroutines();
            _running = false;
            Unsubscribe();
            if (_screen) { _screen.ClearTutorialColleagueGreeting(); _screen.DisposeSession(); }
        }
    }
}
