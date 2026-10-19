using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace Ohabjizol.EditorTools
{
    public static class DiscordCallSpriteInventory
    {
        private const string Output = "Logs/DiscordCallUI";
        private static readonly string[] SourceFolders =
        {
            "Assets/Art/Discord/Source/Aseprite/오합지졸-디코",
            "Assets/Art/Discord/Source/Aseprite/디코 통화방 UI"
        };

        [Serializable]
        private sealed class Report
        {
            public string generatedUtc;
            public string previewPurpose = "Review only; not UI assets.";
            public List<AssetEntry> assets = new List<AssetEntry>();
            public List<string> errors = new List<string>();
        }

        [Serializable]
        private sealed class AssetEntry
        {
            public string path, guid, mainAssetType, importerType;
            public bool hasSprites;
            public List<SpriteEntry> sprites = new List<SpriteEntry>();
            public List<TextureEntry> textures = new List<TextureEntry>();
            public List<string> errors = new List<string>();
        }

        [Serializable]
        private sealed class SpriteEntry
        {
            public string name, guid, textureName;
            public long localFileID;
            public bool identified;
            public Rect rect;
            public Vector2 pivot;
            public int textureWidth, textureHeight;
        }

        [Serializable]
        private sealed class TextureEntry
        {
            public string name, guid, previewPath, error;
            public long localFileID;
            public bool identified, isReadable, isSubAsset;
            public int width, height;
        }

        [MenuItem("Ohabjizol/Interview/Inspect Call UI Sprites")]
        public static void Run()
        {
            var report = new Report { generatedUtc = DateTime.UtcNow.ToString("o") };
            string projectRoot = Directory.GetParent(Application.dataPath).FullName;
            string outputDirectory = Path.Combine(projectRoot, Output);
            Directory.CreateDirectory(Path.Combine(outputDirectory, "Previews"));
            var folders = new List<string>();
            foreach (string folder in SourceFolders)
            {
                if (AssetDatabase.IsValidFolder(folder)) folders.Add(folder);
                else report.errors.Add("Source folder missing: " + folder);
            }

            var paths = new SortedSet<string>(StringComparer.Ordinal);
            if (folders.Count > 0)
            {
                foreach (string guid in AssetDatabase.FindAssets("", folders.ToArray()))
                {
                    string path = AssetDatabase.GUIDToAssetPath(guid);
                    if (path.EndsWith(".aseprite", StringComparison.OrdinalIgnoreCase)) paths.Add(path);
                }
            }
            foreach (string path in paths)
            {
                var entry = new AssetEntry { path = path, guid = AssetDatabase.AssetPathToGUID(path) };
                report.assets.Add(entry);
                try
                {
                    var main = AssetDatabase.LoadMainAssetAtPath(path);
                    var importer = AssetImporter.GetAtPath(path);
                    entry.mainAssetType = main == null ? "null" : main.GetType().FullName;
                    entry.importerType = importer == null ? "null" : importer.GetType().FullName;
                    int textureIndex = 0;
                    foreach (var asset in AssetDatabase.LoadAllAssetsAtPath(path))
                    {
                        try
                        {
                            var sprite = asset as Sprite;
                            if (sprite != null)
                            {
                                string guid;
                                long id;
                                bool identified = AssetDatabase.TryGetGUIDAndLocalFileIdentifier(sprite, out guid, out id);
                                var texture = sprite.texture;
                                entry.sprites.Add(new SpriteEntry
                                {
                                    name = sprite.name, guid = guid, localFileID = id, identified = identified,
                                    rect = sprite.rect, pivot = sprite.pivot,
                                    textureName = texture == null ? null : texture.name,
                                    textureWidth = texture == null ? 0 : texture.width,
                                    textureHeight = texture == null ? 0 : texture.height
                                });
                            }
                            var nativeTexture = asset as Texture2D;
                            if (nativeTexture != null)
                                ExtractTexture(nativeTexture, entry, projectRoot, textureIndex++);
                        }
                        catch (Exception exception)
                        {
                            entry.errors.Add((asset == null ? "null" : asset.name) + ": " + exception.Message);
                        }
                    }
                }
                catch (Exception exception) { entry.errors.Add(exception.ToString()); }
                entry.hasSprites = entry.sprites.Count > 0;
                if (!entry.hasSprites) entry.errors.Add("No Sprite subassets found.");
            }
            if (paths.Count == 0) report.errors.Add("No .aseprite assets found in the specified source folders.");
            File.WriteAllText(Path.Combine(outputDirectory, "sprite-inventory.json"), JsonUtility.ToJson(report, true));
            Debug.Log("Call UI inventory: Logs/DiscordCallUI/sprite-inventory.json");
        }

        private static void ExtractTexture(Texture2D source, AssetEntry asset, string root, int index)
        {
            var entry = new TextureEntry { name = source.name };
            asset.textures.Add(entry);
            try
            {
                string guid;
                long id;
                entry.identified = AssetDatabase.TryGetGUIDAndLocalFileIdentifier(source, out guid, out id);
                entry.guid = guid;
                entry.localFileID = id;
                entry.width = source.width;
                entry.height = source.height;
                entry.isReadable = source.isReadable;
                entry.isSubAsset = AssetDatabase.IsSubAsset(source);
                // Identity-based names avoid invalid filename characters and duplicate texture names.
                string relative = Output + "/Previews/" + asset.guid + "_" + id + "_" + index + ".png";
                WritePreview(source, Path.Combine(root, relative));
                entry.previewPath = relative;
            }
            catch (Exception exception) { entry.error = exception.ToString(); }
        }

        private static void WritePreview(Texture2D source, string path)
        {
            if (source.isReadable)
            {
                File.WriteAllBytes(path, source.EncodeToPNG());
                return;
            }
            RenderTexture previous = RenderTexture.active;
            bool previousSrgbWrite = GL.sRGBWrite;
            RenderTexture temporary = null;
            Texture2D readable = null;
            try
            {
                temporary = RenderTexture.GetTemporary(source.width, source.height, 0,
                    RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB);
                GL.sRGBWrite = QualitySettings.activeColorSpace == ColorSpace.Linear;
                Graphics.Blit(source, temporary);
                RenderTexture.active = temporary;
                readable = new Texture2D(source.width, source.height, TextureFormat.RGBA32, false, false);
                readable.hideFlags = HideFlags.HideAndDontSave;
                readable.ReadPixels(new Rect(0, 0, source.width, source.height), 0, 0);
                readable.Apply(false, false);
                File.WriteAllBytes(path, readable.EncodeToPNG());
            }
            finally
            {
                RenderTexture.active = previous;
                GL.sRGBWrite = previousSrgbWrite;
                if (temporary != null) RenderTexture.ReleaseTemporary(temporary);
                if (readable != null) UnityEngine.Object.DestroyImmediate(readable);
            }
        }
    }
}
