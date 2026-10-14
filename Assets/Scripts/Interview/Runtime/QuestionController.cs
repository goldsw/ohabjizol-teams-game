using System;
using System.Collections.Generic;

namespace Ohabjizol.Interview
{
    public sealed class QuestionController
    {
        public sealed class AnswerRequest
        {
            internal AnswerRequest(Applicant applicant, int index, int version, string responseText)
            { Applicant = applicant; Index = index; Version = version; ResponseText = responseText; }
            public Applicant Applicant { get; }
            public int Index { get; }
            public string ResponseText { get; }
            internal int Version { get; }
        }

        readonly Random _random = new Random();
        readonly float _fakeAnswerChance;
        public QuestionController(float fakeAnswerChance = 0.5f)
        {
            if (float.IsNaN(fakeAnswerChance) || fakeAnswerChance < 0f || fakeAnswerChance > 1f)
                throw new ArgumentOutOfRangeException(nameof(fakeAnswerChance));
            _fakeAnswerChance = fakeAnswerChance;
        }
        readonly bool[] _used = new bool[5];
        int _version;
        int _pendingIndex = -1;
        public Applicant Applicant { get; private set; }
        public IReadOnlyList<QuestionDefinition> Definitions { get; private set; }
        public string Response { get; private set; } = "";
        public bool IsAnswering => _pendingIndex >= 0;
        public int UsedQuestionCount
        {
            get
            {
                int count = 0;
                foreach (bool used in _used) if (used) count++;
                return count;
            }
        }
        public event Action Changed;

        public void Bind(Applicant applicant, IReadOnlyList<QuestionDefinition> definitions)
        {
            Cancel();
            Array.Clear(_used, 0, _used.Length);
            Applicant = applicant;
            Definitions = definitions;
            Response = "";
            Changed?.Invoke();
        }

        public bool CanAsk(int index) => Applicant != null && !IsAnswering && index >= 0
            && index < _used.Length && !_used[index] && Definitions != null && index < Definitions.Count
            && Definitions[index] != null && !string.IsNullOrWhiteSpace(Definitions[index].Prompt);

        public AnswerRequest TryBegin(int index)
        {
            if (!CanAsk(index)) return null;
            string responseText = InterviewQuestionCatalog.ScheduleResponse;
            if (index == 0)
            {
                var quality = Applicant.TruePortfolioQuality;
                if (Applicant.FakePortfolioQuality.HasValue && _random.NextDouble() < _fakeAnswerChance)
                    quality = Applicant.FakePortfolioQuality.Value;
                responseText = PortfolioResponseCatalog.Pick(quality, _random);
            }
            else if (index != 1) responseText = string.Empty;
            _used[index] = true; // Pending answers reserve their slot.
            _pendingIndex = index;
            Response = "";
            var request = new AnswerRequest(Applicant, index, ++_version, responseText);
            Changed?.Invoke();
            return request;
        }

        public bool IsCurrent(AnswerRequest request) => request != null && request.Version == _version
            && request.Applicant == Applicant && request.Index == _pendingIndex;

        public bool Complete(AnswerRequest request) => Complete(request, null);

        public bool Complete(AnswerRequest request, string response)
        {
            if (!IsCurrent(request)) return false;
            _pendingIndex = -1;
            Response = request.ResponseText;
            Changed?.Invoke();
            return true;
        }

        public void Cancel()
        {
            ++_version;
            if (_pendingIndex >= 0) _used[_pendingIndex] = false;
            _pendingIndex = -1;
            Changed?.Invoke();
        }
    }
}