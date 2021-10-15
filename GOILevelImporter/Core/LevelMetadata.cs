using System;
using UnityEngine;

namespace GOILevelImporter.Core
{
    [Serializable]
    public struct LevelMetadata
    {
        public string LevelName { get; }
        public string Author { get; }
        public string Description { get; }
        private byte[] Thumbnail;
        private byte type;
        public bool LegacyMap { get; }

        public LevelMetadata(string levelName, string author, string description, bool legacy, byte[] thumbnail, byte thumbnailFormat)
        {
            LevelName = levelName;
            Author = author;
            Description = description;
            Thumbnail = thumbnail;
            type = thumbnailFormat;
            LegacyMap = legacy;
        }

        public Texture2D GetThumbnail()
        {
            if (Thumbnail != null) { 
                Texture2D thumbnail = new Texture2D(960, 540, (TextureFormat)type, false);
                ImageConversion.LoadImage(thumbnail, Thumbnail);
                return thumbnail;
            }
            return null;
        }
    }
}
