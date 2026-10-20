using System;
using Ohabjizol.UI;
using UnityEngine;
using UnityEngine.UI;

namespace Ohabjizol.Interview
{
    public sealed partial class InterviewScreen
    {
        [SerializeField] InterviewTutorialTargets _tutorialTargets;

        public IInterviewTutorialTargets TutorialTargets
        {
            get
            {
                if (!_tutorialTargets)
                    throw new InvalidOperationException("InterviewScreen tutorial targets are not authored.");
                _tutorialTargets.Validate(this);
                if (_tutorialTargets.Downloads != _downloadPanel || _tutorialTargets.Questions != _radialQuestions ||
                    _questions == null || _questions.Length != 5)
                    throw new InvalidOperationException("Tutorial targets do not match their Screen views.");
                for (int i = 0; i < _questions.Length; i++)
                    if (!_questions[i] || _questions[i].Button != _tutorialTargets.QuestionButtons[i])
                        throw new InvalidOperationException("Tutorial questions must follow Screen question order.");
                return _tutorialTargets;
            }
        }

        public bool CanAskQuestion(int index) => _runtime != null && _runtime.CanAskQuestion(index);

#if UNITY_EDITOR
        public void ConfigureTutorialTargetsEditor()
        {
            if (Application.isPlaying || UnityEditor.EditorApplication.isPlayingOrWillChangePlaymode)
                throw new InvalidOperationException("Tutorial target authoring requires EditMode.");
            if (!_downloadPanel || !_workspace || !_workspace.Library || !_radialQuestions || !_call ||
                !_calendarWindow || !_notepadWindow || _questions == null || _questions.Length != 5)
                throw new InvalidOperationException("Author the Screen views before binding tutorial targets.");

            var teamLauncher = UniqueTutorialComponentEditor<InterviewTeamLauncher>();
            var teamWindow = TutorialReferenceEditor<InterviewTeamWindow>(teamLauncher, "_window");
            var fixedLauncher = TutorialReferenceEditor<RadialQuestionLauncher>(_radialQuestions, "_fixedLauncher");
            var questionButtons = new Button[5];
            for (int i = 0; i < questionButtons.Length; i++)
            {
                if (!_questions[i] || !_questions[i].Button)
                    throw new InvalidOperationException("A Screen question button is missing.");
                questionButtons[i] = _questions[i].Button;
            }
            Button application = null, portfolio = null;
            foreach (var item in _downloadPanel.Items)
            {
                if (!item) continue;
                if (item.Kind == DocumentKind.Application) application = item.Button;
                if (item.Kind == DocumentKind.Portfolio) portfolio = item.Button;
            }
            var buttons = new[] { _chatButton, application, portfolio,
                TutorialReferenceEditor<Button>(_downloadPanel, "_closeButton"),
                TutorialReferenceEditor<Button>(_workspace, "libraryButton"),
                TutorialReferenceEditor<Button>(_workspace.Library, "_closeButton"),
                TutorialReferenceEditor<Button>(fixedLauncher, "_button"),
                TutorialReferenceEditor<Button>(teamLauncher, "_button"), _calendarButton, _notepadButton };
            foreach (var button in buttons)
                if (!button || !button.transform.IsChildOf(transform))
                    throw new InvalidOperationException("A tutorial launcher/control is missing or belongs to another Screen.");
            UnityEditor.Undo.RecordObject(this, "Bind Screen tutorial targets");
            if (!_tutorialTargets)
            {
                var existing = GetComponentsInChildren<InterviewTutorialTargets>(true);
                if (existing.Length > 1)
                    throw new InvalidOperationException("Multiple tutorial target bindings exist.");
                if (existing.Length == 1) _tutorialTargets = existing[0];
                else
                {
                    var child = new GameObject("InterviewTutorialTargets");
                    UnityEditor.Undo.RegisterCreatedObjectUndo(child, "Create tutorial targets");
                    child.transform.SetParent(transform, false);
                    _tutorialTargets = UnityEditor.Undo.AddComponent<InterviewTutorialTargets>(child);
                }
            }
            if (_tutorialTargets.transform.parent != transform)
                throw new InvalidOperationException("Tutorial target binding must be a direct ScreenRoot child.");
            _tutorialTargets.ConfigureEditor(buttons, questionButtons, _call.transform as RectTransform,
                _windowParent, _downloadPanel, _radialQuestions,
                new WindowToolbar[] { _workspace.Library, teamWindow, _calendarWindow, _notepadWindow });
            _ = TutorialTargets;
            TrackTutorialContentEditor(_discordButton, _discordViewRoot.gameObject);
            TrackTutorialContentEditor(buttons[4],
                TutorialReferenceEditor<RectTransform>(_workspace.Library, "_info").gameObject);
            TrackTutorialContentEditor(_calendarButton,
                TutorialReferenceEditor<RectTransform>(_calendarWindow, "_info").gameObject);
            UnityEditor.EditorUtility.SetDirty(this);
            UnityEditor.PrefabUtility.RecordPrefabInstancePropertyModifications(this);
        }

        static void TrackTutorialContentEditor(Button button, GameObject content)
        {
            if (!button || !content)
                throw new InvalidOperationException("A visible-content binding is missing.");
            var background = ButtonActiveBackground.Ensure(button);
            UnityEditor.Undo.RecordObject(background, "Track visible window content");
            background.TrackVisibleContent(content);
            UnityEditor.EditorUtility.SetDirty(background);
            UnityEditor.PrefabUtility.RecordPrefabInstancePropertyModifications(background);
        }

        T UniqueTutorialComponentEditor<T>() where T : Component
        {
            var components = GetComponentsInChildren<T>(true);
            if (components.Length != 1)
                throw new InvalidOperationException("Expected exactly one authored tutorial binding component.");
            return components[0];
        }

        static T TutorialReferenceEditor<T>(UnityEngine.Object source, string propertyName) where T : UnityEngine.Object
        {
            if (!source) throw new InvalidOperationException("Tutorial binding source is missing: " + propertyName);
            var serialized = new UnityEditor.SerializedObject(source);
            var property = serialized.FindProperty(propertyName);
            var reference = property == null ? null : property.objectReferenceValue as T;
            if (!reference) throw new InvalidOperationException(source.name + " is missing " + propertyName + ".");
            return reference;
        }
#endif
    }
}
