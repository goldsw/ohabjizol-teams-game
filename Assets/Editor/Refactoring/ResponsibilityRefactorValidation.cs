#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using Ohabjizol.Interview;
using UnityEditor;
using UnityEngine;

public static class ResponsibilityRefactorValidation
{
    public static void Validate()
    {
        InterviewRefreshValidation.Validate();
        ValidateDomainLifecycles();
        ValidatePersistence();
        Debug.Log("[ResponsibilityRefactorValidation] PASS: authored UI, question cancellation, completion retry, legacy migration, journal recovery and run isolation.");
    }


    static void ValidateDomainLifecycles()
    {
        var definition = StageCatalog.Get(StageId.EntranceGameJam);
        var applicant = definition.Applicants[0];
        var questions = new QuestionController();
        questions.Bind(applicant, InterviewQuestionCatalog.GetQuestions(applicant.PrimaryRole));
        var abandoned = questions.TryBegin(0);
        Check(abandoned != null && questions.IsAnswering && questions.UsedQuestionCount == 1, "Question reservation failed.");
        questions.Cancel();
        Check(!questions.IsAnswering && questions.UsedQuestionCount == 0, "Cancellation did not release the question.");
        Check(!questions.Complete(abandoned, "old response"), "Cancelled response was accepted.");
        var retried = questions.TryBegin(0);
        Check(retried != null && questions.Complete(retried, "completed"), "Cancelled question could not be retried.");
        questions.Cancel();
        Check(questions.UsedQuestionCount == 1 && questions.Response == "completed", "Cancelling an idle view erased a completed answer.");
        questions.Bind(applicant, InterviewQuestionCatalog.GetQuestions(applicant.PrimaryRole));
        Check(questions.UsedQuestionCount == 0 && questions.Response == "", "Applicant reset retained questions.");

        int attempts = 0;
        var manager = new StageManager(new StageContext("refactor-domain-check", definition), (runId, result) =>
        {
            attempts++;
            if (attempts == 1) throw new InvalidOperationException("Injected completion failure.");
        });
        manager.Start();
        int remaining = definition.Applicants.Count + 1;
        while (!manager.Session.IsFinished && remaining-- > 0) manager.Decide(false);
        Check(manager.Session.IsFinished && manager.Result == null && manager.CompletionPending, "Uncommitted result became visible.");
        manager.RetryCompletion();
        Check(manager.Result != null && !manager.CompletionPending && attempts == 2, "Completion retry did not publish the result.");
        manager.RetryCompletion();
        Check(attempts == 2, "Committed completion was repeated.");
    }

    static void ValidatePersistence()
    {
        var saved = new Dictionary<string, string>();
        foreach (string key in StageCompletionService.PersistenceKeys)
            saved[key] = PlayerPrefs.HasKey(key) ? PlayerPrefs.GetString(key) : null;
        try
        {
            ClearOwnedKeys();
            const string legacy = "{\"cleared\":[true,false],\"best\":[82,55]}";
            PlayerPrefs.SetString("ohabjizol.stage-progress.v1", legacy);
            PlayerPrefs.Save();
            var migrated = StageProgressStore.Load();
            Check(migrated.IsCleared(StageId.EntranceGameJam) && migrated.BestAccuracy(StageId.EntranceGameJam) == 82, "Legacy first record lost.");
            Check(!migrated.IsCleared(StageId.GameProgrammingIntro) && migrated.BestAccuracy(StageId.GameProgrammingIntro) == 55, "Legacy partial record lost.");
            Check(migrated.BestAccuracy(StageId.SecondYear) == -1, "New stage default incorrect.");
            Check(PlayerPrefs.GetString("ohabjizol.stage-progress.v1") == legacy, "Migration overwrote the original record.");
            migrated.Record(StageId.SecondYear, 91);
            StageProgressStore.Save(migrated);
            Check(StageProgressStore.Load().BestAccuracy(StageId.SecondYear) == 91, "Identity-based progress round trip failed.");

            ClearOwnedKeys();
            var run = CampaignRunStore.StartNewRun();
            var first = Result(StageId.EntranceGameJam, 65);
            StageCompletionService.Complete(run.RunId, first);
            var once = CampaignRunStore.Load();
            int halfStars = once.TotalHalfStars;
            Check(once.NextStage == StageId.GameProgrammingIntro, "A failed stage did not advance.");
            Check(!StageProgressStore.Load().IsCleared(StageId.EntranceGameJam), "A failed stage was marked clear.");
            StageCompletionService.Complete(run.RunId, first);
            StageCompletionService.RecoverPending();
            var twice = CampaignRunStore.Load();
            Check(twice.NextStage == once.NextStage && twice.TotalHalfStars == halfStars, "Duplicate completion changed campaign progress.");
            ExpectRejected(() => StageCompletionService.Complete(run.RunId, Result(StageId.EntranceGameJam, 100)), "Conflicting duplicate accepted.");
            Check(StageProgressStore.Load().BestAccuracy(StageId.EntranceGameJam) == 65, "Conflicting duplicate changed lifetime progress.");


            const string journalKey = "ohabjizol.stage-completion-journal.v1";
            var journal = new JournalFixture { version = 1, runId = run.RunId,
                stageId = StageId.GameProgrammingIntro.ToString(), percentage = 72 };
            var partial = StageProgressStore.Load();
            partial.Record(StageId.GameProgrammingIntro, 72);
            StageProgressStore.Save(partial);
            PlayerPrefs.SetString(journalKey, JsonUtility.ToJson(journal));
            PlayerPrefs.Save();
            StageCompletionService.RecoverPending();
            var recovered = CampaignRunStore.Load();
            Check(recovered.IsCompleted(StageId.GameProgrammingIntro) && recovered.NextStage == StageId.SecondYear, "Pending completion did not recover.");
            int recoveredStars = recovered.TotalHalfStars;
            PlayerPrefs.SetString(journalKey, JsonUtility.ToJson(journal));
            PlayerPrefs.Save();
            StageCompletionService.RecoverPending();
            Check(!PlayerPrefs.HasKey(journalKey) && CampaignRunStore.Load().TotalHalfStars == recoveredStars, "Recovery duplicated an already-applied result.");

            var nextRun = CampaignRunStore.StartNewRun();
            ExpectRejected(() => StageCompletionService.Complete(run.RunId, first), "Stale run completion accepted.");
            Check(CampaignRunStore.Load().RunId == nextRun.RunId && !CampaignRunStore.Load().IsCompleted(StageId.EntranceGameJam), "Old completion contaminated a new run.");
            PlayerPrefs.SetString(journalKey, JsonUtility.ToJson(journal));
            PlayerPrefs.Save();
            StageCompletionService.RecoverPending();
            Check(!PlayerPrefs.HasKey(journalKey) && !CampaignRunStore.Load().IsCompleted(StageId.GameProgrammingIntro), "Stale journal changed the new campaign.");

            ClearOwnedKeys();
            const string futureRecord = "{\"version\":3,\"entries\":[]}";
            PlayerPrefs.SetString("ohabjizol.stage-progress.v2", futureRecord);
            PlayerPrefs.SetString("ohabjizol.stage-progress.v1", legacy);
            PlayerPrefs.Save();
            bool futureSchemaRejected = false;
            try { StageProgressStore.Load(); }
            catch (NotSupportedException) { futureSchemaRejected = true; }
            Check(futureSchemaRejected, "Unsupported future schema accepted.");
            Check(PlayerPrefs.GetString("ohabjizol.stage-progress.v2") == futureRecord, "Unsupported future schema overwritten.");

        }
        finally
        {
            foreach (var pair in saved)
                if (pair.Value == null) PlayerPrefs.DeleteKey(pair.Key); else PlayerPrefs.SetString(pair.Key, pair.Value);
            PlayerPrefs.Save();
        }
    }

    static InterviewResult Result(StageId stage, int percent) =>
        new InterviewResult(new SkillSet(0, 0, 0), percent, 0, 0, 0, (ResultGrade)0,
            stage, new SkillSet(1, 1, 1), percent, StageAccuracyRule.IsClear(percent));

    [Serializable] sealed class JournalFixture
    {
        public int version;
        public string runId;
        public string stageId;
        public int percentage;
    }

    static void ClearOwnedKeys()
    {
        foreach (string key in StageCompletionService.PersistenceKeys) PlayerPrefs.DeleteKey(key);
        PlayerPrefs.Save();
    }

    static void ExpectRejected(Action action, string message)
    {
        try { action(); }
        catch (ArgumentException) { return; }
        catch (InvalidOperationException) { return; }
        throw new InvalidOperationException(message);
    }

    static void Check(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
#endif

