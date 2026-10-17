using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace Ohabjizol.Interview.Editor
{
    public static class BakeApplicantVisuals
    {
        const string Root = "Assets/Art/Applicants/";
        const string CatalogPath = Root + "ApplicantVisualCatalog.asset";
        const string PrefabPath = "Assets/Prefabs/InterviewRefresh/InterviewScreen.prefab";
        static readonly string[] States = { "idle", "talk", "accepted", "rejected" };
        static readonly string[] SpriteFields = { "_idle", "_talk", "_accepted", "_rejected" };

        [Serializable] sealed class Manifest
        {
            public Dimensions dimensions;
            public Character[] characters;
        }
        [Serializable] sealed class Dimensions { public int width; public int height; }
        [Serializable] sealed class Character { public string id; public string tag; public Pose[] states; }
        [Serializable] sealed class Pose { public string state; public int sourceframe; public string path; }

        [MenuItem("Tools/Interview/Bake Applicant Visuals")]
        public static void Apply()
        {
            var manifest = JsonUtility.FromJson<Manifest>(File.ReadAllText(Root + "characters.json"));
            Require(manifest != null && manifest.dimensions != null && manifest.dimensions.width == 57 &&
                manifest.dimensions.height == 66, "Manifest dimensions must be 57 x 66.");
            Require(manifest.characters != null && manifest.characters.Length == 13, "Expected 13 characters.");
            var ids = new HashSet<string>(StringComparer.Ordinal);
            var paths = new HashSet<string>(StringComparer.Ordinal);
            for (int i = 0; i < 13; i++)
            {
                var character = manifest.characters[i];
                string expectedId = "character-" + (i + 1).ToString("D2");
                Require(character != null && character.id == expectedId && ids.Add(character.id),
                    "Invalid character ID or order at index " + i + ".");
                Require(character.states != null && character.states.Length == 4, "Expected four states: " + character.id);
                for (int j = 0; j < 4; j++)
                {
                    var pose = character.states[j];
                    string expectedPath = character.id + "/" + States[j] + ".png";
                    Require(pose != null && pose.state == States[j] && pose.sourceframe == i * 4 + j &&
                        pose.path == expectedPath && paths.Add(pose.path), "Invalid state/frame/path: " + expectedPath);
                    Require(File.Exists(Root + pose.path), "Missing PNG: " + expectedPath);
                }
            }
            Require(paths.Count == 52, "Expected 52 distinct PNGs.");
            Require(AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath), "Missing InterviewScreen prefab.");
            var catalog = AssetDatabase.LoadAssetAtPath<ApplicantVisualCatalog>(CatalogPath);
            Require(catalog || !File.Exists(CatalogPath), "Catalog path contains an incompatible asset.");
            var durations = new Dictionary<string, Vector2>(StringComparer.Ordinal);
            foreach (var character in manifest.characters)
            {
                ApplicantVisualDefinition existing;
                durations[character.id] = catalog && catalog.TryGet(character.id, out existing)
                    ? new Vector2(existing.AcceptedDuration, existing.RejectedDuration) : new Vector2(3f, 3f);
            }

            var sprites = new Sprite[13, 4];
            for (int i = 0; i < 13; i++)
            for (int j = 0; j < 4; j++)
            {
                string path = Root + manifest.characters[i].states[j].path;
                AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
                var importer = AssetImporter.GetAtPath(path) as TextureImporter;
                Require(importer != null, "Missing TextureImporter: " + path);
                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.filterMode = FilterMode.Point;
                importer.textureCompression = TextureImporterCompression.Uncompressed;
                importer.crunchedCompression = false;
                importer.mipmapEnabled = false;
                importer.alphaSource = TextureImporterAlphaSource.FromInput;
                importer.alphaIsTransparency = true;
                importer.npotScale = TextureImporterNPOTScale.None;
                importer.SaveAndReimport();
                var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
                Require(texture && texture.width == 57 && texture.height == 66, "Invalid imported dimensions: " + path);
                sprites[i, j] = AssetDatabase.LoadAssetAtPath<Sprite>(path);
                Require(sprites[i, j], "Missing Single Sprite: " + path);
            }

            var prefab = PrefabUtility.LoadPrefabContents(PrefabPath);
            try
            {
                var calls = prefab.GetComponentsInChildren<InterviewCallView>(true);
                Require(calls.Length == 1, "Expected exactly one authored InterviewCallView.");
                var call = new SerializedObject(calls[0]);
                var reference = call.FindProperty("_visualCatalog");
                var portraitField = call.FindProperty("_portrait");
                Require(reference != null && portraitField != null, "Missing CallView serialized fields.");
                var portrait = portraitField.objectReferenceValue as Image;
                Require(portrait, "Missing authored portrait Image.");
                if (!catalog)
                {
                    catalog = ScriptableObject.CreateInstance<ApplicantVisualCatalog>();
                    AssetDatabase.CreateAsset(catalog, CatalogPath);
                }
                var serialized = new SerializedObject(catalog);
                var entries = serialized.FindProperty("_visuals");
                entries.arraySize = 13;
                for (int i = 0; i < 13; i++)
                {
                    var entry = entries.GetArrayElementAtIndex(i);
                    string id = manifest.characters[i].id;
                    entry.FindPropertyRelative("_characterId").stringValue = id;
                    for (int j = 0; j < 4; j++)
                        entry.FindPropertyRelative(SpriteFields[j]).objectReferenceValue = sprites[i, j];
                    entry.FindPropertyRelative("_acceptedDuration").floatValue = durations[id].x;
                    entry.FindPropertyRelative("_rejectedDuration").floatValue = durations[id].y;
                }
                serialized.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(catalog);
                reference.objectReferenceValue = catalog;
                call.ApplyModifiedPropertiesWithoutUndo();
                portrait.preserveAspect = true;
                EditorUtility.SetDirty(portrait);
                bool saved;
                PrefabUtility.SaveAsPrefabAsset(prefab, PrefabPath, out saved);
                Require(saved, "Failed to save InterviewScreen prefab.");
                AssetDatabase.SaveAssets();
            }
            finally { PrefabUtility.UnloadPrefabContents(prefab); }
        }

        static void Require(bool condition, string message)
        {
            if (!condition) throw new InvalidOperationException(message);
        }
    }
}
