namespace Ohabjizol.Interview
{
    /// <summary>
    /// Korean labels for <see cref="JobRole"/>. Art is not a role name.
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
                    return "프로그래밍";
                case JobRole.Graphics:
                    return "그래픽";
                default:
                    return string.Empty;
            }
        }
    }
}
