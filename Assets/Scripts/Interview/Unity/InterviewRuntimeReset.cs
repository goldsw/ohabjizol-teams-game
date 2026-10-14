using UnityEngine;

namespace Ohabjizol.Interview
{
    /// <summary>
    /// Drops play-mode slots when the subsystem reloads, including when domain reload is off.
    /// </summary>
    static class InterviewRuntimeReset
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetSlots()
        {
            StageSelection.Clear();
            ResultCarry.Clear();
        }
    }
}
