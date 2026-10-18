#if UNITY_EDITOR
using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using Ohabjizol.Presentation;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Ohabjizol.Interview.Diagnostics
{
    public sealed class InterviewRefreshRuntimeProbe : MonoBehaviour
    {
        void Start() { StartCoroutine(Guard()); }
        IEnumerator Guard()
        {
            IEnumerator checks = Checks();
            while (true)
            {
                Exception failure = null;
                bool next = false;
                object current = null;
                try { next = checks.MoveNext(); if (next) current = checks.Current; }
                catch (Exception error) { failure = error; }
                if (failure != null) { Finish("FAIL: " + failure); yield break; }
                if (!next) { Finish("PASS: monitor boot, downloads, double-click, minimize, question timing, disable cancellation/re-enable, applicant cleanup, four-stage failure progression and ending reset."); yield break; }
                yield return current;
            }
        }

        IEnumerator Checks()
        {
            yield return null;
            var menu = FindFirstObjectByType<MainMenuController>();
            Check(menu != null && menu.enabled && menu.HasAuthoredUi, "Authored main menu did not initialize.");
            menu.MonitorClicked();
            yield return new WaitForSecondsRealtime(3.3f);
            var desktop = All<DesktopView>().FirstOrDefault();
            Check(desktop != null && desktop.gameObject.activeInHierarchy, "Monitor click/boot did not reach desktop.");
            menu.StartNewRun();
            menu.Launch(StageId.EntranceGameJam);
            float deadline = Time.realtimeSinceStartup + 30f;
            while (SceneManager.GetActiveScene().name != "InterviewScene" && Time.realtimeSinceStartup < deadline) yield return null;
            Check(SceneManager.GetActiveScene().name == "InterviewScene", "Desktop launch/loading transition did not reach interview.");
            yield return null;
            yield return null;
            var screen = FindFirstObjectByType<InterviewScreen>();
            Check(screen != null && screen.enabled && screen.HasAuthoredUi && !string.IsNullOrEmpty(screen.ApplicantKey), "Authored interview did not initialize.");
            Check(screen.UsedQuestionCount == 0 && screen.InboxUnread && !screen.IsAnswering, "Applicant initial state invalid.");
            Check(All<DocumentTaskbarItem>().Length == 0, "Documents appeared before download.");
            screen.OpenDocumentPanel();
            yield return new WaitForSecondsRealtime(.35f);
            Check(!screen.InboxUnread, "Opening panel did not clear notification.");
            var panel = All<DownloadPanelView>().First();
            Get<Button>(panel, "_applicationButton").onClick.Invoke();
            Get<Button>(panel, "_applicationButton").onClick.Invoke();
            Check(All<DocumentTaskbarItem>().Length == 1, "Duplicate download created another PDF item.");
            Get<Button>(panel, "_portfolioButton").onClick.Invoke();
            Check(All<DocumentTaskbarItem>().Length == 2, "Portfolio download failed.");
            var item = All<DocumentTaskbarItem>().First(value => value.Kind == DocumentKind.Application);
            var pointer = new PointerEventData(EventSystem.current) { button = PointerEventData.InputButton.Left, clickCount = 1 };
            item.OnPointerClick(pointer);
            Check(screen.Documents.GetState(DocumentKind.Application) == DocumentViewerState.Closed, "Single click opened PDF.");
            pointer.clickCount = 2;
            item.OnPointerClick(pointer);
            Check(screen.Documents.GetState(DocumentKind.Application) == DocumentViewerState.Normal, "Double click did not open PDF.");
            var viewer = All<PdfDocumentViewer>().First();
            Vector2 position = viewer.GetComponent<RectTransform>().anchoredPosition;
            viewer.Minimize();
            Check(screen.Documents.GetState(DocumentKind.Application) == DocumentViewerState.Minimized && All<DocumentTaskbarItem>().Length == 2, "Minimize discarded document.");
            item.OnPointerClick(pointer);
            Check(screen.Documents.GetState(DocumentKind.Application) == DocumentViewerState.Normal && viewer.GetComponent<RectTransform>().anchoredPosition == position, "Restore changed viewer position.");
            string previous = screen.ApplicantKey;
            screen.AskQuestion(0);
            Check(screen.IsAnswering && screen.UsedQuestionCount == 1, "Question did not consume/start.");
            var call = All<InterviewCallView>().First();
            Image portrait = Get<Image>(call, "_portrait");
            Sprite initial = portrait.sprite;
            yield return new WaitForSecondsRealtime(.65f);
            Check(portrait.sprite != null && portrait.sprite != initial, "Talk animation did not change motion.");
            screen.RejectApplicant();
            yield return null;
            Check(screen.ApplicantKey != previous && screen.UsedQuestionCount == 0 && !screen.IsAnswering && screen.InboxUnread, "Decision did not reset applicant state.");
            Check(All<DocumentTaskbarItem>().Length == 0 && All<PdfDocumentViewer>().Length == 0, "Decision left old document windows/items.");
            var panelRect = Get<RectTransform>(panel, "_panelRect");
            Check(panelRect.anchoredPosition == Get<Vector2>(panel, "_closedPosition"), "Decision left slide panel open.");
            yield return new WaitForSecondsRealtime(3.1f);
            Check(string.IsNullOrEmpty(Get<Text>(screen, "_response").text), "Cancelled answer leaked into new applicant.");
            screen.AskQuestion(0);
            yield return new WaitForSecondsRealtime(3.2f);
            Check(!screen.IsAnswering && screen.UsedQuestionCount == 1 && !string.IsNullOrEmpty(Get<Text>(screen, "_response").text), "Question completion failed.");
            Check(Get<Text>(call, "_dots").text == "", "Dots remained after response.");
            Sprite idle = Get<Sprite>(call, "_idleSprite");
            Check(portrait.sprite == idle, "Portrait did not return to Idle.");
            screen.AskQuestion(0);
            Check(!screen.IsAnswering && screen.UsedQuestionCount == 1, "Used question was consumed twice.");
            // Disabling cancels the pending reservation without replacing the current applicant.
            previous = screen.ApplicantKey;
            var displayState = screen.DisplayedPortfolioState;
            int usedBeforeCancel = screen.UsedQuestionCount;
            screen.AskQuestion(1);
            Check(screen.IsAnswering && screen.UsedQuestionCount == usedBeforeCancel + 1, "Pending question did not reserve a slot.");
            yield return new WaitForSecondsRealtime(.65f);
            screen.enabled = false;
            Check(!screen.IsAnswering && screen.UsedQuestionCount == usedBeforeCancel, "Disable failed to roll back the pending slot.");
            Check(screen.ApplicantKey == previous && screen.DisplayedPortfolioState == displayState, "Disable changed applicant identity/presentation.");
            screen.enabled = true;
            Check(screen.ApplicantKey == previous && screen.DisplayedPortfolioState == displayState
                && screen.UsedQuestionCount == usedBeforeCancel && !screen.IsAnswering && screen.InboxUnread,
                "Re-enable replaced the applicant or reset completed questions/notification.");
            Check(Get<Text>(call, "_dots").text == "", "Re-enable retained answering dots.");
            yield return new WaitForSecondsRealtime(3.1f);
            Check(string.IsNullOrEmpty(Get<Text>(screen, "_response").text), "Disabled answer published after re-enable.");
            screen.AskQuestion(1);
            Check(screen.IsAnswering && screen.UsedQuestionCount == usedBeforeCancel + 1, "Cancelled question could not be asked again.");
            yield return new WaitForSecondsRealtime(3.2f);
            Check(!screen.IsAnswering && screen.UsedQuestionCount == usedBeforeCancel + 1
                && !string.IsNullOrEmpty(Get<Text>(screen, "_response").text), "Retried answer failed after disable.");
            string completedResponse = Get<Text>(screen, "_response").text;
            // Repeated subscription cycles must preserve the response and allow only one decision.
            screen.enabled = false;
            screen.enabled = true;
            screen.enabled = false;
            screen.enabled = true;
            Check(screen.ApplicantKey == previous && screen.DisplayedPortfolioState == displayState
                && screen.UsedQuestionCount == usedBeforeCancel + 1
                && Get<Text>(screen, "_response").text == completedResponse, "Repeated re-enable discarded completed answer state.");
            previous = screen.ApplicantKey;
            screen.AcceptApplicant();
            yield return null;
            Check(screen.ApplicantKey != previous && screen.UsedQuestionCount == 0, "Accept required all five questions.");
            for (int stage = 0; stage < 4; stage++)
            {
                int budget = 300;
                while (SceneManager.GetActiveScene().name == "InterviewScene" && screen.Result == null && budget-- > 0)
                {
                    screen.RejectApplicant();
                    yield return null;
                }
                deadline = Time.realtimeSinceStartup + 10f;
                while (!ResultSceneRoute.IsResultScene(SceneManager.GetActiveScene().name) && Time.realtimeSinceStartup < deadline) yield return null;
                yield return null;
                var board = FindFirstObjectByType<ResultBoard>();
                var run = CampaignRunStore.Load();
                Check(board != null && board.Result != null && board.Result.Stage == (StageId)stage && !board.Result.IsCleared, "Failed stage result did not arrive.");
                Check(run.IsCompleted((StageId)stage), "Failed stage was not marked completed.");
                Check(stage == 3 ? run.IsComplete : run.NextStage == (StageId)(stage + 1), "Failed stage did not advance.");
                Get<Button>(board, "continueButton").onClick.Invoke();
                yield return null;
                yield return null;
                if (stage == 3)
                {
                    Check(SceneManager.GetActiveScene().name == "EndingScene" && FindFirstObjectByType<EndingBoard>() != null && run.Ending == EndingKind.Bad, "Final result did not open bad ending.");
                    var ending = FindFirstObjectByType<EndingBoard>();
                    Get<Button>(ending, "newRunButton").onClick.Invoke();
                    yield return null;
                    Check(SceneManager.GetActiveScene().name == "MainMenuScene" && !CampaignRunStore.Load().IsComplete && CampaignRunStore.Load().NextStage == StageId.EntranceGameJam, "Ending new run did not reset campaign.");
                }
                else
                {
                    Check(SceneManager.GetActiveScene().name == "MainMenuScene", "Intermediate result did not return to desktop.");
                    menu = FindFirstObjectByType<MainMenuController>();
                    menu.Launch((StageId)(stage + 1));
                    deadline = Time.realtimeSinceStartup + 30f;
                    while (SceneManager.GetActiveScene().name != "InterviewScene" && Time.realtimeSinceStartup < deadline) yield return null;
                    Check(SceneManager.GetActiveScene().name == "InterviewScene", "Next stage loading did not complete.");
                    yield return null;
                    yield return null;
                    screen = FindFirstObjectByType<InterviewScreen>();
                    Check(screen != null && screen.enabled, "Next stage screen missing.");
                }
            }
        }

        static T[] All<T>() where T : Component
        {
            return SceneManager.GetActiveScene().GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<T>(true)).ToArray();
        }
        static T Get<T>(object target, string field)
        {
            var member = target.GetType().GetField(field, BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance);
            if (member == null) throw new InvalidOperationException("Diagnostic field missing: " + field);
            return (T)member.GetValue(target);
        }
        static void Check(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
        static void Finish(string message)
        {
            SessionState.SetString("InterviewRefresh.Runtime.Result", message);
            if (message.StartsWith("FAIL")) Debug.LogError("[InterviewRefreshRuntimeProbe] " + message);
            else Debug.Log("[InterviewRefreshRuntimeProbe] " + message);
            EditorApplication.isPlaying = false;
        }
    }
}
#endif