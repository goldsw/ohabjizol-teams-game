namespace Ohabjizol.Interview
{
    /// <summary>
    /// Korean labels for <see cref="JobRole"/>. Enum values remain compatible with saved data.
    /// </summary>
    public static class JobRoleText
    {
        public static string Korean(JobRole role)
        {
            switch (role)
            {
                case JobRole.Planning:
                    return "기획";
                case JobRole.Programming:
                    return "프로그래머";
                case JobRole.Graphics:
                    return "아트";
                default:
                    return string.Empty;
            }
        }
    }
}
