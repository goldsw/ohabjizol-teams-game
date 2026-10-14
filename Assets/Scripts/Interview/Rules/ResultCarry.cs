using System;

namespace Ohabjizol.Interview
{
    /// <summary>
    /// One slot for an <see cref="InterviewResult"/>. Taking the result empties the slot.
    /// </summary>
    public static class ResultCarry
    {
        static InterviewResult _result;

        public static void Put(InterviewResult result)
        {
            if (result == null)
                throw new ArgumentNullException(nameof(result));

            _result = result;
        }

        public static InterviewResult Take()
        {
            InterviewResult result = _result;
            _result = null;
            return result;
        }

        internal static void Clear()
        {
            _result = null;
        }
    }
}
