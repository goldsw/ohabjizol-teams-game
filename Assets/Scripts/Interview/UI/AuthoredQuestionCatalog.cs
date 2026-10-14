using System;
using System.Collections.Generic;
using UnityEngine;

namespace Ohabjizol.Interview
{
    [CreateAssetMenu(menuName = "Interview/Authored Question Catalog")]
    public sealed class AuthoredQuestionCatalog : ScriptableObject
    {
        [Serializable]
        public sealed class Entry
        {
            public JobRole Role;
            public string Id;
            [TextArea(2, 4)] public string Prompt;
            [TextArea(2, 5)] public string GoodResponse;
            [TextArea(2, 5)] public string NormalResponse;
            [TextArea(2, 5)] public string BadResponse;
        }

        [SerializeField] Entry[] _entries = Array.Empty<Entry>();
        [SerializeField, Min(0)] int normalMinimum = 3;
        [SerializeField, Min(0)] int goodMinimum = 5;
        public IReadOnlyList<Entry> Entries => Array.AsReadOnly(_entries);
        public int QuestionCount => _entries.Length;
        public int ResponseCount => _entries.Length * 3;

        // Called by the parent baker before asset creation. Existing authored values are preserved.
        public void SeedDefaults()
        {
            if (_entries != null && _entries.Length != 0) return;
            JobRole[] roles = { JobRole.Planning, JobRole.Programming, JobRole.Graphics };
            var entries = new List<Entry>(15);
            foreach (JobRole role in roles)
            {
                var definitions = InterviewQuestionCatalog.GetQuestions(role);
                Applicant bad = DefaultApplicant(role, 0);
                Applicant normal = DefaultApplicant(role, 3);
                Applicant good = DefaultApplicant(role, 5);
                for (int i = 0; i < definitions.Count; i++)
                {
                    entries.Add(new Entry
                    {
                        Role = role,
                        Id = definitions[i].Id,
                        Prompt = definitions[i].Prompt,
                        GoodResponse = InterviewQuestionCatalog.GetResponse(good, i),
                        NormalResponse = InterviewQuestionCatalog.GetResponse(normal, i),
                        BadResponse = InterviewQuestionCatalog.GetResponse(bad, i)
                    });
                }
            }
            _entries = entries.ToArray();
        }

        public IReadOnlyList<QuestionDefinition> GetQuestions(JobRole role)
        {
            var questions = new List<QuestionDefinition>(5);
            foreach (Entry entry in _entries)
                if (entry != null && entry.Role == role)
                    questions.Add(new QuestionDefinition(entry.Id, entry.Prompt));
            if (questions.Count != 5)
                throw new InvalidOperationException("AuthoredQuestionCatalog requires five questions for " + role + ".");
            return questions.AsReadOnly();
        }

        public string GetResponse(Applicant applicant, int questionIndex)
        {
            if (applicant == null) throw new ArgumentNullException(nameof(applicant));
            if (questionIndex < 0 || questionIndex >= 5) throw new ArgumentOutOfRangeException(nameof(questionIndex));
            if (normalMinimum < 0 || goodMinimum < normalMinimum)
                throw new InvalidOperationException("AuthoredQuestionCatalog thresholds require 0 <= normalMinimum <= goodMinimum.");
            int index = 0;
            foreach (Entry entry in _entries)
            {
                if (entry == null || entry.Role != applicant.PrimaryRole) continue;
                if (index++ != questionIndex) continue;
                int skill = applicant.ActualSkills.Read(applicant.PrimaryRole);
                return skill >= goodMinimum ? entry.GoodResponse
                    : skill >= normalMinimum ? entry.NormalResponse : entry.BadResponse;
            }
            throw new InvalidOperationException("AuthoredQuestionCatalog response entry missing.");
        }

        static Applicant DefaultApplicant(JobRole role, int skill)
        {
            return new Applicant("Default response seed", role, SkillSet.Zero,
                new SkillSet(skill, skill, skill), new ApplicantDocument[4], default(ApplicantKind));
        }
    }
}