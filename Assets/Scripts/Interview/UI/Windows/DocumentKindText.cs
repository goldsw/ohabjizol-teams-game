namespace Ohabjizol.Interview
{
    /// <summary>
    /// Korean labels and colors for <see cref="DocumentKind"/>. Values match the interview screen.
    /// </summary>
    public static class DocumentKindText
    {
        static readonly UnityEngine.Color ApplicationColor = new UnityEngine.Color(0.22f, 0.40f, 0.78f, 1f);
        static readonly UnityEngine.Color PortfolioColor = new UnityEngine.Color(0.82f, 0.45f, 0.14f, 1f);
        static readonly UnityEngine.Color ScheduleColor = new UnityEngine.Color(0.12f, 0.55f, 0.50f, 1f);
        static readonly UnityEngine.Color InterviewRecordColor = new UnityEngine.Color(0.48f, 0.30f, 0.68f, 1f);
        static readonly UnityEngine.Color UnknownColor = new UnityEngine.Color(0.5f, 0.5f, 0.5f, 1f);

        public static string Korean(DocumentKind kind)
        {
            switch (kind)
            {
                case DocumentKind.Application:
                    return "지원서";
                case DocumentKind.Portfolio:
                    return "포트폴리오";
                case DocumentKind.Schedule:
                    return "일정표";
                case DocumentKind.InterviewRecord:
                    return "면접 기록";
                default:
                    return string.Empty;
            }
        }

        public static UnityEngine.Color Color(DocumentKind kind)
        {
            switch (kind)
            {
                case DocumentKind.Application:
                    return ApplicationColor;
                case DocumentKind.Portfolio:
                    return PortfolioColor;
                case DocumentKind.Schedule:
                    return ScheduleColor;
                case DocumentKind.InterviewRecord:
                    return InterviewRecordColor;
                default:
                    return UnknownColor;
            }
        }
    }
}
