using UnityEngine;
namespace Ohabjizol.Interview
{
    public sealed class ResultSceneEntry : MonoBehaviour
    {
        void Start()
        {
            InterviewResult result = ResultCarry.Take();
            ResultBoard board = GetComponent<ResultBoard>();
            if (result == null)
            {
                if (board != null) board.ShowPending();
                return;
            }
            if (board != null) board.Show(result);
        }
    }
}
