using UnityEngine;

namespace Ohabjizol.Interview
{
    /// <summary>
    /// Empty shell. A blank name, or required skills that are all zero, resolves to <see cref="PrototypeCatalog.Theme"/>.
    /// </summary>
    [CreateAssetMenu(fileName = "Theme", menuName = "Ohabjizol/Theme")]
    public sealed class ThemeAsset : ScriptableObject
    {
        [SerializeField] string displayName = "";
        [SerializeField] int requiredPlanning = 0;
        [SerializeField] int requiredProgramming = 0;
        [SerializeField] int requiredGraphics = 0;
        [SerializeField] JobRole coreRole = JobRole.Graphics;

        public bool IsEmpty
        {
            get
            {
                return string.IsNullOrEmpty(displayName)
                    || (requiredPlanning == 0 && requiredProgramming == 0 && requiredGraphics == 0);
            }
        }

        public GameJamTheme Resolve()
        {
            if (IsEmpty)
                return PrototypeCatalog.Theme;

            return new GameJamTheme(
                displayName,
                new SkillSet(requiredPlanning, requiredProgramming, requiredGraphics),
                coreRole);
        }
    }
}
