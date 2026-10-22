using UnityEngine;
using UnityEngine.SceneManagement;

namespace Ohabjizol.Interview
{
    /// <summary>
    /// Opens <see cref="InterviewScreen.HandoffSceneName"/> once the screen holds a result.
    /// </summary>
    public sealed class InterviewExit : MonoBehaviour
    {
        InterviewScreen _screen;
        bool _opened;

        void Awake()
        {
            _screen = GetComponent<InterviewScreen>();
        }

        void Update()
        {
            if (_opened || _screen == null)
                return;

            InterviewResult result = _screen.Result;
            if (result == null)
                return;

            string sceneName = ResultSceneRoute.For(result);
            if (string.IsNullOrEmpty(sceneName))
                return;

            sceneName = ProductionProgressView.RouteBeforeResult(sceneName, result);
            _opened = true;
            ResultCarry.Put(result);
            SceneManager.LoadScene(sceneName);
        }
    }
}
