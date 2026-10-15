using System.Collections.Generic;

namespace Ohabjizol.Interview
{
    /// <summary>
    /// Temporary applicants and the single jam theme.
    /// </summary>
    public static class PrototypeCatalog
    {
        // PROTOTYPE ONLY
        // 최종 기획 확정 후 교체 예정

        public const string CorrectSuspicionLineId = "4-portfolio-1";

        public const string SituationA = "2, 3, 1을 합격하면 그래픽 14/15 이고 다음은 표시 18인 4번이다.";

        public const string SituationB = "6번은 기획 7이라, 기획이 0인 팀에 필요하다.";

        public const string SituationC = "4번의 그 한 줄만 올바른 의심이다.";

        public static GameJamTheme Theme { get; } = new GameJamTheme(
            "미연시",
            new SkillSet(planning: 10, programming: 5, graphics: 15),
            JobRole.Graphics);

        public static IReadOnlyList<Applicant> Applicants { get; } = new Applicant[]
        {
            Person(
                "김민수",
                JobRole.Programming,
                ApplicantKind.Normal,
                "1",
                new[] { "프로그래밍 경력 : 3년", "주 사용 언어 : C#" },
                new[] { "게임 로직 구현 샘플", "프로그래밍 숙련 : 12" },
                new[] { "오전 : 시스템 구현", "오후 : 버그 수정" },
                "프로그래밍 중심으로 지원",
                programming: 12, truePortfolioQuality: PortfolioDisplayState.Normal),
            Person(
                "이서연",
                JobRole.Graphics,
                ApplicantKind.Normal,
                "2",
                new[] { "그래픽 경력 : 4년", "주 작업 : 캐릭터 원화" },
                new[] { "미연시 일러스트 샘플", "그래픽 숙련 : 14" },
                new[] { "오전 : 캐릭터 작화", "오후 : 화면 장식" },
                "그래픽 중심으로 지원",
                graphics: 14, truePortfolioQuality: PortfolioDisplayState.Good),
            Person(
                "박준호",
                JobRole.Planning,
                ApplicantKind.Normal,
                "3",
                new[] { "기획 경력 : 3년", "주 작업 : 시나리오" },
                new[] { "선택지 분기 기획서", "기획 숙련 : 11" },
                new[] { "오전 : 대사 구성", "오후 : 선택지 정리" },
                "기획 중심으로 지원",
                planning: 11, truePortfolioQuality: PortfolioDisplayState.Good),
            FraudArtist(),
            Person(
                "정태식",
                JobRole.Programming,
                ApplicantKind.Inappropriate,
                "5",
                new[] { "프로그래밍 숙련 : 15", "비주얼 노벨이 아닌 도구 작업" },
                new[] { "에디터 도구를 제작함", "비주얼 노벨 콘텐츠는 만들지 않음" },
                new[] { "오전 : 도구 기능 구현", "오후 : 도구 동작 확인" },
                "도구 작업으로 지원",
                programming: 15,
                interviewAnswer: "비주얼 노벨 제작은 하지 않음", truePortfolioQuality: PortfolioDisplayState.Normal),
            Person(
                "한소율",
                JobRole.Planning,
                ApplicantKind.Normal,
                "6",
                new[] { "기획 경력 : 1년", "기획 숙련 : 7" },
                new[] { "짧은 시나리오 샘플", "선택지 초안 하나" },
                new[] { "오전 : 설정 정리", "오후 : 흐름 검토" },
                "기획 중심으로 지원",
                planning: 7, truePortfolioQuality: PortfolioDisplayState.Bad)
        };

        static Applicant FraudArtist()
        {
            var displayed = new SkillSet(0, 0, 18);
            var actual = new SkillSet(0, 0, 7);
            return new Applicant(
                "최하은",
                JobRole.Graphics,
                displayed,
                actual,
                new ApplicantDocument[]
                {
                    Doc("4", DocumentKind.Application, "그래픽 숙련 : 18", "주 작업 : 배경 원화"),
                    Doc("4", DocumentKind.Portfolio, "아트 경력 : 4년", "상업 일러스트 다수", CorrectSuspicionLineId),
                    Doc("4", DocumentKind.Schedule, "오전 : 메인 비주얼", "오후 : 표정 작화"),
                    Doc("4", DocumentKind.InterviewRecord, "그래픽 중심으로 지원", SkillAnswerText.Phrase(displayed.Graphics))
                },
                ApplicantKind.Fraud, PortfolioDisplayState.Bad);
        }

        static Applicant Person(
            string name,
            JobRole role,
            ApplicantKind kind,
            string key,
            string[] application,
            string[] portfolio,
            string[] schedule,
            string interviewIntro,
            int planning = 0,
            int programming = 0,
            int graphics = 0,
            string interviewAnswer = null,
            PortfolioDisplayState truePortfolioQuality = PortfolioDisplayState.Normal)
        {
            var skills = new SkillSet(planning, programming, graphics);
            if (interviewAnswer == null)
                interviewAnswer = SkillAnswerText.Phrase(skills.Read(role));
            return new Applicant(
                name,
                role,
                skills,
                skills,
                new ApplicantDocument[]
                {
                    Doc(key, DocumentKind.Application, application[0], application[1]),
                    Doc(key, DocumentKind.Portfolio, portfolio[0], portfolio[1]),
                    Doc(key, DocumentKind.Schedule, schedule[0], schedule[1]),
                    Doc(key, DocumentKind.InterviewRecord, interviewIntro, interviewAnswer)
                },
                kind, truePortfolioQuality);
        }

        static ApplicantDocument Doc(string key, DocumentKind kind, string first, string second, string correctLineId = null)
        {
            string prefix = key + "-" + KindKey(kind);
            string firstId = prefix + "-1";
            string secondId = prefix + "-2";
            return new ApplicantDocument(kind, new DocumentLine[]
            {
                new DocumentLine(firstId, first, firstId == correctLineId),
                new DocumentLine(secondId, second, secondId == correctLineId)
            });
        }

        static string KindKey(DocumentKind kind)
        {
            switch (kind)
            {
                case DocumentKind.Application:
                    return "application";
                case DocumentKind.Portfolio:
                    return "portfolio";
                case DocumentKind.Schedule:
                    return "schedule";
                default:
                    return "interview";
            }
        }
    }
}
