using System;
using System.Collections.Generic;
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
        public bool hasThumbnail { get; }
        public bool LegacyMap { get; }

        /// <summary>
        /// Every key from the level's property bag that isn't one of the
        /// well known fields above. Legacy maps get their keys from the
        /// .txt/.mdata sidecar, new maps from the metadata blob in the .glf.
        /// </summary>
        public Dictionary<string, string> Properties { get; }

        //Camera and scene values pulled out of Properties but with defaults.
        public float ZPlane;
        public float FarPlane;
        public float BGFarPlane;
        public int CameraMode;
        public string Fog;
        public bool ReplaceSky;
        public bool ReplaceLighting;
        public bool HideShadow;
        public bool FixHammerMaterial;
        public bool ReplacementMap;
        public string StartScene;

        public LevelMetadata(string levelName, string author, string description, bool legacy, bool hasThumbnail, byte[] thumbnail, byte thumbnailFormat, Dictionary<string, string> properties = null)
        {
            LevelName = levelName;
            Author = author;
            Description = description;
            Thumbnail = thumbnail;
            type = thumbnailFormat;
            LegacyMap = legacy;
            Properties = properties ?? new Dictionary<string, string>();

            this.hasThumbnail = hasThumbnail;

            ReadSettings();
        }

        public Texture2D GetThumbnail()
        {
            if (hasThumbnail && Thumbnail != null) { 
                Texture2D thumbnail = new Texture2D(960, 540, (TextureFormat)type, false);
                ImageConversion.LoadImage(thumbnail, Thumbnail);
                return thumbnail;
            }
            return null;
        }

        /// <summary>
        /// Pulls the typed values out of the raw property bag. Called from the
        /// constructor so every metadata instance comes out fully populated.
        /// </summary>
        private void ReadSettings()
        {
            ZPlane = GetFloat("zplane", -20f);
            FarPlane = GetFloat("farplane", 100f);
            BGFarPlane = GetFloat("bgfarplane", 2800f);
            CameraMode = (int)GetFloat("cam", 0f);
            Fog = GetString("fog", null);
            ReplaceSky = GetFlag("sky");
            ReplaceLighting = GetFlag("lighting");
            HideShadow = GetInt("shadow", 0) > 0;
            FixHammerMaterial = GetInt("hammermat", 0) > 0;
            ReplacementMap = GetString("mode", string.Empty).ToLowerInvariant() == "r";
            StartScene = GetString("scene", null);

            if (CameraMode != 0)
            {
                FixHammerMaterial = true;
            }
        }

        private bool TryGet(string key, out string value) => Properties.TryGetValue(key, out value);

        private string GetString(string key, string fallback)
        {
            return TryGet(key, out var value) ? value : fallback;
        }

        private int GetInt(string key, int fallback)
        {
            if (TryGet(key, out var value) && int.TryParse(value, out var parsed))
            {
                return parsed;
            }
            return fallback;
        }

        private float GetFloat(string key, float fallback)
        {
            if (TryGet(key, out var value) && float.TryParse(value, out var parsed))
            {
                return parsed;
            }
            return fallback;
        }

        /// <summary>
        /// Legacy settings are written as key=r for things like sky and lighting.
        /// Anything other than "r" means leave the game's own version alone.
        /// </summary>
        private bool GetFlag(string key)
        {
            return TryGet(key, out var value) && value.ToLowerInvariant() == "r";
        }
    }
}
