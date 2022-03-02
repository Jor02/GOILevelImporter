using System;
using System.Collections.Generic;
using UnityEngine;

namespace GOILevelImporter.Core.Components
{
	[Serializable]
	public class CustomHitSound
	{
		public AudioClip GetHit()
		{
			if (this.hits.Count > 0)
			{
				return this.hits[UnityEngine.Random.Range(0, this.hits.Count)];
			}
			return null;
		}

		public AudioClip GetHardHit()
		{
			if (this.hits.Count > 0)
			{
				return this.hardHits[UnityEngine.Random.Range(0, this.hardHits.Count)];
			}
			return this.GetHit();
		}

		public AudioClip GetScrape()
		{
			if (this.scrapes.Count > 0)
			{
				return this.scrapes[UnityEngine.Random.Range(0, this.scrapes.Count)];
			}
			return null;
		}

		public string name;
		public List<AudioClip> hits;
		public List<AudioClip> hardHits;
		public List<AudioClip> scrapes;
	}
}