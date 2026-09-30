using System;
using UnityEngine;
using UnityEngine.Scripting.APIUpdating;
#if DEBUG
using System.Text;
using GOILevelImporter.Utils;
#endif

[MovedFrom(false, "Assembly-CSharp", "", "GravityControlLite")]
public class GravityControlLite : MonoBehaviour
{
	private Vector2 gvec;

	public GravityControlLite.Attractors[] gravityWells;

	[Serializable]
	public class Attractors
	{
		public Transform gravityWell;

		public float gravityModifier;
	}
}
