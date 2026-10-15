#if UNITY_EDITOR
using Ohabjizol.Interview;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Ohabjizol.EditorTools
{
    public static class ScopedResultScheduleBake
    {
        // Operates on loaded scenes only. Caller owns scene selection and saving.
        [MenuItem("Tools/Interview/Bake Result Schedule (Loaded Scenes)")]
        public static void BakeLoadedScenes()
        {
            int count = 0;
            foreach (var board in Object.FindObjectsByType<ResultBoard>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (!board.gameObject.scene.IsValid() || !board.gameObject.scene.isLoaded) continue;
                board.BakeResultScheduleUi();
                EditorSceneManager.MarkSceneDirty(board.gameObject.scene);
                count++;
            }
            Debug.Log("Result schedule UI bake: " + count + " board(s). Save loaded scenes explicitly.");
        }
    }
}
#endif