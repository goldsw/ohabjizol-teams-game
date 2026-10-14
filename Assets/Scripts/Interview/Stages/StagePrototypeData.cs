using System.Collections.Generic;

namespace Ohabjizol.Interview
{
    /// <summary>
    /// Stand-in themes and applicants for stages after the entrance jam.
    /// </summary>
    static class StagePrototypeData
    {
        // PROTOTYPE ONLY
        // 최종 기획 확정 후 교체 예정

        public static StageDefinition GameProgrammingIntro()
        {
            const string stage = "s2";
            const string theme = "플랫포머";
            return Define(
                StageId.GameProgrammingIntro,
                "겜프입 프로젝트",
                theme,
                new SkillSet(planning: 16, programming: 12, graphics: 20),
                JobRole.Programming,
                new Applicant[]
                {
                    Normal(stage, 1, "서민재", JobRole.Planning, theme, planning: 9),
                    Normal(stage, 2, "오하늘", JobRole.Planning, theme, planning: 8),
                    Normal(stage, 3, "남기준", JobRole.Programming, theme, programming: 12),
                    Normal(stage, 4, "배도윤", JobRole.Graphics, theme, graphics: 18),
                    Normal(stage, 5, "신유나", JobRole.Graphics, theme, graphics: 6),
                    Fraud(stage, 6, "고은별", JobRole.Graphics, theme, new SkillSet(0, 0, 22)),
                    OffTheme(stage, 7, "문재혁", JobRole.Programming, theme, new SkillSet(0, 28, 0), "플랫포머 제작은 하지 않음")
                });
        }

        public static StageDefinition SecondYear()
        {
            const string stage = "s3";
            const string theme = "협동 퍼즐";
            return Define(
                StageId.SecondYear,
                "2학년 프로젝트",
                theme,
                new SkillSet(planning: 22, programming: 18, graphics: 26),
                JobRole.Planning,
                new Applicant[]
                {
                    Normal(stage, 1, "강이안", JobRole.Planning, theme, planning: 12),
                    Normal(stage, 2, "노서현", JobRole.Planning, theme, planning: 10),
                    Normal(stage, 3, "류하준", JobRole.Programming, theme, programming: 18),
                    Normal(stage, 4, "백시우", JobRole.Graphics, theme, graphics: 24),
                    Normal(stage, 5, "설아린", JobRole.Graphics, theme, graphics: 8),
                    Fraud(stage, 6, "임도하", JobRole.Programming, theme, new SkillSet(0, 20, 0)),
                    Fraud(stage, 7, "조예린", JobRole.Graphics, theme, new SkillSet(0, 0, 28)),
                    OffTheme(stage, 8, "한가람", JobRole.Planning, theme, new SkillSet(30, 0, 0), "협동 퍼즐이 아닌 도구 작업")
                });
        }

        public static StageDefinition ThirdYear()
        {
            const string stage = "s4";
            const string theme = "도시 액션";
            return Define(
                StageId.ThirdYear,
                "3학년 프로젝트",
                theme,
                new SkillSet(planning: 28, programming: 24, graphics: 32),
                JobRole.Graphics,
                new Applicant[]
                {
                    Normal(stage, 1, "표지후", JobRole.Planning, theme, planning: 15),
                    Normal(stage, 2, "구하린", JobRole.Planning, theme, planning: 14),
                    Normal(stage, 3, "마준서", JobRole.Programming, theme, programming: 24),
                    Normal(stage, 4, "도은결", JobRole.Graphics, theme, graphics: 30),
                    Normal(stage, 5, "석미래", JobRole.Programming, theme, programming: 10),
                    Fraud(stage, 6, "나율", JobRole.Planning, theme, new SkillSet(16, 0, 0)),
                    Fraud(stage, 7, "위시온", JobRole.Graphics, theme, new SkillSet(0, 0, 34)),
                    OffTheme(stage, 8, "차민", JobRole.Graphics, theme, new SkillSet(0, 0, 40), "도시가 아닌 정물만 그림")
                });
        }

        static StageDefinition Define(
            StageId id,
            string displayName,
            string themeName,
            SkillSet required,
            JobRole coreRole,
            IReadOnlyList<Applicant> applicants)
        {
            return new StageDefinition(id, displayName, new GameJamTheme(themeName, required, coreRole), applicants);
        }

        static Applicant Normal(
            string stage,
            int number,
            string name,
            JobRole role,
            string theme,
            int planning = 0,
            int programming = 0,
            int graphics = 0)
        {
            var skills = new SkillSet(planning, programming, graphics);
            return Person(stage, number, name, role, ApplicantKind.Normal, skills, skills, theme, null, false);
        }

        static Applicant Fraud(string stage, int number, string name, JobRole role, string theme, SkillSet displayed)
        {
            return Person(stage, number, name, role, ApplicantKind.Fraud, displayed, SkillSet.Zero, theme, null, true);
        }

        static Applicant OffTheme(
            string stage,
            int number,
            string name,
            JobRole role,
            string theme,
            SkillSet skills,
            string interviewAnswer)
        {
            return Person(
                stage,
                number,
                name,
                role,
                ApplicantKind.Inappropriate,
                skills,
                skills,
                theme,
                interviewAnswer,
                false);
        }

        static Applicant Person(
            string stage,
            int number,
            string name,
            JobRole role,
            ApplicantKind kind,
            SkillSet displayed,
            SkillSet actual,
            string theme,
            string interviewAnswer,
            bool portfolioSuspicion)
        {
            string roleName = JobRoleText.Korean(role);
            int shown = displayed.Read(role);
            if (interviewAnswer == null)
                interviewAnswer = SkillAnswerText.Phrase(shown);
            return new Applicant(
                name,
                role,
                displayed,
                actual,
                new ApplicantDocument[]
                {
                    Doc(stage, number, DocumentKind.Application, roleName + " 경력 : " + (1 + (number % 4)) + "년", roleName + " 숙련 : " + shown, false),
                    Doc(stage, number, DocumentKind.Portfolio, roleName + " 숙련 : " + shown, theme + " 작업 샘플", portfolioSuspicion),
                    Doc(stage, number, DocumentKind.Schedule, "오전 : " + theme + " 작업", "오후 : 결과 정리", false),
                    Doc(stage, number, DocumentKind.InterviewRecord, roleName + " 중심으로 지원", interviewAnswer, false)
                },
                kind);
        }

        static ApplicantDocument Doc(
            string stage,
            int number,
            DocumentKind kind,
            string first,
            string second,
            bool firstIsSuspicion)
        {
            string prefix = stage + "-" + number + "-" + KindKey(kind);
            return new ApplicantDocument(kind, new DocumentLine[]
            {
                new DocumentLine(prefix + "-1", first, firstIsSuspicion),
                new DocumentLine(prefix + "-2", second, false)
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
