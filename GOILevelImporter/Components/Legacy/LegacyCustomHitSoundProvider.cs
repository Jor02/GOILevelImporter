using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Scripting.APIUpdating;
#if DEBUG
using System.Text;
using GOILevelImporter.Utils;
#endif

[MovedFrom(false, "Assembly-CSharp", "", "CustomHitSoundProvider")]
public class CustomHitSoundProvider : MonoBehaviour
{
	public CustomHitSound[] hitSounds;
}
