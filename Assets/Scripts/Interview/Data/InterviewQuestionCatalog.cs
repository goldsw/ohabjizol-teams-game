using System;
using System.Collections.Generic;
namespace Ohabjizol.Interview
{
    public static class InterviewQuestionCatalog
    {
        public const string ScheduleResponse = "프로젝트 일정표를 확인해 주세요.";
        static readonly IReadOnlyList<QuestionDefinition>[] Questions = { Define("planning"), Define("programming"), Define("graphics") };
        public static IReadOnlyList<QuestionDefinition> GetQuestions(JobRole role)
        {
            switch (role) {
                case JobRole.Planning: return Questions[0];
                case JobRole.Programming: return Questions[1];
                case JobRole.Graphics: return Questions[2];
                default: throw new ArgumentOutOfRangeException(nameof(role));
            }
        }
        public static string GetResponse(Applicant applicant, int questionIndex) => GetResponse(applicant, questionIndex, 3, 5);
        public static string GetResponse(Applicant applicant, int questionIndex, int normalMin, int goodMin)
        {
            if (applicant == null) throw new ArgumentNullException(nameof(applicant));
            if (questionIndex < 0 || questionIndex >= 5) throw new ArgumentOutOfRangeException(nameof(questionIndex));
            if (questionIndex == 0) return PortfolioResponseCatalog.Pick(applicant.TruePortfolioQuality, new Random());
            return questionIndex == 1 ? ScheduleResponse : string.Empty;
        }
        static IReadOnlyList<QuestionDefinition> Define(string prefix)
        {
            return Array.AsReadOnly(new[] {
                new QuestionDefinition(prefix + "-1", "포트폴리오 프로젝트"),
                new QuestionDefinition(prefix + "-2", "일정"),
                new QuestionDefinition(prefix + "-3", ""),
                new QuestionDefinition(prefix + "-4", ""),
                new QuestionDefinition(prefix + "-5", "")
            });
        }
    }
}