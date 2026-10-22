using System;
using System.Linq;
using Ohabjizol.Interview;
using Ohabjizol.Presentation;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class InterviewRefreshValidation
{
    public static void Validate()
    {
        Check(!EditorApplication.isPlayingOrWillChangePlaymode, "Static validation requires Edit Mode.");
        Check(StarRatingRule.HalfUnits(0) == 0 && StarRatingRule.HalfUnits(65) == 7 && StarRatingRule.HalfUnits(70) == 7 && StarRatingRule.HalfUnits(75) == 8 && StarRatingRule.HalfUnits(100) == 10, "Half-star boundaries failed.");
        Check(!StageAccuracyRule.IsClear(65) && StageAccuracyRule.IsClear(70), "Clear threshold changed.");
        foreach (int percent in new[] { 40, 60, 90 })
        {
            var run = new CampaignRunProgress("validation-" + percent);
            for (int i = 0; i < 4; i++)
            {
                Check(run.NextStage == (StageId)i && run.Record((StageId)i, percent), "Stage did not advance after completion.");
                int total = run.TotalHalfStars;
                Check(!run.Record((StageId)i, 100) && run.TotalHalfStars == total, "Duplicate stage result added stars.");
            }
            Check(run.IsComplete, "Four-stage run did not complete.");
            EndingKind expected = percent == 40 ? EndingKind.Bad : percent == 60 ? EndingKind.Normal : EndingKind.Good;
            Check(run.Ending == expected, "Ending partition failed.");
        }
        foreach (JobRole role in Enum.GetValues(typeof(JobRole)))
        {
            var questions = InterviewQuestionCatalog.GetQuestions(role);
            Check(questions != null && questions.Count == InterviewQuestionCatalog.QuestionCount && questions.Select(question => question.Id).Distinct().Count() == InterviewQuestionCatalog.QuestionCount, "Role question catalog invalid.");
        }
        var authoredQuestions = AssetDatabase.LoadAssetAtPath<AuthoredQuestionCatalog>("Assets/Art/InterviewRefresh/QuestionCatalog.asset");
        Check(authoredQuestions != null && authoredQuestions.QuestionCount == 3 * InterviewQuestionCatalog.QuestionCount && authoredQuestions.ResponseCount == 3 * InterviewQuestionCatalog.QuestionCount * 3, "Authored questions/responses missing.");
        Check(!typeof(ApplicantDocumentPacket).GetProperties().Any(property => property.Name.Contains("Actual") || property.Name.Contains("True")), "Hidden data exposed through public document packet.");
        var catalog = AssetDatabase.LoadAssetAtPath<PortfolioSpriteCatalog>("Assets/Art/InterviewRefresh/PortfolioCatalog.asset");
        Check(catalog != null, "Portfolio catalog missing.");
        foreach (JobRole role in Enum.GetValues(typeof(JobRole)))
            foreach (PortfolioDisplayState state in Enum.GetValues(typeof(PortfolioDisplayState)))
                Check(catalog.GetCard(role, state) != null, "Portfolio card missing: " + role + "/" + state);
        VerifyScene<MainMenuController>("MainMenuScene");
        VerifyScene<InterviewScreen>("InterviewScene");
        VerifyScene<ResultBoard>("ResultScene");
        VerifyScene<ResultBoard>(Ohabjizol.Interview.ResultSceneRoute.GoodSceneName);
        VerifyScene<ResultBoard>(Ohabjizol.Interview.ResultSceneRoute.NormalSceneName);
        VerifyScene<ResultBoard>(Ohabjizol.Interview.ResultSceneRoute.BadSceneName);
        VerifyScene("EndingScene", "Ohabjizol.Interview.EndingBoard");
        Check(EditorBuildSettings.scenes.Any(scene => scene.enabled && scene.path == "Assets/Scenes/EndingScene.unity"), "Ending scene not enabled in build.");
        Debug.Log("[InterviewRefreshValidation] PASS: authoring, hidden boundary, questions, stars, progression and endings.");
    }

    static void VerifyScene<T>(string name) where T : MonoBehaviour => VerifyScene(name, typeof(T).FullName);
    static void VerifyScene(string name, string typeName)
    {
        var scene = EditorSceneManager.OpenPreviewScene("Assets/Scenes/" + name + ".unity");
        try
        {
            MonoBehaviour controller = null;
            foreach (var root in scene.GetRootGameObjects())
            {
                foreach (var component in root.GetComponentsInChildren<MonoBehaviour>(true))
                    if (component != null && component.GetType().FullName == typeName) { Check(controller == null, "Duplicate screen controller."); controller = component; }
                foreach (Text text in root.GetComponentsInChildren<Text>(true)) Check(text.font != null && EditorUtility.IsPersistent(text.font), "Unpersisted font in " + name + ": " + text.name);
            }
            Check(controller != null, "Authored controller missing: " + typeName);
            var property = controller.GetType().GetProperty("HasAuthoredUi");
            Check(property != null && (bool)property.GetValue(controller), "Authored references missing in " + name);
            foreach (var view in scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<InterviewCallView>(true)))
            {
                var serialized = new SerializedObject(view);
                Check(serialized.FindProperty("_idleSprite").objectReferenceValue != null && serialized.FindProperty("_talkSprite").objectReferenceValue != null, "Portrait motion assets not saved.");
            }
        }
        finally { EditorSceneManager.ClosePreviewScene(scene); }
    }
    public static void Check(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
}