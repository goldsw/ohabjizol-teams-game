using System;

namespace Ohabjizol.Interview
{
    public static class ResultGradeRule
    {
        // PROTOTYPE ONLY
        // 최종 기획 확정 후 교체 예정

        // PROTOTYPE RULE
        // Final verification scoring is not yet defined.
        public static ResultGrade Evaluate(SkillSet actual, SkillSet required, JobRole coreRole, int fraudCount)
        {
            if (actual == null)
                throw new ArgumentNullException(nameof(actual));
            if (required == null)
                throw new ArgumentNullException(nameof(required));

            if (actual.Read(coreRole) == 0)
                return ResultGrade.Fail;

            if (MeetsAll(actual, required) && fraudCount <= 0)
                return ResultGrade.S;

            if (MeetsAll(actual, required))
                return ResultGrade.A;

            if (actual.Read(coreRole) < required.Read(coreRole))
                return ResultGrade.C;

            return ResultGrade.B;
        }

        static bool MeetsAll(SkillSet actual, SkillSet required)
        {
            return Meets(actual.Planning, required.Planning)
                && Meets(actual.Programming, required.Programming)
                && Meets(actual.Graphics, required.Graphics);
        }

        static bool Meets(int actual, int required)
        {
            return required <= 0 || actual >= required;
        }
    }
}
