using UnityEditor;
using UnityEngine;

namespace Neon.EditorTools
{
    /// Forces pixel-art import settings on everything under Resources/Art.
    /// The generator also writes .meta files, so this is a safety net for anything
    /// dropped in by hand later.
    public class ArtImport : AssetPostprocessor
    {
        void OnPreprocessTexture()
        {
            if (!assetPath.Replace('\\', '/').Contains("/Resources/Art/")) return;
            var t = (TextureImporter)assetImporter;
            t.textureType = TextureImporterType.Sprite;
            t.spriteImportMode = SpriteImportMode.Single;
            t.spritePixelsPerUnit = 32f;
            t.filterMode = FilterMode.Point;
            t.mipmapEnabled = false;
            t.alphaIsTransparency = true;
            t.wrapMode = TextureWrapMode.Clamp;
            t.textureCompression = TextureImporterCompression.Uncompressed;
            var s = t.GetDefaultPlatformTextureSettings();
            s.textureCompression = TextureImporterCompression.Uncompressed;
            s.format = TextureImporterFormat.Automatic;
            t.SetPlatformTextureSettings(s);
        }
    }
}
