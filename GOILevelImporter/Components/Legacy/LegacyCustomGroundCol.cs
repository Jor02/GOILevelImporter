using System;
using UnityEngine;
using UnityEngine.Scripting.APIUpdating;
#if DEBUG
using GOILevelImporter.Utils;
#endif

[MovedFrom(false, "Assembly-CSharp", "", "CustomGroundCol")]
public class CustomGroundCol : MonoBehaviour
{
	public Color groundCol = new Color(0.7f, 0.6f, 0.3f);

	public string material;

	public bool isSolid;
}
