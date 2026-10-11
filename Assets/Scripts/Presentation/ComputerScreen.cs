using UnityEngine;
using UnityEngine.UI;

namespace Ohabjizol.Presentation
{
    /// <summary>
    /// Marks the swappable monitor. The mesh can change; transition code uses this transform.
    /// Forward should point out of the display, toward the player.
    /// World-space screen UI is parented here so it sits on the quad.
    /// </summary>
    public sealed class ComputerScreen : MonoBehaviour
    {
        [SerializeField] Transform viewRoot;
        [SerializeField] bool applyDefaultScreenColor = true;
        [SerializeField] Color screenColor = new Color(0.02f, 0.09f, 0.07f, 1f);
        [SerializeField] Vector2 worldCanvasSize = new Vector2(1600f, 900f);

        public Transform ViewRoot
        {
            get { return viewRoot != null ? viewRoot : transform.root; }
        }

        public void PlaceWorldCanvas(RectTransform root)
        {
            PlaceWorldCanvas(root, null);
        }

        public void PlaceWorldCanvas(RectTransform root, Camera worldCamera)
        {
            if (root == null)
                return;

            FitCanvas(root, worldCamera);
        }

        void OnEnable()
        {
            if (!Application.isPlaying)
                return;

            Canvas.preWillRenderCanvases += FitChildCanvases;
        }

        void OnDisable()
        {
            Canvas.preWillRenderCanvases -= FitChildCanvases;
        }

        void FitChildCanvases()
        {
            var canvases = GetComponentsInChildren<Canvas>(true);
            for (int i = 0; i < canvases.Length; i++)
            {
                var canvas = canvases[i];
                if (canvas.transform.parent != transform || !canvas.isRootCanvas)
                    continue;

                FitCanvas(canvas.transform as RectTransform, canvas.worldCamera);
            }
        }

        void FitCanvas(RectTransform root, Camera worldCamera)
        {
            if (root == null)
                return;

            // Cover the unit quad in this transform's local space. The monitor's
            // local scale sets the world size; this canvas stays on that quad and
            // does not spill onto the backdrop. Do not size from Screen or the game
            // view: the editor view and the build framebuffer are different resolutions.
            root.SetParent(transform, false);
            root.localRotation = Quaternion.Euler(0f, 180f, 0f);
            root.localPosition = Vector3.zero;
            float width = Mathf.Max(1f, worldCanvasSize.x);
            float height = Mathf.Max(1f, worldCanvasSize.y);
            root.anchorMin = new Vector2(0.5f, 0.5f);
            root.anchorMax = new Vector2(0.5f, 0.5f);
            root.pivot = new Vector2(0.5f, 0.5f);
            root.sizeDelta = new Vector2(width, height);
            root.localScale = new Vector3(1f / width, 1f / height, 1f / width);

            var canvas = root.GetComponent<Canvas>();
            if (canvas == null)
                return;

            canvas.renderMode = RenderMode.WorldSpace;
            canvas.pixelPerfect = false;
            // Enabled (0) resizes the rect to the render target during Camera.Render.
            // That runs in the player and leaves a small panel on the quad. The editor
            // game view does not take that path.
            canvas.updateRectTransformForStandalone = StandaloneRenderResize.Disabled;
            if (worldCamera != null)
                canvas.worldCamera = worldCamera;
            else if (canvas.worldCamera == null)
                canvas.worldCamera = Camera.main;

            var scaler = root.GetComponent<CanvasScaler>();
            if (scaler != null)
                Destroy(scaler);
        }

        void Awake()
        {
            var canvas = GetComponentInChildren<Canvas>(true);
            if (canvas != null && canvas.renderMode == RenderMode.WorldSpace && canvas.worldCamera == null)
                canvas.worldCamera = Camera.main;

            if (!applyDefaultScreenColor)
                return;

            var target = GetComponent<Renderer>();
            if (target == null)
                return;

            // Shader.Find is stripped in a build unless a material already references it.
            // Falling through kept the default Lit material, which reads as a beige quad
            // around the dark UI. Prefer the shader already on the renderer.
            var source = target.sharedMaterial;
            var shader = source != null ? source.shader : null;
            if (shader == null || shader.name.IndexOf("Unlit", System.StringComparison.Ordinal) < 0)
                shader = Shader.Find("Universal Render Pipeline/Unlit");
            if (shader == null)
                shader = Shader.Find("Sprites/Default");
            if (shader == null)
                shader = source != null ? source.shader : null;
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
