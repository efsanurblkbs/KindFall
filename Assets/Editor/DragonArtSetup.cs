using UnityEditor;
using UnityEngine;

namespace KindFall.Editor
{
    /// <summary>
    /// Assets/Art/ altındaki piksel art görselleri ilk içe aktarımda otomatik olarak
    /// 32 PPU, Point (no filter) ve Compression: None ayarlarıyla yapılandırır.
    /// </summary>
    public class DragonArtSetup : AssetPostprocessor
    {
        private void OnPreprocessTexture()
        {
            if (!assetPath.StartsWith("Assets/Art/")) return;

            TextureImporter importer = (TextureImporter)assetImporter;
            if (!importer.importSettingsMissing) return;

            importer.textureType = TextureImporterType.Sprite;
            importer.spritePixelsPerUnit = assetPath.Contains("BabyDragon_Sheet") ? 96f
                : (assetPath.Contains("DragonFireball_Sheet") || assetPath.Contains("Burnable") || assetPath.Contains("FireCrystal") || assetPath.Contains("EnergyBarrier") ? 64f : 32f);
            importer.filterMode = FilterMode.Point;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.mipmapEnabled = false;

            if (assetPath.Contains("_Sheet"))
            {
                importer.spriteImportMode = SpriteImportMode.Multiple;
            }
        }
    }
}
