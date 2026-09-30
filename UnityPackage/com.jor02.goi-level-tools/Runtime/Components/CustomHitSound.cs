using System;
using System.Collections.Generic;
using UnityEngine;

namespace GOILevelImporter.Components
{
    [Serializable]
    public class CustomHitSound
    {
        public string name;
        public List<AudioClip> hits;

        public List<AudioClip> hardHits;
        public List<AudioClip> scrapes;

        [Tooltip("Hard hammer strikes on this material throw sparks instead of debris, like rock and metal.")]
        public bool isSolid;
    }
}
