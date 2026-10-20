using UnityEngine;

namespace Ohabjizol.Tutorial
{
    public static class TutorialProgress
    {
        const string Key = "ohabjizol.tutorial.seen.v1";
        public static bool Seen => PlayerPrefs.GetInt(Key, 0) == 1;
        public static void MarkSeen()
        {
            PlayerPrefs.SetInt(Key, 1);
            PlayerPrefs.Save();
        }
    }
}
