using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;

namespace GOILevelImporter.Core.Components
{
    class CustomTrigger : ComponentBase
    {
        public DetectMode Detect;
        public Trigger TriggerType;
        public bool Mode;
        public Vector3 Destination;
        public AudioClip Sound;
        public Transform[] GravityAtractors;
        public Animation animation;
        public string anim;
        public UnityEvent TriggerEvent;
        public GameObject[] TargetProp;
        public AudioSource source;

        protected override void StartComp()
        {
			if (TriggerType == CustomTrigger.Trigger.Playsound)
			{
				source = base.gameObject.GetComponent<AudioSource>();
				if (source == null)
				{
					source = (AudioSource)base.gameObject.AddComponent(typeof(AudioSource));
					return;
				}
			}
		}

		private void OnTriggerEnter2D(Collider2D col)
		{
			Debug.Log(TriggerType);
			if ((col.gameObject.layer != 8 && Detect == CustomTrigger.DetectMode.PlayerOnly) || (col.gameObject.layer == 8 && Detect == CustomTrigger.DetectMode.PropsOnly))
			{
				return;
			}
			if (Detect == CustomTrigger.DetectMode.SpecificProp && !TargetProp.Contains(col.gameObject))
			{
				return;
			}
			switch (TriggerType)
			{
				case CustomTrigger.Trigger.Reset:
					PlayerPrefs.DeleteKey("NumSaves");
					PlayerPrefs.DeleteKey("SaveGame0");
					PlayerPrefs.DeleteKey("SaveGame1");
					PlayerPrefs.DeleteKey("targetScene");
					PlayerPrefs.Save();
					SceneManager.LoadScene("Mian");
					return;
				case CustomTrigger.Trigger.Teleport:
					ComponentHelper.Instance.Teleport(Destination);
					if (!Mode)
					{
						Rigidbody2D component = ComponentHelper.Instance.player.GetComponent<Rigidbody2D>();
						if (component != null)
						{
							component.velocity = Vector2.zero;
						}
					}
					return;
				case CustomTrigger.Trigger.Finish:
					{
						MonoBehaviour.print("Finished");
						PlayerPrefs.SetFloat("LastTime", UnityEngine.Object.FindObjectOfType<Narrator>().timePlayedThisGame);
						PlayerPrefs.DeleteKey("NumSaves");
						PlayerPrefs.DeleteKey("SaveGame0");
						PlayerPrefs.DeleteKey("SaveGame1");
						PlayerPrefs.DeleteKey("targetScene");
						int num = PlayerPrefs.GetInt("NumWins");
						num++;
						PlayerPrefs.SetInt("NumWins", num);
						Debug.Log("Game Done, deleting saves");
						PlayerPrefs.Save();
						SceneManager.LoadScene("Reward Loader");
						return;
					}
				case CustomTrigger.Trigger.Playsound:
					if (!source.isPlaying)
					{
						source.PlayOneShot(Sound);
					}
					if (Mode)
					{
						Collider2D[] components = base.GetComponents<Collider2D>();
						for (int i = 0; i < components.Length; i++)
						{
							components[i].enabled = false;
						}
					}
					return;
				case CustomTrigger.Trigger.Animation:
					if (!string.IsNullOrEmpty(anim))
					{
						animation.Play(anim);
						return;
					}
					animation.Play();
					return;
				case CustomTrigger.Trigger.Event:
					TriggerEvent.Invoke();
					return;
				case CustomTrigger.Trigger.SwitchScene:
					PlayerPrefs.DeleteKey("NumSaves");
					PlayerPrefs.DeleteKey("SaveGame0");
					PlayerPrefs.DeleteKey("SaveGame1");
					PlayerPrefs.Save();

					Base.configTargetScene.SetSerializedValue(anim);
					Base.configTargetSceneLevel.SetSerializedValue(LevelLoader.currentBundlePath);
					LevelLoader.Scene = anim;

					SceneManager.LoadScene("Mian");
					return;
				default:
					return;
			}
		}

		public enum Trigger
        {
            Reset,
            Teleport,
            Finish,
            Playsound,
            Animation,
            Event,
            SwitchScene
        }

        public enum DetectMode
        {
            PlayerOnly,
            PlayerAndProps,
            PropsOnly,
            SpecificProp,
        }
    }
}
