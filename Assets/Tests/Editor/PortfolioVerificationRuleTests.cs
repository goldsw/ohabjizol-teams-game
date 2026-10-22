using System;
using System.Linq;
using NUnit.Framework;

namespace Ohabjizol.Interview
{
    public sealed class PortfolioVerificationRuleTests
    {
        [TestCase(PortfolioDisplayState.Good)]
        [TestCase(PortfolioDisplayState.Normal)]
        [TestCase(PortfolioDisplayState.Bad)]
        public void ZeroChanceNeverCreatesFake(PortfolioDisplayState actual)
        {
            Assert.IsNull(PortfolioQualityRule.CreateFake(actual, new Random(19), 0f));
        }

        [TestCase(PortfolioDisplayState.Good)]
        [TestCase(PortfolioDisplayState.Normal)]
        public void CertainFakeIsAlwaysStrictlyLower(PortfolioDisplayState actual)
        {
            for (int seed = 0; seed < 128; seed++)
            {
                var fake = PortfolioQualityRule.CreateFake(actual, new Random(seed), 1f);
                Assert.IsTrue(fake.HasValue, "seed=" + seed);
                Assert.Less(PortfolioQualityRule.Rank(fake.Value), PortfolioQualityRule.Rank(actual));
            }
        }

        [Test]
        public void BadQualityCannotHaveLowerFake()
        {
            Assert.IsNull(PortfolioQualityRule.CreateFake(PortfolioDisplayState.Bad, new Random(19), 1f));
        }

        [TestCase(float.NaN)]
        [TestCase(-0.01f)]
        [TestCase(1.01f)]
        public void InvalidChanceIsRejected(float chance)
        {
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                PortfolioQualityRule.CreateFake(PortfolioDisplayState.Good, new Random(19), chance));
        }

        [TestCase(PortfolioDisplayState.Good)]
        [TestCase(PortfolioDisplayState.Normal)]
        public void DefaultHalfChanceReachesBothBranchesAndReplays(PortfolioDisplayState actual)
        {
            int absent = 0, present = 0;
            for (int seed = 0; seed < 128; seed++)
            {
                var fake = PortfolioQualityRule.CreateFake(actual, new Random(seed));
                Assert.AreEqual(fake, PortfolioQualityRule.CreateFake(actual, new Random(seed), .5f));
                if (!fake.HasValue) { absent++; continue; }
                present++;
                Assert.Less(PortfolioQualityRule.Rank(fake.Value), PortfolioQualityRule.Rank(actual));
            }
            Assert.Greater(absent, 0);
            Assert.Greater(present, 0);
        }

        [TestCase(ApplicantKind.Normal, false)]
        [TestCase(ApplicantKind.Fraud, false)]
        [TestCase(ApplicantKind.Normal, true)]
        [TestCase(ApplicantKind.Fraud, true)]
        public void CanonicalFlagDependsOnlyOnStoredFake(ApplicantKind kind, bool fake)
        {
            Assert.AreEqual(fake, MakeApplicant(kind, fake).HasFalsePortfolio);
        }

        [TestCase(ApplicantKind.Fraud, false, null, "None")]
        [TestCase(ApplicantKind.Normal, true, null, "Failure")]
        [TestCase(ApplicantKind.Normal, true, "suspicious", "Success")]
        [TestCase(ApplicantKind.Fraud, false, "suspicious", "Failure")]
        [TestCase(ApplicantKind.Normal, true, "ordinary-0", "Failure")]
        [TestCase(ApplicantKind.Normal, false, "missing", "Failure")]
        [TestCase(ApplicantKind.Normal, true, "missing", "Failure")]
        public void VerificationRequiresCanonicalFraudAndCorrectDocumentLine(
            ApplicantKind kind, bool fake, string selectedLine, string expected)
        {
            Assert.AreEqual(expected, VerificationRule.Evaluate(MakeApplicant(kind, fake, true), selectedLine).ToString());
        }

        [TestCase(0f)]
        [TestCase(1f)]
        public void SessionCopiesOnceAndPreservesCanonicalFlagThroughQuestionsAndResult(float chance)
        {
            // Opposite legacy kind makes result and score assertions detect Kind-based mapping.
            var source = MakeApplicant(chance == 0f ? ApplicantKind.Fraud : ApplicantKind.Normal, false);
            var stage = new StageDefinition(StageId.GameProgrammingIntro, "regression",
                new GameJamTheme("regression", new SkillSet(0, 10, 0), JobRole.Programming), new[] { source });
            var session = new InterviewSession(stage, 15, new Random(71), chance);
            var replay = new InterviewSession(stage, 15, new Random(71), chance);
            var stored = session.Current;
            var fake = stored.FakePortfolioQuality;
            Assert.AreNotSame(source, stored);
            Assert.IsFalse(source.HasFalsePortfolio);
            Assert.IsNull(source.FakePortfolioQuality);
            Assert.AreEqual(chance == 1f, stored.HasFalsePortfolio);
            Assert.AreEqual(fake, replay.Current.FakePortfolioQuality);
            Assert.AreEqual(PortfolioDisplayState.Good, stored.TruePortfolioQuality);
            Assert.AreEqual(source.Kind, stored.Kind);
            Assert.AreSame(source.ActualSkills, stored.ActualSkills);

            for (int read = 0; read < 8; read++)
            {
                session.ReadSelectedTeam();
                Assert.AreSame(stored, session.Current);
                Assert.AreEqual(fake, session.Applicants[0].FakePortfolioQuality);
                Assert.AreEqual(Response(stored, 0f, 91), Response(stored, 1f, 91));
            }
            session.Accept();
            Assert.IsTrue(session.IsFinished);
            Assert.IsNotNull(session.Result);
            var member = session.Result.SelectedMembers.Single();
            for (int read = 0; read < 8; read++)
                Assert.AreEqual(stored.HasFalsePortfolio, member.HasFalsePortfolio);
            Assert.AreEqual(chance == 1f ? 1 : 0, session.Result.FraudCount);
            Assert.AreEqual(fake, stored.FakePortfolioQuality);
            Assert.IsFalse(source.HasFalsePortfolio);
        }

        [TestCase(false)]
        [TestCase(true)]
        public void GoodResponseUsesStoredQualityAndCachesAcrossRebind(bool fake)
        {
            var applicant = MakeApplicant(ApplicantKind.Normal, fake);
            var actualResponseApplicant = MakeApplicant(ApplicantKind.Normal, false, false,
                applicant.FakePortfolioQuality ?? applicant.TruePortfolioQuality);
            for (int seed = 0; seed < 16; seed++)
            {
                Assert.AreEqual(Response(actualResponseApplicant, 0f, seed), Response(applicant, 0f, seed));
                Assert.AreEqual(Response(applicant, 0f, seed), Response(applicant, 1f, seed));
            }
        }

        [TestCase(0f)]
        [TestCase(1f)]
        public void StageManagerForwardsFraudProbabilityBeforeSessionCreation(float chance)
        {
            var source = MakeApplicant(ApplicantKind.Normal, false);
            var definition = new StageDefinition(StageId.GameProgrammingIntro, "regression",
                new GameJamTheme("regression", new SkillSet(0, 10, 0), JobRole.Programming), new[] { source });
            var manager = new StageManager(new StageContext("portfolio-regression", definition),
                (runId, result) => { }, 15, new Random(71), chance);
            Assert.AreEqual(chance == 1f, manager.Session.Current.HasFalsePortfolio);
            Assert.IsFalse(source.HasFalsePortfolio);
        }

        private static Applicant MakeApplicant(ApplicantKind kind, bool fake, bool suspicious = false,
            PortfolioDisplayState actual = PortfolioDisplayState.Good)
        {
            var kinds = new[] { DocumentKind.Application, DocumentKind.Portfolio,
                DocumentKind.Schedule, DocumentKind.InterviewRecord };
            var docs = new ApplicantDocument[4];
            for (int i = 0; i < docs.Length; i++)
                docs[i] = new ApplicantDocument(kinds[i], new[] {
                    new DocumentLine(i == 0 ? "suspicious" : "line-" + i, "document", suspicious && i == 0),
                    new DocumentLine("ordinary-" + i, "document", false) });
            return new Applicant("regression", JobRole.Programming, new SkillSet(0, 10, 0),
                new SkillSet(0, 10, 0), docs, kind, actual,
                fake ? (PortfolioDisplayState?)PortfolioDisplayState.Normal : null);
        }

        private static string Response(Applicant applicant, float chance, int seed)
        {
            var definitions = new[] { new QuestionDefinition("portfolio", "portfolio response"),
                new QuestionDefinition("schedule", "schedule response") };
            var controller = new QuestionController(chance, new Random(seed));
            controller.Bind(applicant, definitions);
            var first = controller.TryBegin(0);
            Assert.IsNotNull(first);
            Assert.IsNotEmpty(first.ResponseText);
            Assert.IsTrue(controller.Complete(first));
            Assert.IsFalse(controller.CanAsk(0));
            Assert.IsNull(controller.TryBegin(0), "Answered questions stay disabled");
            controller.Bind(applicant, definitions);
            var cached = controller.TryBegin(0);
            Assert.IsNotNull(cached);
            Assert.AreEqual(first.ResponseText, cached.ResponseText, "Rebinding must preserve cached text");
            return first.ResponseText;
        }
    }
}
