using System;
using UnityEngine;
namespace Ohabjizol.Interview
{
    [Serializable]
    public sealed class StatRange
    {
        public int min, max;
        public string raw, cell;
        public bool blank;
        public int Sample(System.Random random)
        {
            if (blank || min < 0 || max < min || max > 10)
                throw new InvalidOperationException("Missing or invalid range at " + cell);
            return random.Next(min, max + 1);
        }
    }
    [Serializable]
    public sealed class SkillRanges
    {
        public StatRange planning, programming, graphics;
        public SkillSet Sample(System.Random random) => new SkillSet(planning.Sample(random), programming.Sample(random), graphics.Sample(random));
        public SkillSet SampleOptional(System.Random random, SkillSet fallback) => new SkillSet(
            Optional(planning, random, fallback.Planning), Optional(programming, random, fallback.Programming), Optional(graphics, random, fallback.Graphics));
        static int Optional(StatRange range, System.Random random, int fallback) => range == null || range.blank ? fallback : range.Sample(random);
        public bool HasValues => (planning != null && !planning.blank) || (programming != null && !programming.blank) || (graphics != null && !graphics.blank);
    }
    [Serializable]
    public sealed class ApplicantRoleProfile
    {
        public JobRole role;
        public string roleCell, fakeRoleMetadata, observedSourceCellsJson;
        public SkillRanges baseStatRanges, fakeStatRanges;
    }
    [CreateAssetMenu(fileName = "ApplicantProfile", menuName = "Ohabjizol/Applicant Profile")]
    public sealed class ApplicantProfileAsset : ScriptableObject
    {
        public string characterId, displayName, sourceSheet;
        public int sourceRow;
        public ApplicantRoleProfile[] roleProfiles;
        [Tooltip("Temporary authored quality; not derived from source stats.")]
        public PortfolioDisplayState truePortfolioQuality = PortfolioDisplayState.Normal;
        public Applicant Sample(System.Random random)
        {
            if (roleProfiles == null || roleProfiles.Length != 3) throw new InvalidOperationException("Three role variants required: " + name);
            var profile = roleProfiles[random.Next(roleProfiles.Length)];
            var expected = profile.baseStatRanges.Sample(random);
            var fake = profile.fakeStatRanges != null && profile.fakeStatRanges.HasValues ? profile.fakeStatRanges.SampleOptional(random, expected) : null;
            string roleName = JobRoleText.Korean(profile.role);
            var documents = new ApplicantDocument[4];
            var kinds = new[] { DocumentKind.Application, DocumentKind.Portfolio, DocumentKind.Schedule, DocumentKind.InterviewRecord };
            for (int i = 0; i < kinds.Length; i++)
                documents[i] = new ApplicantDocument(kinds[i], new[] { new DocumentLine(characterId + "-" + i + "-1", displayName + " / " + roleName, false), new DocumentLine(characterId + "-" + i + "-2", "임시 지원 문서", false) });
            return new Applicant(displayName, profile.role, expected, fake ?? expected, documents, ApplicantKind.Normal,
                truePortfolioQuality, fakeSkills: fake, fakeRoleMetadata: profile.fakeRoleMetadata, characterId: characterId);
        }
    }
}