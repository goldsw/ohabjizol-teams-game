namespace Ohabjizol.Interview
{
    /// <summary>
    /// Interview-record wording for one skill number.
    /// </summary>
    public static class SkillAnswerText
    {
        // PROTOTYPE ONLY
        // 최종 기획 확정 후 교체 예정
        public static string Phrase(int skill)
        {
            if (skill <= 0)
                return "답변을 하지 못함";
            if (skill <= 6)
                return "말을 많이 절었음";
            if (skill <= 12)
                return "적절하게 서술함";
            return "훌륭함";
        }
    }
}
