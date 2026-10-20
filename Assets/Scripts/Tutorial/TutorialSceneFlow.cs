using System;
using UnityEngine.SceneManagement;
using UnityEngine;

namespace Ohabjizol.Tutorial
{
    public static class TutorialSceneFlow
    {
        static bool _returning;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetSession() { _returning = false; }
        public static void Start(Action<string> launch)
        {
            if (launch == null) throw new ArgumentNullException(nameof(launch));
            launch("TutorialScene");
        }
        public static void ReturnMain()
        {
            _returning = true;
            SceneManager.LoadScene("MainMenuScene", LoadSceneMode.Single);
        }
        public static bool ConsumeReturn()
        {
            bool value = _returning;
            _returning = false;
            return value;
        }
    }
}
