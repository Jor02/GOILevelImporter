using System.IO;
using System.Reflection;
using UnityEngine;

namespace GOILevelImporter.Core.Menu
{
    internal static class UiAssets
    {
        public static Sprite DefaultThumb { get; private set; }
        public static Sprite LegacyThumb { get; private set; }
        public static Sprite MissingThumb { get; private set; }
        public static Sprite TempThumb { get; private set; }
        public static Sprite RefreshIcon { get; private set; }
        public static Sprite WarningIcon { get; private set; }

        public static Texture2D DefaultThumbTexture { get; private set; }

        public static void Load()
        {
            if (loaded) return;
            loaded = true;

            Texture2D defaultThumb;
            DefaultThumb = LoadSprite("DefaultThumb", out defaultThumb);
            DefaultThumbTexture = defaultThumb;
            LegacyThumb = LoadSprite("LegacyThumb");
            MissingThumb = LoadSprite("MissingThumb");
            TempThumb = LoadSprite("TempThumb");
            RefreshIcon = LoadSprite("refresh", mipmaps: true);
            WarningIcon = LoadSprite("Warning", mipmaps: true);
        }

        private static Sprite LoadSprite(string name, bool mipmaps = false)
        {
            return LoadSprite(name, out _, mipmaps);
        }

        /// <summary>
        /// Mipmaps matter for the small UI icons, which ship as large canvases but draw
        /// into a few dozen pixels. Without them, minification samples one texel per
        /// screen pixel and hard edges come out aliased instead of filtered.
        /// </summary>
        private static Sprite LoadSprite(string name, out Texture2D texture, bool mipmaps = false)
        {
            texture = null;

            byte[] png = ReadPng(name);
            if (png == null) return null;

            // LoadImage resizes the texture to the PNG's dimensions, so the mipChain flag
            // has to be set here rather than after decoding.
            texture = new Texture2D(2, 2, TextureFormat.RGBA32, mipmaps);
            if (!texture.LoadImage(png, mipmaps))
            {
                Debug.LogError("[GOI Level Importer] Could not decode the embedded " + name + ".png.");
                Destroy(texture);
                texture = null;
                return null;
            }
            texture.name = name;
            texture.wrapMode = TextureWrapMode.Clamp;
            texture.filterMode = mipmaps ? FilterMode.Trilinear : FilterMode.Bilinear;

            return Sprite.Create(texture, new Rect(0f, 0f, texture.width, texture.height), Vector2.one / 2f);
        }

        private static byte[] ReadPng(string name)
        {
            string resourceName = ResourcePrefix + name + ".png";
            Assembly assembly = typeof(UiAssets).Assembly;

            using (Stream stream = assembly.GetManifestResourceStream(resourceName))
            {
                if (stream == null)
                {
                    Debug.LogError("[GOI Level Importer] Embedded resource " + resourceName + " is missing, so the level select will be missing part of its art.");
                    return null;
                }

                using (MemoryStream buffer = new MemoryStream())
                {
                    stream.CopyTo(buffer);
                    return buffer.ToArray();
                }
            }
        }

        private static void Destroy(Object target)
        {
            if (Application.isPlaying) Object.Destroy(target);
            else Object.DestroyImmediate(target);
        }

        private const string ResourcePrefix = "GOILevelImporter.Assets.";
        private static bool loaded;
    }
}
