using UnityEngine;

namespace Ohabjizol.Interview
{
    /// <summary>
    /// Takes the carried result and asks <see cref="ResultBoard"/> to draw it.
    /// </summary>
    public sealed class ResultSceneEntry : MonoBehaviour
    {
        void Start()
        {
            InterviewResult result = ResultCarry.Take();
            if (result == null)
                return;

            ResultBoard board = GetComponent<ResultBoard>();
            if (board == null)
                return;

            board.Show(result);
        }
    }
}
