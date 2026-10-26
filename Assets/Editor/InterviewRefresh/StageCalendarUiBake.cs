#if UNITY_EDITOR
using System;
using System.IO;
using Ohabjizol.Interview;
using Ohabjizol.UI;
using UnityEditor;
using UnityEngine;
namespace Ohabjizol.EditorTools
{
    public static class StageCalendarUiBake
    {
        public const string WindowPath = "Assets/Prefabs/Interview/StageCalendarWindow.prefab";
        public const string ConfigPath = "Assets/Resources/Interview/StageScheduleConfig.asset";
        [MenuItem("Tools/Interview/Build Stage Calendar Assets")]
        public static void BuildAssets()
        {
            if (Application.isPlaying) throw new InvalidOperationException("Calendar baking requires EditMode.");
            Directory.CreateDirectory(Path.GetDirectoryName(WindowPath));
            Directory.CreateDirectory(Path.GetDirectoryName(ConfigPath));
            AssetDatabase.Refresh();
            if (AssetDatabase.LoadAssetAtPath<StageScheduleConfig>(ConfigPath) == null)
                AssetDatabase.CreateAsset(ScriptableObject.CreateInstance<StageScheduleConfig>(), ConfigPath);
            var host = new GameObject("CalendarBake", typeof(RectTransform));
            try {
                var window = WindowFactory.Create<StageCalendarWindow>(host.GetComponent<RectTransform>(),
                    "프로젝트 일정표", new Vector2(460, 470), Vector2.zero);
                window.CaptureEditorReferences();
                PrefabUtility.SaveAsPrefabAsset(window.gameObject, WindowPath);
                AssetDatabase.SaveAssets();
            }
            finally { UnityEngine.Object.DestroyImmediate(host); }
        }
    }
}
#endif
