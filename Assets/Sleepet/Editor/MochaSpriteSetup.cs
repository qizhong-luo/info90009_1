using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace Sleepet.Editor
{
    public static class MochaSpriteSetup
    {
        const string Art = "Assets/Sleepet/Art/Mocha";
        [MenuItem("Sleepet/Apply free Mocha dog sprites")]
        public static void Apply()
        {
            AssetDatabase.Refresh();
            string[] files = Directory.GetFiles(Art, "*.png").OrderBy(p => p).ToArray();
            // Every source frame is 100x100. Use one shared crop across all motions,
            // retaining the author's foot position instead of trimming frames separately.
            int left = 100, bottom = 100, right = 0, top = 0;
            foreach (string path in files)
            {
                var texture = new Texture2D(2, 2);
                texture.LoadImage(File.ReadAllBytes(path));
                for (int y = 0; y < texture.height; y++)
                    for (int x = 0; x < texture.width; x++)
                        if (texture.GetPixel(x, y).a > 0.01f)
                        {
                            int localX = x % 100;
                            left = Mathf.Min(left, localX); right = Mathf.Max(right, localX + 1);
                            bottom = Mathf.Min(bottom, y); top = Mathf.Max(top, y + 1);
                        }
                UnityEngine.Object.DestroyImmediate(texture);
            }
            var motions = files.Select(path =>
            {
                string assetPath = path.Replace('\\', '/');
                var importer = (TextureImporter)AssetImporter.GetAtPath(assetPath);
                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Multiple;
                importer.filterMode = FilterMode.Point;
                importer.textureCompression = TextureImporterCompression.Uncompressed;
                importer.mipmapEnabled = false;
                importer.alphaIsTransparency = true;
                importer.npotScale = TextureImporterNPOTScale.None;
                importer.spritePixelsPerUnit = 100;
                importer.GetSourceTextureWidthAndHeight(out int width, out int height);
                string motionName = Path.GetFileNameWithoutExtension(path).Substring("Golden-Retriever-".Length).ToLowerInvariant();
                var slices = Enumerable.Range(0, width / 100).Select(i => new SpriteMetaData {
                    name = motionName + "_" + i.ToString("D2"),
                    rect = new Rect(i * 100 + left, bottom, right - left, top - bottom),
                    alignment = (int)SpriteAlignment.BottomCenter, pivot = new Vector2(0.5f, 0)
                }).ToArray();
#pragma warning disable 618
                importer.spritesheet = slices;
#pragma warning restore 618
                importer.SaveAndReimport();
                return new PetController.SpriteMotion { name = motionName,
                    fps = motionName == "stretching" ? 12 : motionName == "itching" ? 5 : 10,
                    frames = AssetDatabase.LoadAllAssetsAtPath(assetPath).OfType<Sprite>().OrderBy(s => s.name).ToArray() };
            }).ToArray();
            if (motions.Length != 11 || motions.Any(m => m.frames.Length == 0)) throw new Exception("Incomplete dog sprites.");
            var root = PrefabUtility.LoadPrefabContents(SleepetProjectPaths.PetPrefabPath);
            try
            {
                var pet = root.GetComponent<PetController>();
                pet.motions = motions;
                pet.spriteImage = pet.body.GetComponent<Image>();
                pet.spriteImage.sprite = motions.Single(m => m.name == "idle").frames[0];
                pet.spriteImage.color = Color.white;
                pet.spriteImage.type = Image.Type.Simple;
                pet.spriteImage.preserveAspect = true;
                pet.spriteImage.raycastTarget = false;
                foreach (Transform child in pet.body) child.gameObject.SetActive(false);
                pet.body.anchorMin = pet.body.anchorMax = new Vector2(0, 1);
                pet.body.pivot = new Vector2(0.5f, 0);
                pet.body.anchoredPosition = new Vector2(110, -132);
                pet.body.sizeDelta = new Vector2(right - left, top - bottom) * 4.5f;
                pet.stateLabel.text = "Mocha - Here with you";
                PrefabUtility.SaveAsPrefabAsset(root, SleepetProjectPaths.PetPrefabPath);
            }
            finally { PrefabUtility.UnloadPrefabContents(root); }
            AssetDatabase.SaveAssets();
            Debug.Log("Mocha CC0 sprites installed. Shared crop: " + new Rect(left, bottom, right-left, top-bottom));
        }
    }
}
