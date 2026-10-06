using UnityEngine;

namespace Ohabjizol.Presentation
{
    /// <summary>
    /// Marks the swappable monitor. The mesh can change; transition code uses this transform.
    /// Forward should point out of the display, toward the player.
    /// </summary>
    public sealed class ComputerScreen : MonoBehaviour
    {
        [SerializeField] Transform viewRoot;
        [SerializeField] bool applyDefaultScreenColor = true;
        [SerializeField] Color screenColor = new Color(0.02f, 0.09f, 0.07f, 1f);

        public Transform ViewRoot
        {
            get { return viewRoot != null ? viewRoot : transform.root; }
        }

        void Awake()
        {
            if (!applyDefaultScreenColor)
                return;

            var target = GetComponent<Renderer>();
            if (target == null)
                return;

            var shader = Shader.Find("Universal Render Pipeline/Unlit");
            if (shader == null)
                shader = Shader.Find("Sprites/Default");
            if (shader == null)
                return;

            var material = new Material(shader);
            if (material.HasProperty("_BaseColor"))
                material.SetColor("_BaseColor", screenColor);
            if (material.HasProperty("_Color"))
                material.SetColor("_Color", screenColor);
            target.sharedMaterial = material;
        }
    }
}
