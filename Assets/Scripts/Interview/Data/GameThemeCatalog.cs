using System;
using System.Collections.Generic;

namespace Ohabjizol.Interview
{
    /// <summary>
    /// Run themes. Required skills are the normal-ending minimums for that theme.
    /// A run keeps one theme: the choice is derived from its run id, so reloads agree without a save field.
    /// </summary>
    public static class GameThemeCatalog
    {
        public static IReadOnlyList<GameJamTheme> All { get; } = Array.AsReadOnly(new[]
        {
            new GameJamTheme("미연시", new SkillSet(20, 5, 45), JobRole.Graphics),
            new GameJamTheme("퍼즐 게임", new SkillSet(35, 20, 15), JobRole.Planning),
            new GameJamTheme("액션 게임", new SkillSet(15, 45, 10), JobRole.Programming)
        });

        public static GameJamTheme ForRun(string runId)
        {
            if (string.IsNullOrEmpty(runId))
                throw new ArgumentException("A run id is required.", nameof(runId));

            // FNV-1a; string.GetHashCode is not stable across runtimes.
            uint hash = 2166136261;
            foreach (char c in runId)
                hash = (hash ^ c) * 16777619;
            return All[(int)(hash % (uint)All.Count)];
        }

        public static StageDefinition WithRunTheme(StageDefinition stage, string runId)
        {
            if (stage == null)
                throw new ArgumentNullException(nameof(stage));
            return new StageDefinition(stage.Id, stage.DisplayName, ForRun(runId), stage.Applicants);
        }
    }
}
