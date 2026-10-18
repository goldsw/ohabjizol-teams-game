namespace Ohabjizol.Interview
{
    public static class ResultSceneRoute
    {
        public const string FallbackSceneName = "ResultScene";
        public const string GoodSceneName = "GoodResultScene";
        public const string NormalSceneName = "NormalResultScene";
        public const string BadSceneName = "BadResultScene";

        public static bool IsResultScene(string sceneName)
        {
            return sceneName == FallbackSceneName || sceneName == GoodSceneName
                || sceneName == NormalSceneName || sceneName == BadSceneName;
        }

        public static string For(InterviewResult result)
        {
            if (result == null) return FallbackSceneName;
            if (result.HalfStarUnits >= 9) return GoodSceneName;
            if (result.HalfStarUnits >= 7) return NormalSceneName;
            return BadSceneName;
        }
    }
}
