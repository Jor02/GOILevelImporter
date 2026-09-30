using System;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Scripting.APIUpdating;
#if DEBUG
using System.Text;
using GOILevelImporter.Utils;
#endif

[RequireComponent(typeof(Collider2D))]
[MovedFrom(false, "Assembly-CSharp", "", "CustomTrigger")]
public class CustomTrigger : MonoBehaviour
{
	public CustomTrigger.Trigger TriggerType;

	public bool Mode;

	public Vector3 Destination;

	public AudioSource source;

	public AudioClip Sound;

	private bool EnableCredits;

	public UnityEvent TriggerEvent;

	public Animation animation;

	public string anim;

	public CustomTrigger.DetectMode Detect;

	public GameObject[] TargetProp;

	public enum Trigger
	{
		Reset,
		Teleport,
		Finish,
		Playsound,
		Event = 5,
		Animation = 4,
		SwitchScene = 6
	}

	public enum DetectMode
	{
		PlayerOnly,
		PlayerAndProps,
		PropsOnly,
		SpecificProp
	}
}
