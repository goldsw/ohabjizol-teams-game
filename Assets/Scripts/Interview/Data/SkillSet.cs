using System;

namespace Ohabjizol.Interview
{
    /// <summary>
    /// Planning, programming, then graphics. Unlisted skills stay 0. Add and read only.
    /// </summary>
    public sealed class SkillSet
    {
        public static SkillSet Zero { get; } = new SkillSet(0, 0, 0);

        public SkillSet(int planning, int programming, int graphics)
        {
            Planning = planning;
            Programming = programming;
            Graphics = graphics;
        }

        public int Planning { get; }

        public int Programming { get; }

        public int Graphics { get; }

        public int Read(JobRole role)
        {
            switch (role)
            {
                case JobRole.Planning:
                    return Planning;
                case JobRole.Programming:
                    return Programming;
                case JobRole.Graphics:
                    return Graphics;
                default:
                    return 0;
            }
        }

        public SkillSet Add(SkillSet other)
        {
            if (other == null)
                throw new ArgumentNullException(nameof(other));

            return new SkillSet(
                Planning + other.Planning,
                Programming + other.Programming,
                Graphics + other.Graphics);
        }
    }
}
