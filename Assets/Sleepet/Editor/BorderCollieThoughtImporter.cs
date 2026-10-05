using UnityEditor;
using UnityEngine;

namespace Sleepet.Editor
{
    // GIF previews and PNG sequences share the same six generated keyframes.
    public sealed class BorderCollieThoughtImporter : AssetPostprocessor
    {
        void OnPreprocessTexture()
        {
            if (!assetPath.StartsWith("Assets/Sleepet/Resources/BorderCollieThoughts/") || !assetPath.EndsWith(".png")) return;
            var importer = (TextureImporter)assetImporter;
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = false;
            importer.filterMode = FilterMode.Bilinear;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.maxTextureSize = 256;
            importer.spritePixelsPerUnit = 100;
        }
    }
}
