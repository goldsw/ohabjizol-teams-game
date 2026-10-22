using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace Ohabjizol.Interview
{
    public sealed class InterviewQuestionPresenter
    {
        readonly MonoBehaviour _host;
        readonly QuestionController _controller;
        readonly AuthoredQuestionCatalog _catalog;
        readonly InterviewQuestionView[] _views;
        readonly InterviewCallView _call;
        readonly Text _response;
        readonly InterviewResponsePanel _responsePanel;
        readonly RadialQuestionView _radial;
        readonly InterviewApplicantPresenter _applicant;
        readonly bool _collapseOnAsk;
        Coroutine _answer;
        bool _inputBlocked;

        // The tutorial points at both question buttons in turn, so it keeps the options open.
        public InterviewQuestionPresenter(MonoBehaviour host, QuestionController controller,
            AuthoredQuestionCatalog catalog, InterviewQuestionView[] views, InterviewCallView call, Text response,
            RadialQuestionView radial = null, InterviewApplicantPresenter applicant = null, bool collapseOnAsk = false)
        {
            _host = host; _controller = controller; _catalog = catalog; _views = views;
            _call = call; _response = response; _radial = radial; _applicant = applicant;
            _responsePanel = InterviewResponsePanel.Ensure(response);
            _collapseOnAsk = collapseOnAsk;
        }

        public void Subscribe() { _controller.Changed += Render; }
        public void Unsubscribe()
        {
            _controller.Changed -= Render;
            Cancel();
            if (_radial)
            {
                _radial.SetAvailable(false);
                _radial.ResetCollapsed();
            }
        }

        public void Bind(Applicant applicant)
        {
            Cancel();
            if (_radial) _radial.ResetCollapsed();
            _controller.Bind(applicant, applicant == null ? null : InterviewQuestionCatalog.GetQuestions(applicant.PrimaryRole));
        }

        public void SetInputBlocked(bool blocked)
        {
            _inputBlocked = blocked;
            Render();
        }

        public void Ask(int index)
        {
            if (_inputBlocked || !_host.isActiveAndEnabled) return;
            var request = _controller.TryBegin(index);
            if (request == null) return;
            if (_collapseOnAsk && _radial) _radial.CollapseContent();
            _answer = _host.StartCoroutine(Answer(request));
        }

        public void Cancel()
        {
            _controller.Cancel(); // Invalidate before stopping so an old iterator cannot publish.
            if (_answer != null && _host) _host.StopCoroutine(_answer);
            _answer = null;
            SetAnswering(false, 0);
        }

        public void Render()
        {
            if (_responsePanel) _responsePanel.Present(_controller.Response);
            else _response.text = _controller.Response;
            if (_radial) _radial.SetAvailable(_controller.Applicant != null);
            for (int i = 0; i < _views.Length; i++)
            {
                var definitions = _controller.Definitions;
                _views[i].Bind(definitions != null && i < definitions.Count ? definitions[i].Prompt : "");
                _views[i].Button.interactable = !_inputBlocked && _controller.CanAsk(i);
                _views[i].SetUsed(_controller.IsUsed(i));
            }
            if (!_controller.IsAnswering) SetAnswering(false, 0);
        }

        void SetAnswering(bool answering, int tick)
        {
            _call.SetAnswering(answering, tick);
            _applicant?.SetAnswering(answering, tick);
        }

        IEnumerator Answer(QuestionController.AnswerRequest request)
        {
            float elapsed = 0f;
            while (elapsed < 3f)
            {
                if (!_controller.IsCurrent(request)) { SetAnswering(false, 0); yield break; }
                SetAnswering(true, Mathf.FloorToInt(elapsed / .5f));
                yield return null;
                elapsed += Time.unscaledDeltaTime;
            }
            if (!_controller.IsCurrent(request)) { SetAnswering(false, 0); yield break; }
            try { _controller.Complete(request); }
            catch (Exception error)
            {
                _controller.Cancel();
                Debug.LogException(error, _host);
            }
            _answer = null;
            SetAnswering(false, 0);
        }
    }
}
