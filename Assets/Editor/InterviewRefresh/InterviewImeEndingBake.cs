using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Ohabjizol.Interview;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class InterviewImeEndingBake
{
    static readonly EndingKind[] Kinds = { EndingKind.Good, EndingKind.Normal, EndingKind.Bad };
    static readonly string[] Names = { "Good_Ending", "Normal_Ending", "Re_Bad_Ending" };
    const string ArtFolder = "Assets/Art/Ending/Source/Aseprite/텍스트 X/";
    const string ReportPath = "Logs/CallImeEnding/ime-ending-apply.json";

    [Serializable]
    sealed class Report
    {
        public bool success;
        public string status;
        public int replacedInputs;
        public int savedEndingScenes;
        public string error;
    }

    public static void ConfigureEndingArtwork(EndingBoard board)
    {
        var sprites = new Sprite[3];
        for (int i = 0; i < sprites.Length; i++)
        {
            var matches = AssetDatabase.LoadAllAssetsAtPath(ArtFolder + Names[i] + ".aseprite")
                .OfType<Sprite>().Where(sprite => sprite.name == Names[i]).ToArray();
            if (matches.Length != 1)
                throw new InvalidOperationException("Expected one native Sprite named " + Names[i]);
            sprites[i] = matches[0];
            Vector2 size = sprites[i].rect.size;
            Vector2 expected = i == 2 ? new Vector2(211, 118) : new Vector2(192, 108);
            if (size != expected) throw new InvalidOperationException("Unexpected native Sprite dimensions: " + Names[i] + " " + size);
        }
        board.ConfigureArtworkEditor(sprites[0], sprites[1], sprites[2]);
    }

    [MenuItem("Ohabjizol/Interview/Apply IME and Ending Artwork")]
    public static void Apply()
    {
        var report = new Report { status = "started" };
        var active = SceneManager.GetActiveScene();
        var opened = new List<Scene>();
        try
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling)
                throw new InvalidOperationException("Stop Play Mode and wait for compilation.");
            var interview = SceneManager.GetSceneByPath("Assets/Scenes/InterviewScene.unity");
            if (!interview.IsValid() || !interview.isLoaded)
                throw new InvalidOperationException("Open InterviewScene additively before applying.");
            foreach (var kind in Kinds)
            {
                string path = "Assets/Scenes/" + EndingSceneRoute.For(kind) + ".unity";
                var existing = SceneManager.GetSceneByPath(path);
                if (!File.Exists(path)) throw new InvalidOperationException("Missing ending scene: " + path);
                if (existing.IsValid() && existing.isLoaded && existing.isDirty)
                    throw new InvalidOperationException("Save dirty ending scene before applying: " + path);
            }
            Undo.IncrementCurrentGroup();
            int group = Undo.GetCurrentGroup();
            Undo.SetCurrentGroupName("Apply notepad IME input");
            try
            {
                int screens = 0;
                foreach (var root in interview.GetRootGameObjects())
                    foreach (var screen in root.GetComponentsInChildren<InterviewScreen>(true))
                    {
                        screens++;
                        report.replacedInputs += screen.ConfigureNotepadImeEditor();
                    }
                if (screens == 0) throw new InvalidOperationException("InterviewScreen missing.");
                Undo.CollapseUndoOperations(group);
            }
            catch { Undo.RevertAllDownToGroup(group); throw; }
            if (report.replacedInputs > 0) EditorSceneManager.MarkSceneDirty(interview);
            foreach (var kind in Kinds)
            {
                string path = "Assets/Scenes/" + EndingSceneRoute.For(kind) + ".unity";
                var scene = SceneManager.GetSceneByPath(path);
                if (!scene.IsValid() || !scene.isLoaded)
                {
                    scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Additive);
                    opened.Add(scene);
                }
                if (!SceneManager.SetActiveScene(scene))
                    throw new InvalidOperationException("Could not activate ending scene: " + path);
                var boards = scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<EndingBoard>(true)).ToArray();
                if (boards.Length != 1) throw new InvalidOperationException("Expected one EndingBoard: " + path);
                ConfigureEndingArtwork(boards[0]);
                EditorSceneManager.MarkSceneDirty(scene);
                if (!EditorSceneManager.SaveScene(scene, path)) throw new IOException("Could not save " + path);
                report.savedEndingScenes++;
            }
            var buildScenes = EditorBuildSettings.scenes.ToList();
            foreach (var kind in Kinds)
            {
                string path = "Assets/Scenes/" + EndingSceneRoute.For(kind) + ".unity";
                if (!buildScenes.Any(entry => entry.path == path)) buildScenes.Add(new EditorBuildSettingsScene(path, true));
            }
            EditorBuildSettings.scenes = buildScenes.ToArray();
            report.success = true;
            report.status = "applied";
        }
        catch (Exception error)
        {
            report.status = "failed";
            report.error = error.ToString();
            throw;
        }
        finally
        {
            if (active.IsValid() && active.isLoaded) SceneManager.SetActiveScene(active);
            for (int i = opened.Count - 1; i >= 0; i--)
                if (opened[i].IsValid() && opened[i].isLoaded) EditorSceneManager.CloseScene(opened[i], true);
            Directory.CreateDirectory(Path.GetDirectoryName(ReportPath));
            File.WriteAllText(ReportPath, JsonUtility.ToJson(report, true));
        }
    }
}
