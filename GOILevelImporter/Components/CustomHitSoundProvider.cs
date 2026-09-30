using System;
using System.Collections.Generic;
using UnityEngine;

namespace GOILevelImporter.Components
{
	public class CustomMaterialProvider : MonoBehaviour
	{
		private void Awake()
		{
			foreach (CustomHitSound customHitSound in this.hitSounds)
			{
				this.hitDict.Add(customHitSound.name, customHitSound);
			}
		}

		public AudioClip GetHit(string mat)
		{
			if (this.hitDict.ContainsKey(mat))
			{
				return this.hitDict[mat].GetHit();
			}
			return null;
		}

		public AudioClip GetHardHit(string mat)
		{
			if (this.hitDict.ContainsKey(mat))
			{
				return this.hitDict[mat].GetHardHit();
			}
			return null;
		}

		public AudioClip GetScrape(string mat)
		{
			if (this.hitDict.ContainsKey(mat))
			{
				return this.hitDict[mat].GetScrape();
			}
			return null;
		}

		public CustomHitSound[] hitSounds;
		public Dictionary<string, CustomHitSound> hitDict = new Dictionary<string, CustomHitSound>();

		public bool IsSolid(string mat)
		{
			if (this.hitDict.ContainsKey(mat))
			{
				return this.hitDict[mat].isSolid;
			}

			return false;
		}
	}
}