using System;
using UnityEngine;

namespace GOILevelImporter.Core.Components
{
	public class CustomGroundCol : MonoBehaviour
	{
		public Color groundCol = new Color(0.7f, 0.6f, 0.3f);
		public string material;
		public bool isSolid;
	}
}