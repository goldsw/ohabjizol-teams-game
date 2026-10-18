namespace Ohabjizol.Interview
{
    public static class EndingSceneRoute
    {
        public static string For(EndingKind kind)
        {
            switch (kind)
            {
                case EndingKind.Good: return "GoodEndingScene";
                case EndingKind.Normal: return "NormalEndingScene";
                case EndingKind.Bad: return "BadEndingScene";
                default: throw new System.ArgumentOutOfRangeException(nameof(kind), kind, "Unknown ending kind.");
            }
        }
    }
}