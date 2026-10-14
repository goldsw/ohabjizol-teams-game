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
        readonly RadialQuestionView _radial;
        Coroutine _answer;

        public InterviewQuestionPresenter(MonoBehaviour host, QuestionController controller,
            AuthoredQuestionCatalog catalog, InterviewQuestionView[] views, InterviewCallView call, Text response, RadialQuestionView radial = null)
        { _host = host; _controller = controller; _catalog = catalog; _views = views; _call = call; _response = response; _radial = radial; }

        public void Subscribe() { _controller.Changed += Render; }
        public void Unsubscribe() { _controller.Changed -= Render; Cancel(); if (_radial) _radial.SetAvailable(false); }

        public void Bind(Applicant applicant)
        {
            Cancel();
            if (_radial) _radial.ResetCollapsed();
            _controller.Bind(applicant, applicant == null ? null : InterviewQuestionCatalog.GetQuestions(applicant.PrimaryRole));
        }

        public void Ask(int index)
        {
            if (!_host.isActiveAndEnabled) return;
            var request = _controller.TryBegin(index);
            if (request != null) _answer = _host.StartCoroutine(Answer(request));
        }

        public void Cancel()
        {
            _controller.Cancel(); // Invalidate before stopping so an old iterator cannot publish.
            if (_answer != null && _host) _host.StopCoroutine(_answer);
            _answer = null;
            _call.SetAnswering(false, 0);
        }

        public void Render()
        {
            _response.text = _controller.Response;
            if (_radial) _radial.SetAvailable(_controller.Applicant != null);
            for (int i = 0; i < _views.Length; i++)
            {
                var definitions = _controller.Definitions;
                _views[i].Bind(definitions != null && i < definitions.Count ? definitions[i].Prompt : "");
                _views[i].Button.interactable = _controller.CanAsk(i);
            }
            if (!_controller.IsAnswering) _call.SetAnswering(false, 0);
        }

        IEnumerator Answer(QuestionController.AnswerRequest request)
        {
            float elapsed = 0f;
            while (elapsed < 3f)
            {
                if (!_controller.IsCurrent(request)) { _call.SetAnswering(false, 0); yield break; }
                _call.SetAnswering(true, Mathf.FloorToInt(elapsed / .5f));
                yield return null;
                elapsed += Time.unscaledDeltaTime;
            }
            if (!_controller.IsCurrent(request)) { _call.SetAnswering(false, 0); yield break; }
            try { _controller.Complete(request); }
            catch (Exception error)
            {
                _controller.Cancel();
                Debug.LogException(error, _host);
            }
            _answer = null;
            _call.SetAnswering(false, 0);
        }
    }
}